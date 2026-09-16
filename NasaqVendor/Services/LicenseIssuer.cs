using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Nasag.Licensing.Cryptography;
using Nasag.Licensing.License;
using NasaqVendor.Models;
using NasaqVendor.Repositories;

namespace NasaqVendor.Services;

/// <summary>
/// Orchestrates license issuance: builds LicenseFile, signs via ECDSA, writes .naslic, persists DB row + audit.
/// </summary>
public sealed class LicenseIssuer : ILicenseIssuer
{
    private readonly IIssuerKeyService _keyService;
    private readonly ICustomersRepository _customers;
    private readonly ILicensesRepository _licenses;
    private readonly IIssueAuditRepository _audit;

    public LicenseIssuer(
        IIssuerKeyService keyService,
        ICustomersRepository customers,
        ILicensesRepository licenses,
        IIssueAuditRepository audit)
    {
        _keyService = keyService;
        _customers = customers;
        _licenses = licenses;
        _audit = audit;
    }

    public async Task<IssueLicenseResult> IssueAsync(IssueLicenseRequest req)
    {
        if (req is null) throw new ArgumentNullException(nameof(req));
        if (!_keyService.HasKey)
            throw new InvalidOperationException("لا يوجد مفتاح خاص محمَّل. أنشئ المفاتيح أولاً من «إعدادات المفتاح».");
        if (string.IsNullOrWhiteSpace(req.TargetFilePath))
            throw new ArgumentException("يجب تحديد مسار حفظ ملف الترخيص.", nameof(req));
        if (req.MachineHashes is null || req.MachineHashes.Length == 0)
            throw new ArgumentException("بصمات الجهاز مطلوبة.", nameof(req));

        var customer = await _customers.GetByIdAsync(req.CustomerId)
            ?? throw new InvalidOperationException("العميل غير موجود.");

        var issuedAt = DateTime.UtcNow;
        var license = new LicenseFile
        {
            V = 1,
            CustomerId = customer.Code,
            CustomerName = customer.Name,
            MachineHashes = req.MachineHashes,
            IssuedAtUtc = issuedAt,
            ExpiresAtUtc = req.ExpiresAtUtc,
            Edition = req.Edition,
            Features = req.Features ?? Array.Empty<string>(),
            Signature = ""
        };

        var canonical = LicenseSerializer.SerializeForSigning(license);
        var signature = EcdsaSigner.Sign(canonical, _keyService.GetPrivateKeyBlob());
        license.Signature = Convert.ToBase64String(signature);

        var pretty = LicenseSerializer.Serialize(license);
        var dir = Path.GetDirectoryName(req.TargetFilePath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);
        await File.WriteAllTextAsync(req.TargetFilePath, pretty, Encoding.UTF8);

        var record = new LicenseRecord
        {
            CustomerId = customer.Id,
            Edition = req.Edition,
            FeaturesJson = JsonSerializer.Serialize(req.Features ?? Array.Empty<string>()),
            MachineHashesJson = JsonSerializer.Serialize(req.MachineHashes),
            IssuedAtUtc = issuedAt,
            ExpiresAtUtc = req.ExpiresAtUtc,
            LicenseFilePath = req.TargetFilePath,
            Revoked = false,
            CustomerName = customer.Name,
            CustomerCode = customer.Code
        };

        await _licenses.InsertAsync(record);
        await _audit.InsertAsync(new IssueAudit
        {
            LicenseId = record.Id,
            Action = "Issued",
            AtUtc = issuedAt,
            Operator = Environment.UserName,
            Notes = $"Edition={req.Edition}; Features={record.FeaturesJson}; Expires={(req.ExpiresAtUtc?.ToString("u") ?? "بلا")}"
        });

        return new IssueLicenseResult { Record = record, FilePath = req.TargetFilePath };
    }

    public async Task<string> ReExportAsync(int licenseId, string newTargetPath)
    {
        if (!_keyService.HasKey)
            throw new InvalidOperationException("لا يوجد مفتاح خاص محمَّل.");
        if (string.IsNullOrWhiteSpace(newTargetPath))
            throw new ArgumentException("يجب تحديد مسار الحفظ.", nameof(newTargetPath));

        var rec = await _licenses.GetByIdAsync(licenseId)
            ?? throw new InvalidOperationException("الترخيص غير موجود.");

        var features = JsonSerializer.Deserialize<string[]>(rec.FeaturesJson) ?? Array.Empty<string>();
        var hashes = JsonSerializer.Deserialize<string[]>(rec.MachineHashesJson) ?? Array.Empty<string>();

        var license = new LicenseFile
        {
            V = 1,
            CustomerId = rec.CustomerCode,
            CustomerName = rec.CustomerName,
            MachineHashes = hashes,
            IssuedAtUtc = rec.IssuedAtUtc,
            ExpiresAtUtc = rec.ExpiresAtUtc,
            Edition = rec.Edition,
            Features = features,
            Signature = ""
        };

        var canonical = LicenseSerializer.SerializeForSigning(license);
        var signature = EcdsaSigner.Sign(canonical, _keyService.GetPrivateKeyBlob());
        license.Signature = Convert.ToBase64String(signature);

        var pretty = LicenseSerializer.Serialize(license);
        var dir = Path.GetDirectoryName(newTargetPath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);
        await File.WriteAllTextAsync(newTargetPath, pretty, Encoding.UTF8);

        await _licenses.UpdateLicenseFilePathAsync(licenseId, newTargetPath);
        await _audit.InsertAsync(new IssueAudit
        {
            LicenseId = licenseId,
            Action = "Regenerated",
            AtUtc = DateTime.UtcNow,
            Operator = Environment.UserName,
            Notes = $"Re-exported to: {newTargetPath}"
        });

        return newTargetPath;
    }
}

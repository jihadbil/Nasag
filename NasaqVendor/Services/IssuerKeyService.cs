using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Nasag.Licensing.Cryptography;
using Nasag.Licensing.Storage;
using NasaqVendor.Helpers;

namespace NasaqVendor.Services;

/// <summary>
/// Holds the issuer ECDSA key pair. The private key is stored DPAPI-encrypted on disk
/// (current-user scope) via <see cref="ProtectedStateStore"/> and never displayed.
/// The public key blob (SubjectPublicKeyInfo) is held in-memory after load.
/// </summary>
public sealed class IssuerKeyService : IIssuerKeyService
{
    private readonly ProtectedStateStore _store;
    private byte[]? _privateKey;
    private byte[]? _publicKey;

    public IssuerKeyService()
    {
        _store = new ProtectedStateStore(PathProvider.IssuerPrivateKeyFile);
    }

    public bool HasKey => _privateKey is { Length: > 0 } && _publicKey is { Length: > 0 };

    public string? PublicKeyBase64 => _publicKey is null ? null : Convert.ToBase64String(_publicKey);

    public string? PublicKeyFingerprint
    {
        get
        {
            if (_publicKey is null) return null;
            var hash = SHA256.HashData(_publicKey);
            var sb = new StringBuilder(hash.Length * 3);
            for (var i = 0; i < hash.Length; i++)
            {
                sb.Append(hash[i].ToString("X2"));
                if (i < hash.Length - 1) sb.Append(':');
            }
            return sb.ToString();
        }
    }

    public string PrivateKeyFilePath => PathProvider.IssuerPrivateKeyFile;

    public event EventHandler? KeyChanged;

    public Task EnsureLoadedAsync()
    {
        if (HasKey) return Task.CompletedTask;
        if (!_store.Exists()) return Task.CompletedTask;

        var raw = _store.ReadProtected();
        if (raw is null || raw.Length == 0) return Task.CompletedTask;

        try
        {
            using var ecdsa = ECDsa.Create();
            ecdsa.ImportECPrivateKey(raw, out _);
            _privateKey = raw;
            _publicKey = ecdsa.ExportSubjectPublicKeyInfo();
            KeyChanged?.Invoke(this, EventArgs.Empty);
        }
        catch
        {
            // Corrupt or wrong curve — treat as no key.
            _privateKey = null;
            _publicKey = null;
        }

        return Task.CompletedTask;
    }

    public Task GenerateNewKeyPairAsync()
    {
        var (priv, pub) = EcdsaSigner.GenerateKeyPair();
        _store.WriteProtected(priv);
        _privateKey = priv;
        _publicKey = pub;
        KeyChanged?.Invoke(this, EventArgs.Empty);
        return Task.CompletedTask;
    }

    public Task ImportPrivateKeyAsync(byte[] rawPrivateKey)
    {
        if (rawPrivateKey is null || rawPrivateKey.Length == 0)
            throw new ArgumentException("ملف المفتاح الخاص فارغ.", nameof(rawPrivateKey));

        byte[] pub;
        try
        {
            using var ecdsa = ECDsa.Create();
            ecdsa.ImportECPrivateKey(rawPrivateKey, out _);
            pub = ecdsa.ExportSubjectPublicKeyInfo();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("ملف المفتاح الخاص غير صالح أو غير مدعوم. يجب أن يكون منحنى P-256.", ex);
        }

        _store.WriteProtected(rawPrivateKey);
        _privateKey = rawPrivateKey;
        _publicKey = pub;
        KeyChanged?.Invoke(this, EventArgs.Empty);
        return Task.CompletedTask;
    }

    public byte[] GetPublicKeyBlob()
    {
        if (_publicKey is null) throw new InvalidOperationException("لا يوجد مفتاح عام محمَّل.");
        return _publicKey;
    }

    public byte[] GetPrivateKeyBlob()
    {
        if (_privateKey is null) throw new InvalidOperationException("لا يوجد مفتاح خاص محمَّل.");
        return _privateKey;
    }

    public Task DeleteKeyAsync()
    {
        _store.Delete();
        _privateKey = null;
        _publicKey = null;
        KeyChanged?.Invoke(this, EventArgs.Empty);
        return Task.CompletedTask;
    }
}

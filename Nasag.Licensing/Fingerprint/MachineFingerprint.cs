using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Net.NetworkInformation;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Win32;

namespace Nasag.Licensing.Fingerprint;

/// <summary>
/// جمع بصمة الجهاز من 5 مصادر (CPU/Board/BIOS/MachineGuid/MAC) وحساب التجزئات.
/// </summary>
public static class MachineFingerprint
{
    private static readonly object _gate = new();
    private static FingerprintComponents? _cachedComponents;
    private static string? _cachedComposite;
    private static string[]? _cachedPerComponent;

    /// <summary>
    /// جمع المكوّنات الخام. أي عطل في WMI/Registry/NIC يعيد سلسلة فارغة بدل الكسر.
    /// </summary>
    public static FingerprintComponents Collect()
    {
        lock (_gate)
        {
            if (_cachedComponents is not null) return _cachedComponents;

            var cpu = SafeWmiFirstValue("Win32_Processor", "ProcessorId");
            var board = SafeWmiFirstValue("Win32_BaseBoard", "SerialNumber");
            var bios = SafeWmiFirstValue("Win32_BIOS", "SerialNumber");
            var machineGuid = SafeReadMachineGuid();
            var mac = SafeReadPrimaryMac();

            _cachedComponents = new FingerprintComponents(cpu, board, bios, machineGuid, mac);
            return _cachedComponents;
        }
    }

    /// <summary>
    /// تجزئات SHA-256 لكل مكوّن على حدة (طول الناتج = 5).
    /// </summary>
    public static string[] PerComponentSha256(FingerprintComponents c)
    {
        if (c is null) throw new ArgumentNullException(nameof(c), "مكوّنات البصمة مطلوبة.");
        lock (_gate)
        {
            if (_cachedPerComponent is not null && ReferenceEquals(c, _cachedComponents))
                return (string[])_cachedPerComponent.Clone();

            var arr = new[]
            {
                Sha256Hex(c.Cpu),
                Sha256Hex(c.Board),
                Sha256Hex(c.Bios),
                Sha256Hex(c.MachineGuid),
                Sha256Hex(c.Mac),
            };
            if (ReferenceEquals(c, _cachedComponents))
                _cachedPerComponent = (string[])arr.Clone();
            return arr;
        }
    }

    /// <summary>
    /// التجزئة المركّبة (SHA-256 لسلسلة التجزئات الخمس).
    /// </summary>
    public static string Composite(FingerprintComponents c)
    {
        if (c is null) throw new ArgumentNullException(nameof(c), "مكوّنات البصمة مطلوبة.");
        lock (_gate)
        {
            if (_cachedComposite is not null && ReferenceEquals(c, _cachedComponents))
                return _cachedComposite;

            var per = PerComponentSha256(c);
            var composite = Sha256Hex(string.Join("|", per));
            if (ReferenceEquals(c, _cachedComponents))
                _cachedComposite = composite;
            return composite;
        }
    }

    /// <summary>
    /// تطابق N-of-M بين تجزئات الجهاز الحالي وتلك المسجّلة في الترخيص.
    /// </summary>
    public static bool MatchesAtLeast(string[] currentHashes, string[] licensedHashes, int min = 3)
    {
        if (currentHashes is null || licensedHashes is null) return false;
        if (currentHashes.Length == 0 || licensedHashes.Length == 0) return false;

        int matches = 0;
        int len = Math.Min(currentHashes.Length, licensedHashes.Length);
        for (int i = 0; i < len; i++)
        {
            var a = currentHashes[i] ?? "";
            var b = licensedHashes[i] ?? "";
            if (a.Length == 0 || b.Length == 0) continue;
            if (string.Equals(a, b, StringComparison.OrdinalIgnoreCase))
                matches++;
        }
        return matches >= min;
    }

    // ----- داخلي -----

    private static string Sha256Hex(string? input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input ?? ""));
        var sb = new StringBuilder(bytes.Length * 2);
        foreach (var b in bytes) sb.Append(b.ToString("x2"));
        return sb.ToString();
    }

    private static string SafeWmiFirstValue(string wmiClass, string property)
    {
        try
        {
            using var searcher = new ManagementObjectSearcher($"SELECT {property} FROM {wmiClass}");
            using var collection = searcher.Get();
            foreach (var obj in collection)
            {
                try
                {
                    var val = obj[property]?.ToString();
                    obj.Dispose();
                    if (!string.IsNullOrWhiteSpace(val))
                        return val.Trim();
                }
                catch { /* تجاهل */ }
            }
        }
        catch
        {
            // WMI قد يفشل بسبب صلاحيات أو خدمة معطّلة — نتسامح.
        }
        return "";
    }

    private static string SafeReadMachineGuid()
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Cryptography");
            var val = key?.GetValue("MachineGuid")?.ToString();
            return string.IsNullOrWhiteSpace(val) ? "" : val.Trim();
        }
        catch
        {
            return "";
        }
    }

    private static string SafeReadPrimaryMac()
    {
        try
        {
            var nics = NetworkInterface.GetAllNetworkInterfaces();
            var candidates = new List<(string Mac, long Speed)>();
            foreach (var nic in nics)
            {
                if (nic is null) continue;
                if (nic.OperationalStatus != OperationalStatus.Up) continue;
                if (nic.NetworkInterfaceType == NetworkInterfaceType.Loopback) continue;
                if (nic.NetworkInterfaceType == NetworkInterfaceType.Tunnel) continue;

                var desc = nic.Description ?? "";
                if (desc.IndexOf("Virtual", StringComparison.OrdinalIgnoreCase) >= 0) continue;
                if (desc.IndexOf("Bluetooth", StringComparison.OrdinalIgnoreCase) >= 0) continue;

                var bytes = nic.GetPhysicalAddress().GetAddressBytes();
                if (bytes.Length == 0) continue;
                var mac = string.Join("-", bytes.Select(b => b.ToString("X2")));
                if (string.IsNullOrWhiteSpace(mac)) continue;

                long speed = 0;
                try { speed = nic.Speed; } catch { /* بعض الأجهزة ترمي على هذا */ }
                candidates.Add((mac, speed));
            }

            if (candidates.Count == 0) return "";
            // اختر الأسرع أولاً، ثم بالترتيب الأبجدي لثبات الإخراج.
            return candidates
                .OrderByDescending(x => x.Speed)
                .ThenBy(x => x.Mac, StringComparer.OrdinalIgnoreCase)
                .First().Mac;
        }
        catch
        {
            return "";
        }
    }
}

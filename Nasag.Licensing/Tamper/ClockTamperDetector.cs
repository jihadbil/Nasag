using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.Json;
using Nasag.Licensing.Storage;

namespace Nasag.Licensing.Tamper;

/// <summary>
/// كاشف عبث الساعة عبر علامة مائية عُلوية (High-Watermark) بالـ UTC + فحص صحة لنواة النظام.
/// </summary>
public sealed class ClockTamperDetector
{
    private const string RegWatermark = "ClockWatermarkTicks";
    private const string RegHmac = "ClockWatermarkHmac";
    private static readonly TimeSpan AllowedBackwardSlack = TimeSpan.FromHours(1);
    private static readonly TimeSpan SystemSanitySlack = TimeSpan.FromDays(30);

    private readonly ProtectedStateStore _store;
    private readonly RegistryMirror _registry;
    private readonly string _fingerprint;
    private readonly object _gate = new();

    public ClockTamperDetector(ProtectedStateStore store, RegistryMirror registry, string machineFingerprintComposite)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store), "مخزن الحالة المحمي مطلوب.");
        _registry = registry ?? throw new ArgumentNullException(nameof(registry), "مرآة السجل مطلوبة.");
        if (string.IsNullOrWhiteSpace(machineFingerprintComposite))
            throw new ArgumentException("بصمة الجهاز المركّبة مطلوبة لكاشف العبث.", nameof(machineFingerprintComposite));
        _fingerprint = machineFingerprintComposite;
    }

    public bool IsTampered(out string reason)
    {
        lock (_gate)
        {
            reason = "";
            var now = DateTime.UtcNow;

            var dpapiWm = ReadDpapiWatermark();
            var registryWm = ReadRegistryWatermark();

            DateTime? highest = null;
            if (dpapiWm.HasValue) highest = dpapiWm;
            if (registryWm.HasValue && (!highest.HasValue || registryWm.Value > highest.Value))
                highest = registryWm;

            if (highest.HasValue)
            {
                if (now < highest.Value - AllowedBackwardSlack)
                {
                    reason = "تم اكتشاف تراجع غير اعتيادي في ساعة النظام.";
                    return true;
                }
            }

            // فحص صحة نواة النظام.
            try
            {
                var kernel = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "kernel32.dll");
                if (File.Exists(kernel))
                {
                    var kernelTimeUtc = File.GetLastWriteTimeUtc(kernel);
                    if (now < kernelTimeUtc - SystemSanitySlack)
                    {
                        reason = "ساعة النظام تسبق التاريخ المتوقّع لملفات النظام.";
                        return true;
                    }
                }
            }
            catch
            {
                // فحص اختياري — لا نُفشل التحقق إن لم نستطع قراءة الملف.
            }

            return false;
        }
    }

    public void BumpWatermark()
    {
        lock (_gate)
        {
            var now = DateTime.UtcNow;
            var existing = ReadDpapiWatermark();
            // لا نتراجع أبداً عن العلامة العلوية.
            var target = existing.HasValue && existing.Value > now ? existing.Value : now;
            WriteWatermark(target);
        }
    }

    // ---- داخلي ----

    private DateTime? ReadDpapiWatermark()
    {
        if (!_store.Exists()) return null;
        var bytes = _store.ReadProtected();
        if (bytes is null || bytes.Length == 0) return null;

        try
        {
            var json = Encoding.UTF8.GetString(bytes);
            var dto = JsonSerializer.Deserialize<PersistedWatermark>(json);
            if (dto is null || string.IsNullOrEmpty(dto.Hmac)) return null;

            var payload = FormatPayload(dto.WatermarkTicks);
            if (!HmacUtil.Verify(_fingerprint, payload, dto.Hmac))
                return null; // عبث محتمل — تجاهل القيمة.

            return new DateTime(dto.WatermarkTicks, DateTimeKind.Utc);
        }
        catch
        {
            return null;
        }
    }

    private DateTime? ReadRegistryWatermark()
    {
        var ticks = _registry.GetTicks(RegWatermark);
        var hmac = _registry.GetString(RegHmac);
        if (!ticks.HasValue || string.IsNullOrEmpty(hmac)) return null;
        var payload = FormatPayload(ticks.Value);
        if (!HmacUtil.Verify(_fingerprint, payload, hmac)) return null;
        return new DateTime(ticks.Value, DateTimeKind.Utc);
    }

    private void WriteWatermark(DateTime watermarkUtc)
    {
        var utc = DateTime.SpecifyKind(watermarkUtc, DateTimeKind.Utc);
        var payload = FormatPayload(utc.Ticks);
        var hmac = HmacUtil.ComputeBase64(_fingerprint, payload);

        var dto = new PersistedWatermark
        {
            WatermarkTicks = utc.Ticks,
            Hmac = hmac,
        };
        try
        {
            var json = JsonSerializer.Serialize(dto);
            _store.WriteProtected(Encoding.UTF8.GetBytes(json));
        }
        catch
        {
            // مخزن DPAPI قد يفشل — المرآة كافية كحد أدنى.
        }
        _registry.SetTicks(RegWatermark, utc.Ticks);
        _registry.SetString(RegHmac, hmac);
    }

    private static string FormatPayload(long ticks)
    {
        return string.Format(CultureInfo.InvariantCulture, "watermark|{0}", ticks);
    }

    private sealed class PersistedWatermark
    {
        public long WatermarkTicks { get; set; }
        public string Hmac { get; set; } = "";
    }
}

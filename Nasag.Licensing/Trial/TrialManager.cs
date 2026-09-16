using System;
using System.Globalization;
using System.Text;
using System.Text.Json;
using Nasag.Licensing.Storage;

namespace Nasag.Licensing.Trial;

/// <summary>
/// إدارة فترة تجربة 30 يوماً مع HMAC مربوط ببصمة الجهاز.
/// </summary>
public sealed class TrialManager
{
    public const int TrialDays = 30;

    private const string RegStart = "TrialStartTicks";
    private const string RegLastSeen = "LastSeenTicks";
    private const string RegHmac = "TrialHmac";

    private readonly ProtectedStateStore _store;
    private readonly RegistryMirror _registry;
    private readonly string _fingerprint;
    private readonly object _gate = new();

    public TrialManager(ProtectedStateStore store, RegistryMirror registry, string machineFingerprintComposite)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store), "مخزن الحالة المحمي مطلوب.");
        _registry = registry ?? throw new ArgumentNullException(nameof(registry), "مرآة السجل مطلوبة.");
        if (string.IsNullOrWhiteSpace(machineFingerprintComposite))
            throw new ArgumentException("بصمة الجهاز المركّبة مطلوبة لإدارة التجربة.", nameof(machineFingerprintComposite));
        _fingerprint = machineFingerprintComposite;
    }

    /// <summary>
    /// قراءة الحالة من DPAPI أو إنشاؤها لأول مرة. إن وُجد ملف لكن HMAC غير صالح → تجربة منتهية.
    /// </summary>
    public TrialState GetOrInitializeTrial()
    {
        lock (_gate)
        {
            var existing = TryReadState();
            if (existing is not null) return existing;

            // قد يكون الملف مفقوداً ولكن السجل يحوي قيم. لو وُجد، فهي تجربة منتهية أو عبث.
            var regStart = _registry.GetTicks(RegStart);
            var regLast = _registry.GetTicks(RegLastSeen);
            var regHmac = _registry.GetString(RegHmac);
            if (regStart.HasValue && regLast.HasValue && !string.IsNullOrEmpty(regHmac))
            {
                var candidate = new TrialState(
                    new DateTime(regStart.Value, DateTimeKind.Utc),
                    new DateTime(regLast.Value, DateTimeKind.Utc),
                    regHmac);
                if (VerifyHmac(candidate))
                {
                    // إعادة كتابة الملف المحمي من المرآة.
                    PersistState(candidate);
                    return candidate;
                }
                // عبث — اعتبرها تجربة منتهية.
                return TamperedState();
            }

            // تجربة جديدة فعلاً.
            var now = DateTime.UtcNow;
            var fresh = BuildState(now, now);
            PersistState(fresh);
            return fresh;
        }
    }

    public int DaysRemaining(TrialState state)
    {
        if (state is null) return 0;
        if (state.TrialStartUtc == DateTime.MinValue) return 0;
        var elapsed = (DateTime.UtcNow - state.TrialStartUtc).TotalDays;
        var remaining = (int)Math.Floor(TrialDays - elapsed);
        return Math.Max(0, remaining);
    }

    public bool IsExpired(TrialState state)
    {
        if (state is null) return true;
        if (state.TrialStartUtc == DateTime.MinValue) return true;
        return DaysRemaining(state) <= 0;
    }

    /// <summary>
    /// تحديث LastSeen في الملف والسجل (يستدعى كل ~30 دقيقة + عند الإغلاق).
    /// </summary>
    public void BumpLastSeen()
    {
        lock (_gate)
        {
            var current = TryReadState();
            if (current is null) return;
            if (current.TrialStartUtc == DateTime.MinValue) return; // مُعطَّب
            var updated = BuildState(current.TrialStartUtc, DateTime.UtcNow);
            PersistState(updated);
        }
    }

    // ----- داخلي -----

    private TrialState? TryReadState()
    {
        if (!_store.Exists()) return null;
        var bytes = _store.ReadProtected();
        if (bytes is null || bytes.Length == 0) return null;

        try
        {
            var json = Encoding.UTF8.GetString(bytes);
            var dto = JsonSerializer.Deserialize<PersistedTrial>(json);
            if (dto is null) return null;

            var state = new TrialState(
                DateTime.SpecifyKind(dto.TrialStartUtc, DateTimeKind.Utc),
                DateTime.SpecifyKind(dto.LastSeenUtc, DateTimeKind.Utc),
                dto.Hmac ?? "");

            if (!VerifyHmac(state))
            {
                // تالف أو مُعدَّل يدوياً → تجربة منتهية.
                return TamperedState();
            }
            return state;
        }
        catch
        {
            return TamperedState();
        }
    }

    private TrialState TamperedState()
    {
        // TrialStart = MinValue يجعل IsExpired = true دائماً.
        var tampered = new TrialState(DateTime.MinValue, DateTime.UtcNow, "");
        // لا نكتب إلى الملف هنا — نُحافظ على البصمة الجنائية.
        return tampered;
    }

    private TrialState BuildState(DateTime startUtc, DateTime lastSeenUtc)
    {
        var hmac = ComputeHmac(startUtc, lastSeenUtc);
        return new TrialState(startUtc, lastSeenUtc, hmac);
    }

    private void PersistState(TrialState state)
    {
        var dto = new PersistedTrial
        {
            TrialStartUtc = state.TrialStartUtc,
            LastSeenUtc = state.LastSeenUtc,
            Hmac = state.Hmac,
        };
        var json = JsonSerializer.Serialize(dto);
        var bytes = Encoding.UTF8.GetBytes(json);
        try
        {
            _store.WriteProtected(bytes);
        }
        catch
        {
            // فشل DPAPI — السجل وحده يبقى مرآة.
        }
        _registry.SetTicks(RegStart, state.TrialStartUtc.Ticks);
        _registry.SetTicks(RegLastSeen, state.LastSeenUtc.Ticks);
        _registry.SetString(RegHmac, state.Hmac);
    }

    private string ComputeHmac(DateTime startUtc, DateTime lastSeenUtc)
    {
        var payload = string.Format(
            CultureInfo.InvariantCulture,
            "{0:O}|{1:O}",
            DateTime.SpecifyKind(startUtc, DateTimeKind.Utc),
            DateTime.SpecifyKind(lastSeenUtc, DateTimeKind.Utc));
        return HmacUtil.ComputeBase64(_fingerprint, payload);
    }

    private bool VerifyHmac(TrialState state)
    {
        if (string.IsNullOrEmpty(state.Hmac)) return false;
        var payload = string.Format(
            CultureInfo.InvariantCulture,
            "{0:O}|{1:O}",
            DateTime.SpecifyKind(state.TrialStartUtc, DateTimeKind.Utc),
            DateTime.SpecifyKind(state.LastSeenUtc, DateTimeKind.Utc));
        return HmacUtil.Verify(_fingerprint, payload, state.Hmac);
    }

    private sealed class PersistedTrial
    {
        public DateTime TrialStartUtc { get; set; }
        public DateTime LastSeenUtc { get; set; }
        public string Hmac { get; set; } = "";
    }
}

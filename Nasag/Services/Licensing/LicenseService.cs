using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Nasag.Licensing;
using Nasag.Licensing.Cryptography;
using Nasag.Licensing.Fingerprint;
using Nasag.Licensing.License;
using Nasag.Licensing.Storage;
using Nasag.Licensing.Tamper;
using Nasag.Licensing.Trial;

namespace Nasag.Services.Licensing;

/// <summary>
/// واجهة عُليا (Facade) فوق ‎Nasag.Licensing‎: تُجمِّع البصمة، التجربة، عبث الساعة، وقراءة/تحقق
/// ملف ‎.naslic‎. تخزّن آخر حالة محسوبة وتُنذر المشتركين بالتغيّرات.
/// </summary>
public sealed class LicenseService : ILicenseService
{
    private readonly object _gate = new();
    private readonly LicenseValidator _validator;
    private readonly FingerprintComponents _components;
    private readonly string[] _perComponentHashes;
    private readonly string _composite;
    private readonly TrialManager _trial;
    private readonly ClockTamperDetector _clock;
    private readonly ProtectedStateStore _trialStore;
    private readonly ProtectedStateStore _clockStore;

    public LicenseService()
    {
        PathProvider.EnsureNasaqFolderExists();

        var publicKey = LoadEmbeddedPublicKey();
        _validator = new LicenseValidator(publicKey);

        _components = MachineFingerprint.Collect();
        _perComponentHashes = MachineFingerprint.PerComponentSha256(_components);
        _composite = MachineFingerprint.Composite(_components);

        var trialPath = Path.Combine(PathProvider.NasaqLocalAppData, "trial.dat");
        var clockPath = Path.Combine(PathProvider.NasaqLocalAppData, "clock.dat");

        _trialStore = new ProtectedStateStore(trialPath);
        _clockStore = new ProtectedStateStore(clockPath);

        var trialRegistry = new RegistryMirror(@"Software\Nasaq\State\Trial");
        var clockRegistry = new RegistryMirror(@"Software\Nasaq\State\Clock");

        _trial = new TrialManager(_trialStore, trialRegistry, _composite);
        _clock = new ClockTamperDetector(_clockStore, clockRegistry, _composite);
    }

    public LicenseStatus? Status { get; private set; }

    public string[] CurrentMachineHashes => (string[])_perComponentHashes.Clone();

    public string MachineFingerprintBlock
    {
        get
        {
            var sb = new StringBuilder();
            sb.Append("CPU: ").AppendLine(_perComponentHashes[0]);
            sb.Append("Board: ").AppendLine(_perComponentHashes[1]);
            sb.Append("BIOS: ").AppendLine(_perComponentHashes[2]);
            sb.Append("Machine: ").AppendLine(_perComponentHashes[3]);
            sb.Append("MAC: ").Append(_perComponentHashes[4]);
            return sb.ToString();
        }
    }

    public event EventHandler? StatusChanged;

    public LicenseStatus GetStatusOnStartup()
    {
        lock (_gate)
        {
            var computed = ComputeStatus();
            SetStatusLocked(computed);
            // علامة مائية للساعة + تحديث LastSeen للتجربة.
            try { _clock.BumpWatermark(); } catch { /* تجاهل */ }
            try { _trial.BumpLastSeen(); } catch { /* تجاهل */ }
            return computed;
        }
    }

    public async Task<ActivationResult> ActivateAsync(string licenseFilePath)
    {
        if (string.IsNullOrWhiteSpace(licenseFilePath))
            return new ActivationResult(false, "مسار ملف الترخيص فارغ.", null);
        if (!File.Exists(licenseFilePath))
            return new ActivationResult(false, "ملف الترخيص غير موجود.", null);

        try
        {
            var content = await File.ReadAllTextAsync(licenseFilePath, Encoding.UTF8).ConfigureAwait(false);
            return await ActivateFromTextAsync(content).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            return new ActivationResult(false, $"تعذّر قراءة ملف الترخيص: {ex.Message}", null);
        }
    }

    public Task<ActivationResult> ActivateFromTextAsync(string licenseJson)
    {
        if (string.IsNullOrWhiteSpace(licenseJson))
            return Task.FromResult(new ActivationResult(false, "محتوى الترخيص فارغ.", null));

        try
        {
            var license = LicenseSerializer.Deserialize(licenseJson);
            var status = _validator.Validate(license, _perComponentHashes);

            switch (status)
            {
                case LicenseStatus.Activated:
                    // اكتب الملف إلى المسار المعتمد.
                    PathProvider.EnsureNasaqFolderExists();
                    File.WriteAllText(PathProvider.LicenseFile, licenseJson, Encoding.UTF8);

                    lock (_gate) { SetStatusLocked(status); }
                    return Task.FromResult(new ActivationResult(true, "تم تفعيل الترخيص بنجاح.", status));

                case LicenseStatus.Expired exp:
                    return Task.FromResult(new ActivationResult(false, exp.Reason ?? "انتهت صلاحية الترخيص.", status));

                case LicenseStatus.MachineMismatch mm:
                    return Task.FromResult(new ActivationResult(
                        false,
                        $"الترخيص صُدِر لجهاز مختلف (تطابق {mm.MatchCount} من 5 مكوّنات).",
                        status));

                case LicenseStatus.InvalidSignature sig:
                    return Task.FromResult(new ActivationResult(false, sig.Reason ?? "توقيع الترخيص غير صحيح.", status));

                case LicenseStatus.Missing:
                    return Task.FromResult(new ActivationResult(false, "ملف الترخيص فارغ.", status));

                default:
                    return Task.FromResult(new ActivationResult(false, "حالة الترخيص غير معروفة.", status));
            }
        }
        catch (InvalidOperationException ex)
        {
            return Task.FromResult(new ActivationResult(false, ex.Message, null));
        }
        catch (Exception ex)
        {
            return Task.FromResult(new ActivationResult(false, $"تعذّر التحقق من الترخيص: {ex.Message}", null));
        }
    }

    public void Deactivate()
    {
        lock (_gate)
        {
            try { if (File.Exists(PathProvider.LicenseFile)) File.Delete(PathProvider.LicenseFile); }
            catch { /* تجاهل أخطاء الحذف */ }

            var recomputed = ComputeStatus();
            SetStatusLocked(recomputed);
        }
    }

    // ───────── داخلي ─────────

    private LicenseStatus ComputeStatus()
    {
        // 1) عبث الساعة
        try
        {
            if (_clock.IsTampered(out var reason))
                return new LicenseStatus.TamperedClock(reason);
        }
        catch
        {
            // كاشف العبث ليس Show-stopper. نتجاوزه ونكمل.
        }

        // 2) ملف ترخيص محلي؟
        if (File.Exists(PathProvider.LicenseFile))
        {
            try
            {
                var json = File.ReadAllText(PathProvider.LicenseFile, Encoding.UTF8);
                var license = LicenseSerializer.Deserialize(json);
                return _validator.Validate(license, _perComponentHashes);
            }
            catch (Exception)
            {
                // ملف فاسد → نعتبره مفقوداً ونتراجع إلى التجربة.
            }
        }

        // 3) تجربة 30 يوماً
        try
        {
            var trialState = _trial.GetOrInitializeTrial();
            if (_trial.IsExpired(trialState))
                return new LicenseStatus.Expired(null, "انتهت فترة التجربة المجانية.");
            return new LicenseStatus.Trial(_trial.DaysRemaining(trialState));
        }
        catch (Exception ex)
        {
            return new LicenseStatus.Expired(null, $"تعذّر تهيئة التجربة: {ex.Message}");
        }
    }

    private void SetStatusLocked(LicenseStatus status)
    {
        var changed = !Equals(Status, status);
        Status = status;
        if (changed)
        {
            try { StatusChanged?.Invoke(this, EventArgs.Empty); }
            catch { /* لا يفسد التحديث */ }
        }
    }

    private static byte[] LoadEmbeddedPublicKey()
    {
        // المورد مضمَّن في تجميع Nasag باسم منطقي ‎"Nasag.issuer.public.key"‎.
        var hostAssembly = Assembly.GetExecutingAssembly();
        return EmbeddedPublicKey.LoadFromAssembly(hostAssembly, "Nasag.issuer.public.key");
    }
}

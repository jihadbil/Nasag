using System;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Nasag.Licensing.License;
using Nasag.Services;
using Nasag.Services.Licensing;
using Nasag.Views.Licensing;

namespace Nasag.ViewModels.Licensing;

/// <summary>
/// شاشة بوابة الترخيص — تُعرض عندما لا يكون الترخيص فعّالاً ولا تجربة سارية.
/// </summary>
public sealed partial class LicenseGateViewModel : ObservableObject
{
    private readonly ILicenseService _license;
    private readonly IServiceProvider _services;
    private readonly IApplicationRestarter _restarter;
    private readonly IErrorReporter _errors;

    public LicenseGateViewModel(
        ILicenseService license,
        IServiceProvider services,
        IApplicationRestarter restarter,
        IErrorReporter errors)
    {
        _license = license;
        _services = services;
        _restarter = restarter;
        _errors = errors;
        RefreshFromStatus();
    }

    [ObservableProperty] private string _title = "الترخيص مطلوب";
    [ObservableProperty] private string _subtitle = "يلزم تفعيل البرنامج للمتابعة.";
    [ObservableProperty] private string _heading = "لم يُفعَّل البرنامج بعد";
    [ObservableProperty] private string _explanation = "يرجى تفعيل البرنامج بملف ترخيص ‎.naslic‎ تحصل عليه من المورِّد.";
    [ObservableProperty] private string _statusKind = "Warning"; // Warning | Danger | Info

    public void SetStatus(LicenseStatus status)
    {
        switch (status)
        {
            case LicenseStatus.TamperedClock t:
                Title = "تم اكتشاف عبث في الساعة";
                Subtitle = "لا يمكن متابعة استخدام البرنامج حتى تُصحَّح ساعة النظام.";
                Heading = "ساعة النظام غير صحيحة";
                Explanation = string.IsNullOrWhiteSpace(t.Reason)
                    ? "اضبط ساعة وتاريخ Windows ثم أعد فتح البرنامج."
                    : $"{t.Reason}\nاضبط ساعة وتاريخ Windows ثم أعد فتح البرنامج.";
                StatusKind = "Danger";
                break;

            case LicenseStatus.Expired exp:
                Title = "انتهت الصلاحية";
                Subtitle = "يلزم تفعيل ترخيص جديد للمتابعة.";
                Heading = exp.License is null ? "انتهت فترة التجربة المجانية" : "انتهى ترخيص البرنامج";
                Explanation = string.IsNullOrWhiteSpace(exp.Reason)
                    ? "احصل على ملف ترخيص ‎.naslic‎ من المورِّد ثم اضغط «تفعيل البرنامج»."
                    : exp.Reason;
                StatusKind = "Warning";
                break;

            case LicenseStatus.MachineMismatch mm:
                Title = "الترخيص لا يطابق هذا الجهاز";
                Subtitle = "صُدِر الترخيص لجهاز مختلف.";
                Heading = "هذا الترخيص لجهاز آخر";
                Explanation = $"تطابق {mm.MatchCount} من 5 مكوّنات فقط. اطلب من المورِّد إصدار ترخيص جديد لهذا الجهاز.";
                StatusKind = "Danger";
                break;

            case LicenseStatus.InvalidSignature sig:
                Title = "الترخيص غير صحيح";
                Subtitle = "تعذّر التحقّق من توقيع الترخيص.";
                Heading = "ملف الترخيص غير صالح";
                Explanation = string.IsNullOrWhiteSpace(sig.Reason) ? "اطلب ملف ترخيص جديد من المورِّد." : sig.Reason;
                StatusKind = "Danger";
                break;

            case LicenseStatus.Missing:
            default:
                Title = "الترخيص مطلوب";
                Subtitle = "لا يوجد ترخيص مُحمَّل على هذا الجهاز.";
                Heading = "لم يُفعَّل البرنامج بعد";
                Explanation = "حمِّل ملف ترخيص ‎.naslic‎ من المورِّد لبدء استخدام البرنامج.";
                StatusKind = "Info";
                break;
        }
    }

    [RelayCommand]
    private void OpenActivation()
    {
        try
        {
            var win = _services.GetRequiredService<ActivationWindow>();
            win.DataContext = _services.GetRequiredService<ActivationViewModel>();
            win.Owner = Application.Current?.MainWindow;
            win.ShowDialog();

            // أعد القراءة بعد إغلاق نافذة التفعيل.
            var newStatus = _license.GetStatusOnStartup();
            if (newStatus is LicenseStatus.Activated or LicenseStatus.Trial)
            {
                _restarter.RestartNow();
                return;
            }

            RefreshFromStatus();
        }
        catch (Exception ex)
        {
            _errors.Report("تعذّر فتح نافذة التفعيل", ex.Message, ex);
        }
    }

    [RelayCommand]
    private void Exit()
    {
        Application.Current?.Shutdown(0);
    }

    private void RefreshFromStatus()
    {
        if (_license.Status is { } current)
            SetStatus(current);
    }
}

using System;
using System.Diagnostics;
using System.Windows;

namespace Nasag.Services;

/// <summary>
/// التنفيذ القياسي لـ <see cref="IApplicationRestarter"/>.
/// يضمن أن الإطلاق وإيقاف التطبيق يحدثان على خيط الواجهة (UI thread).
/// </summary>
public sealed class ApplicationRestarter : IApplicationRestarter
{
    private readonly IErrorReporter _errors;

    public ApplicationRestarter(IErrorReporter errors)
    {
        _errors = errors;
    }

    public void RestartNow()
    {
        var app = Application.Current;
        if (app is not null && !app.Dispatcher.CheckAccess())
        {
            app.Dispatcher.Invoke(RestartCore);
            return;
        }

        RestartCore();
    }

    private void RestartCore()
    {
        try
        {
            var exePath = Process.GetCurrentProcess().MainModule?.FileName;
            if (!string.IsNullOrWhiteSpace(exePath))
            {
                Process.Start(exePath);
            }
        }
        catch (Exception ex)
        {
            // لا نبتلع الخطأ — نعرضه للمستخدم ثم نُغلق على أي حال حتى يستطيع إعادة الإطلاق يدوياً.
            try { _errors.Report("تعذّر إعادة تشغيل البرنامج", ex.Message, ex); }
            catch { /* ignore secondary failure */ }
        }
        finally
        {
            Application.Current?.Shutdown(0);
        }
    }
}

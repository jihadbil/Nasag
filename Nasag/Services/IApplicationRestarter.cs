namespace Nasag.Services;

/// <summary>
/// خدمة إعادة تشغيل التطبيق نظيفاً. تُستخدم بعد تبديل قاعدة البيانات أو حفظ
/// إعدادات الاتصال من معالج الإعداد، لإطلاق نسخة جديدة من نفس الـ exe ثم
/// إغلاق العملية الحالية.
/// </summary>
public interface IApplicationRestarter
{
    /// <summary>
    /// Restarts the current process cleanly: launches the same exe in a new process,
    /// then shuts down the current WPF Application with exit code 0.
    /// If launching the new process fails, an error is reported via IErrorReporter and
    /// the app still shuts down (the user can relaunch manually).
    /// </summary>
    void RestartNow();
}

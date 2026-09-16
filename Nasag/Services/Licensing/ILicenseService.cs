using System;
using System.Threading.Tasks;
using Nasag.Licensing.License;

namespace Nasag.Services.Licensing;

/// <summary>
/// نتيجة عملية تفعيل ترخيص — نجاح/فشل، رسالة عربية، وحالة جديدة (إن نجحت).
/// </summary>
public sealed record ActivationResult(bool Success, string Message, LicenseStatus? NewStatus);

/// <summary>
/// خدمة عالية المستوى تُغلِّف قراءة/كتابة/تحقق ملف الترخيص + التجربة + كاشف عبث الساعة.
/// </summary>
public interface ILicenseService
{
    /// <summary>آخر حالة ترخيص محسوبة. تُحدَّث عبر <see cref="GetStatusOnStartup"/> أو <see cref="ActivateAsync"/>.</summary>
    LicenseStatus? Status { get; }

    /// <summary>إعادة احتساب الحالة وتخزينها مؤقتاً. تُستدعى عند بدء التشغيل.</summary>
    LicenseStatus GetStatusOnStartup();

    /// <summary>تجزئات SHA-256 للمكوّنات الخمسة الحالية للجهاز.</summary>
    string[] CurrentMachineHashes { get; }

    /// <summary>كتلة نصية متعددة الأسطر لعرض/نسخ بصمة الجهاز.</summary>
    string MachineFingerprintBlock { get; }

    /// <summary>تفعيل من ملف ‎.naslic على القرص — يُنسخ إلى ‎%LOCALAPPDATA%\Nasaq\license.naslic ويُتحقَّق منه.</summary>
    Task<ActivationResult> ActivateAsync(string licenseFilePath);

    /// <summary>تفعيل من نص JSON ملصوق — تُكتب نسخة إلى ‎%LOCALAPPDATA%\Nasaq\license.naslic ويُتحقَّق منها.</summary>
    Task<ActivationResult> ActivateFromTextAsync(string licenseJson);

    /// <summary>إلغاء التفعيل: حذف الملف من ‎%LOCALAPPDATA%‎ — تُعاد الحالة إلى تجربة أو ترخيص مفقود.</summary>
    void Deactivate();

    /// <summary>يُطلق عند تغيُّر <see cref="Status"/>.</summary>
    event EventHandler? StatusChanged;
}

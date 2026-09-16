using System;
using System.Collections.Generic;

namespace Nasag.Services;

/// <summary>
/// سجل الاتصالات المحفوظة. يحلّ محل <c>IConnectionStringProvider</c> القديم
/// ويدعم عدة قواعد بيانات محفوظة مع اختيار واحدة منها كاتصال نشط.
/// </summary>
public interface IConnectionRegistry
{
    /// <summary>كل الاتصالات المحفوظة مرتبة حسب الاسم الظاهر (تصاعدياً).</summary>
    IReadOnlyList<SavedConnection> All { get; }

    /// <summary>الاتصال النشط حالياً، أو <c>null</c> إذا كان السجل فارغاً.</summary>
    SavedConnection? Active { get; }

    /// <summary>
    /// سلسلة الاتصال المستخدمة فعلياً في <c>DbContextFactory</c>:
    /// النشط → <c>DefaultConnection</c> من <c>appsettings.json</c> →
    /// قيمة LocalDB افتراضية (لضمان قدرة الـ Host على البناء حتى لو فشل الاتصال،
    /// إذ يتولى مسار CannotConnect ← معالج الإعداد المعالجة الطبيعية).
    /// </summary>
    string ActiveConnectionString { get; }

    /// <summary>«Saved» | «AppSettings» | «Default» — نفس دلالة المزوّد القديم.</summary>
    string Source { get; }

    /// <summary>صحيح حين لا يحتوي السجل على أي إدخال (إشارة أول تشغيل).</summary>
    bool IsEmpty { get; }

    /// <summary>المسار الفعلي إلى <c>%LOCALAPPDATA%\Nasaq\connections.json</c>.</summary>
    string StoreFilePath { get; }

    /// <summary>يُطلق بعد أي تعديل (إضافة/تحديث/حذف/تعيين النشط) لتنبيه الواجهة.</summary>
    event EventHandler? Changed;

    /// <summary>يضيف اتصالاً جديداً؛ يجعله النشط إن كان الأول.</summary>
    SavedConnection Add(string displayName, string connectionString);

    /// <summary>يحدّث الاسم وسلسلة الاتصال. يرمي إذا لم يُعثَر على المعرّف.</summary>
    void Update(Guid id, string displayName, string connectionString);

    /// <summary>
    /// يحذف اتصالاً بمعرّفه. إذا كان هو النشط ووُجد غيره، يُصبح الأول التالي نشطاً.
    /// </summary>
    void Remove(Guid id);

    /// <summary>يعيّن الاتصال صاحب المعرّف نشطاً. يرمي إذا لم يُعثَر عليه.</summary>
    void SetActive(Guid id);

    /// <summary>يطبع <c>LastUsedAt</c> على الاتصال النشط (لإحصاءات آخر استخدام).</summary>
    void MarkActiveUsed();
}

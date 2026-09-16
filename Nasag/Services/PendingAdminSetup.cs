namespace Nasag.Services;

/// <summary>
/// حمولة بيانات المدير التي يجمعها معالج الإعداد (Setup Wizard)
/// عند إنشاء قاعدة بيانات جديدة، تُسلَّم لاحقاً للـ Seeder ليُنشئ
/// المستخدم الإداري الأول بدلاً من بيانات العرض التجريبية.
/// </summary>
public sealed class PendingAdminSetup
{
    public string FullName { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

using System;

namespace Nasag.Services;

/// <summary>
/// نموذج اتصال محفوظ (إدخال واحد ضمن سجل الاتصالات).
/// كل عنصر يمثّل قاعدة بيانات يستطيع المستخدم اختيارها من شاشة الدخول.
/// </summary>
public sealed class SavedConnection
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string DisplayName { get; set; } = string.Empty;
    public string ConnectionString { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastUsedAt { get; set; }
}

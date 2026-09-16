using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Nasag.Services;

/// <summary>
/// خادم SQL تم اكتشافه (أو إدخال احتياطي ثابت).
/// </summary>
/// <param name="DisplayName">الاسم الظاهر للمستخدم بالعربية حيثما أمكن.</param>
/// <param name="ConnectionTarget">القيمة التي توضع في <c>Data Source</c>.</param>
/// <param name="IsLocal">صحيح إذا كان الخادم على الجهاز المحلي.</param>
public sealed record DiscoveredServer(string DisplayName, string ConnectionTarget, bool IsLocal);

public interface IServerDiscoveryService
{
    /// <summary>
    /// يعيد قائمة خوادم SQL Server المتاحة من هذا الجهاز، مع إدخالات احتياطية
    /// دائمة (LocalDB، .، .\SQLEXPRESS). يُزال التكرار حسب <c>ConnectionTarget</c>.
    /// يحترم الإلغاء، ومحدّد داخلياً بـ ~6 ثوانٍ لاكتشاف UDP.
    /// </summary>
    Task<IReadOnlyList<DiscoveredServer>> DiscoverAsync(CancellationToken ct = default);
}

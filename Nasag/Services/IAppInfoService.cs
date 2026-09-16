namespace Nasag.Services;

public interface IAppInfoService
{
    string AppNameAr { get; }
    string AppTagline { get; }
    string Version { get; }
}

public sealed class AppInfoService : IAppInfoService
{
    public string AppNameAr => "نَسَق لإدارة المدارس";
    public string AppTagline => "كل بيانات المدرسة في نظام واحد بسيط";
    public string Version => "0.1.0";
}

using System;

namespace Nasag.Services;

public interface IErrorReporter
{
    /// <summary>Show a friendly Arabic error window with full technical details + copy button.</summary>
    void Report(string title, string userMessage, Exception? exception = null);

    /// <summary>Convenience overload — uses a default Arabic title.</summary>
    void Report(Exception exception);
}

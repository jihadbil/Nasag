using System;
using System.Globalization;
using System.Text;
using System.Windows;
using Nasag.Views.Common;

namespace Nasag.Services;

public sealed class ErrorReporter : IErrorReporter
{
    public void Report(string title, string userMessage, Exception? exception = null)
        => ShowOnUiThread(title, userMessage, exception);

    public void Report(Exception exception)
        => ShowOnUiThread("حدث خطأ غير متوقع", exception.Message, exception);

    public static string FormatDetails(string title, string userMessage, Exception? exception)
    {
        var sb = new StringBuilder();
        sb.AppendLine("=== نَسَق لإدارة المدارس — تقرير خطأ ===");
        sb.Append("الوقت: ").AppendLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));
        sb.Append("العنوان: ").AppendLine(title);
        sb.Append("الرسالة: ").AppendLine(userMessage);
        sb.AppendLine();
        if (exception is not null)
        {
            sb.AppendLine("--- Exception ---");
            AppendException(sb, exception, depth: 0);
        }
        return sb.ToString();
    }

    private static void AppendException(StringBuilder sb, Exception ex, int depth)
    {
        var indent = new string(' ', depth * 2);
        sb.Append(indent).Append("Type: ").AppendLine(ex.GetType().FullName);
        sb.Append(indent).Append("Message: ").AppendLine(ex.Message);
        if (ex.Source is not null)
            sb.Append(indent).Append("Source: ").AppendLine(ex.Source);
        if (ex.StackTrace is not null)
        {
            sb.Append(indent).AppendLine("StackTrace:");
            sb.AppendLine(ex.StackTrace);
        }
        if (ex.InnerException is not null)
        {
            sb.AppendLine();
            sb.Append(indent).AppendLine("--- Inner Exception ---");
            AppendException(sb, ex.InnerException, depth + 1);
        }
    }

    private static void ShowOnUiThread(string title, string userMessage, Exception? exception)
    {
        try
        {
            var logDir = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Nasaq", "logs");
            System.IO.Directory.CreateDirectory(logDir);
            var logFile = System.IO.Path.Combine(logDir, "error.log");
            System.IO.File.AppendAllText(logFile, FormatDetails(title, userMessage, exception) + "\n----------------------------------------\n");
        }
        catch { }

        var app = Application.Current;
        if (app is null)
        {
            // Fallback if WPF isn't initialized yet (very early startup).
            // IDialogService / NasaqDialog both need a live WPF Dispatcher to render — at this
            // point we have no Application and no Dispatcher, so MessageBox is the only option
            // that can surface the error to the user. RTL options keep the Arabic text aligned.
            MessageBox.Show(
                userMessage + "\n\n" + (exception?.ToString() ?? string.Empty),
                title,
                MessageBoxButton.OK,
                MessageBoxImage.Error,
                MessageBoxResult.OK,
                MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign);
            return;
        }

        if (app.Dispatcher.CheckAccess())
            ShowWindow(title, userMessage, exception);
        else
            app.Dispatcher.BeginInvoke(new Action(() => ShowWindow(title, userMessage, exception)));
    }

    private static void ShowWindow(string title, string userMessage, Exception? exception)
    {
        try
        {
            var window = new ErrorWindow(title, userMessage, FormatDetails(title, userMessage, exception))
            {
                Owner = Application.Current?.MainWindow != null && Application.Current.MainWindow.IsVisible
                    ? Application.Current.MainWindow
                    : null
            };
            window.ShowDialog();
        }
        catch
        {
            // If the window itself blew up, fall back to MessageBox so the user still sees the original error.
            MessageBox.Show(userMessage + "\n\n" + (exception?.ToString() ?? string.Empty), title,
                MessageBoxButton.OK, MessageBoxImage.Error,
                MessageBoxResult.OK,
                MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign);
        }
    }
}

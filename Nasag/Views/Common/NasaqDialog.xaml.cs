using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace Nasag.Views.Common;

public enum NasaqDialogKind { Info, Success, Warning, Danger, Question }

public partial class NasaqDialog : Window
{
    public bool Result { get; private set; }

    private NasaqDialog()
    {
        InitializeComponent();
        // Drag the window from anywhere — there is no system chrome.
        MouseLeftButtonDown += (_, _) =>
        {
            if (Mouse.LeftButton == MouseButtonState.Pressed) DragMove();
        };
    }

    public static bool Confirm(
        Window? owner,
        string title,
        string message,
        string okText = "تأكيد",
        string cancelText = "إلغاء",
        NasaqDialogKind kind = NasaqDialogKind.Question)
    {
        var dlg = Build(owner, title, message, kind, okText, cancelText, showCancel: true);
        dlg.ShowDialog();
        return dlg.Result;
    }

    public static void Show(
        Window? owner,
        string title,
        string message,
        NasaqDialogKind kind = NasaqDialogKind.Info,
        string okText = "موافق")
    {
        var dlg = Build(owner, title, message, kind, okText, cancelText: null, showCancel: false);
        dlg.ShowDialog();
    }

    private static NasaqDialog Build(
        Window? owner,
        string title,
        string message,
        NasaqDialogKind kind,
        string okText,
        string? cancelText,
        bool showCancel)
    {
        var resolvedOwner = owner ?? (Application.Current?.Windows.Count > 0 ? Application.Current?.MainWindow : null);
        var dlg = new NasaqDialog { Owner = resolvedOwner };
        dlg.TitleText.Text = title;
        dlg.MessageText.Text = message;
        dlg.ConfirmBtn.Content = okText;

        if (showCancel)
        {
            dlg.CancelBtn.Visibility = Visibility.Visible;
            if (!string.IsNullOrEmpty(cancelText)) dlg.CancelBtn.Content = cancelText;
        }
        else
        {
            dlg.CancelBtn.Visibility = Visibility.Collapsed;
        }

        ApplyKind(dlg, kind);
        return dlg;
    }

    private static void ApplyKind(NasaqDialog dlg, NasaqDialogKind kind)
    {
        var (bubble, stroke, geom) = kind switch
        {
            NasaqDialogKind.Success  => ("SuccessSoftBrush", "SuccessBrush", "M5 12 L10 17 L20 7"),
            NasaqDialogKind.Warning  => ("WarningSoftBrush", "WarningBrush", "M12 8 V13 M12 17 H12.01 M2 20 L12 4 L22 20 Z"),
            NasaqDialogKind.Danger   => ("DangerSoftBrush",  "DangerBrush",  "M12 8 V13 M12 17 H12.01 M2 20 L12 4 L22 20 Z"),
            NasaqDialogKind.Question => ("InfoSoftBrush",    "InfoBrush",    "M9 9 a3 3 0 1 1 4 2 c-1 1 -1 2 -1 3 M12 17 h.01"),
            _                        => ("TealSoftBrush",    "TealPressedBrush", "M12 8 H12.01 M11 12 H12 V17 H13"),
        };

        if (dlg.FindResource(bubble) is Brush bb) dlg.IconBubble.Background = bb;
        if (dlg.FindResource(stroke) is Brush sb) dlg.IconPath.Stroke = sb;
        dlg.IconPath.Data = Geometry.Parse(geom);

        if (kind == NasaqDialogKind.Danger)
        {
            // Use the danger button style for destructive confirmations.
            if (dlg.FindResource("DangerButton") is Style dangerStyle)
                dlg.ConfirmBtn.Style = dangerStyle;
        }
    }

    private void OnConfirmClick(object sender, RoutedEventArgs e)
    {
        Result = true;
        Close();
    }

    private void OnCancelClick(object sender, RoutedEventArgs e)
    {
        Result = false;
        Close();
    }
}

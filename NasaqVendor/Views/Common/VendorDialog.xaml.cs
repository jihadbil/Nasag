using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using NasaqVendor.Services;

namespace NasaqVendor.Views.Common;

public partial class VendorDialog : Window
{
    public bool Result { get; private set; }

    private VendorDialog()
    {
        InitializeComponent();
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
        DialogKind kind = DialogKind.Question)
    {
        var dlg = Build(owner, title, message, kind, okText, cancelText, showCancel: true);
        dlg.ShowDialog();
        return dlg.Result;
    }

    public static void Show(
        Window? owner,
        string title,
        string message,
        DialogKind kind = DialogKind.Info,
        string okText = "موافق")
    {
        var dlg = Build(owner, title, message, kind, okText, cancelText: null, showCancel: false);
        dlg.ShowDialog();
    }

    private static VendorDialog Build(
        Window? owner,
        string title,
        string message,
        DialogKind kind,
        string okText,
        string? cancelText,
        bool showCancel)
    {
        var resolvedOwner = owner ?? (Application.Current?.Windows.Count > 0 ? Application.Current?.MainWindow : null);
        if (resolvedOwner is not null && ReferenceEquals(resolvedOwner, Application.Current?.MainWindow) == false)
        {
            // ensure owner is visible
        }
        var dlg = new VendorDialog();
        if (resolvedOwner is not null && resolvedOwner.IsLoaded)
            dlg.Owner = resolvedOwner;
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

    private static void ApplyKind(VendorDialog dlg, DialogKind kind)
    {
        var (bubble, stroke, geom) = kind switch
        {
            DialogKind.Success  => ("SuccessSoftBrush", "SuccessBrush", "M5 12 L10 17 L20 7"),
            DialogKind.Warning  => ("WarningSoftBrush", "WarningBrush", "M12 8 V13 M12 17 H12.01 M2 20 L12 4 L22 20 Z"),
            DialogKind.Danger   => ("DangerSoftBrush",  "DangerBrush",  "M12 8 V13 M12 17 H12.01 M2 20 L12 4 L22 20 Z"),
            DialogKind.Question => ("InfoSoftBrush",    "InfoBrush",    "M9 9 a3 3 0 1 1 4 2 c-1 1 -1 2 -1 3 M12 17 h.01"),
            _                   => ("TealSoftBrush",    "TealPressedBrush", "M12 8 H12.01 M11 12 H12 V17 H13"),
        };

        if (dlg.FindResource(bubble) is Brush bb) dlg.IconBubble.Background = bb;
        if (dlg.FindResource(stroke) is Brush sb) dlg.IconPath.Stroke = sb;
        dlg.IconPath.Data = Geometry.Parse(geom);

        if (kind == DialogKind.Danger)
        {
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

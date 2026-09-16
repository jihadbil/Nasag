using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Nasag.Repositories;

namespace Nasag.Views.Pages.Classes.Dialogs;

public partial class MoveStudentDialog : Window
{
    public MoveTargetSection? Picked { get; private set; }

    private MoveStudentDialog(string studentName, string? currentSection, IReadOnlyList<MoveTargetSection> targets)
    {
        InitializeComponent();
        SubtitleText.Text = string.IsNullOrEmpty(currentSection)
            ? studentName
            : $"{studentName} — حالياً في «{currentSection}»";

        MouseLeftButtonDown += (_, _) =>
        {
            if (Mouse.LeftButton == MouseButtonState.Pressed) DragMove();
        };

        TargetBox.ItemsSource = targets;
    }

    public static MoveTargetSection? Show(string studentName, string? currentSection, IReadOnlyList<MoveTargetSection> targets)
    {
        var owner = Application.Current?.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive)
                    ?? Application.Current?.MainWindow;
        var dlg = new MoveStudentDialog(studentName, currentSection, targets) { Owner = owner };
        dlg.ShowDialog();
        return dlg.Picked;
    }

    private void OnConfirmClick(object sender, RoutedEventArgs e)
    {
        if (TargetBox.SelectedItem is not MoveTargetSection target)
        {
            ErrorText.Text = "الرجاء اختيار الشعبة الجديدة.";
            ErrorText.Visibility = Visibility.Visible;
            return;
        }

        if (target.StudentCount >= target.Capacity)
        {
            ErrorText.Text = $"الشعبة ممتلئة ({target.StudentCount}/{target.Capacity}). اختر شعبة أخرى.";
            ErrorText.Visibility = Visibility.Visible;
            return;
        }

        Picked = target;
        Close();
    }

    private void OnCancelClick(object sender, RoutedEventArgs e)
    {
        Picked = null;
        Close();
    }
}

using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Nasag.Repositories;
using Nasag.ViewModels.Pages.Classes;

namespace Nasag.Views.Pages.Classes.Dialogs;

public partial class GradeEditorDialog : Window
{
    public bool Result { get; private set; }

    private readonly GradeSaveModel _model;
    private readonly IReadOnlyList<GradeLevelOption> _levels;

    private GradeEditorDialog(GradeSaveModel model, bool isEdit, IReadOnlyList<GradeLevelOption> levels)
    {
        InitializeComponent();
        _model = model;
        _levels = levels;
        TitleText.Text = isEdit ? "تعديل الصف" : "إضافة صف";

        MouseLeftButtonDown += (_, _) =>
        {
            if (Mouse.LeftButton == MouseButtonState.Pressed) DragMove();
        };

        LevelBox.ItemsSource = levels;
        LevelBox.SelectedItem = levels.FirstOrDefault(o => o.Value == model.Level) ?? levels[0];

        NameBox.Text = model.NameAr;
        SortOrderBox.Text = model.SortOrder.ToString();

        Loaded += (_, _) => NameBox.Focus();
    }

    public static bool Show(GradeSaveModel model, bool isEdit, IReadOnlyList<GradeLevelOption> levels)
    {
        var owner = Application.Current?.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive)
                    ?? Application.Current?.MainWindow;
        var dlg = new GradeEditorDialog(model, isEdit, levels) { Owner = owner };
        dlg.ShowDialog();
        return dlg.Result;
    }

    private void OnConfirmClick(object sender, RoutedEventArgs e)
    {
        var name = NameBox.Text?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(name))
        {
            ShowError("الرجاء إدخال اسم الصف.");
            NameBox.Focus();
            return;
        }
        if (!int.TryParse(SortOrderBox.Text, out var sort) || sort < 0)
        {
            ShowError("ترتيب العرض يجب أن يكون رقماً صحيحاً موجباً.");
            SortOrderBox.Focus();
            return;
        }

        _model.NameAr = name;
        _model.Level = ((GradeLevelOption?)LevelBox.SelectedItem)?.Value ?? _levels[0].Value;
        _model.SortOrder = sort;

        Result = true;
        Close();
    }

    private void OnCancelClick(object sender, RoutedEventArgs e)
    {
        Result = false;
        Close();
    }

    private void ShowError(string message)
    {
        ErrorText.Text = message;
        ErrorText.Visibility = Visibility.Visible;
    }
}

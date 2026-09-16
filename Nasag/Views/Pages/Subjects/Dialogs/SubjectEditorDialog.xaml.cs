using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Nasag.Repositories;

namespace Nasag.Views.Pages.Subjects.Dialogs;

public partial class SubjectEditorDialog : Window
{
    public bool Result { get; private set; }

    private readonly SubjectSaveModel _model;
    private readonly IReadOnlyList<SubjectGradeOption> _grades;

    private SubjectEditorDialog(SubjectSaveModel model, bool isEdit, IReadOnlyList<SubjectGradeOption> grades)
    {
        InitializeComponent();
        _model = model;
        _grades = grades;

        TitleText.Text = isEdit ? "تعديل المادة" : "إضافة مادة";

        MouseLeftButtonDown += (_, _) =>
        {
            if (Mouse.LeftButton == MouseButtonState.Pressed) DragMove();
        };

        GradeBox.ItemsSource = grades;
        GradeBox.SelectedItem = grades.FirstOrDefault(g => g.Id == model.GradeId)
                                ?? grades.FirstOrDefault();

        NameBox.Text = model.NameAr;
        MaxMarkBox.Text = FormatDecimal(model.MaxMark);
        PassMarkBox.Text = FormatDecimal(model.PassMark);

        Loaded += (_, _) => NameBox.Focus();
    }

    public static bool Show(SubjectSaveModel model, bool isEdit, IReadOnlyList<SubjectGradeOption> grades)
    {
        var owner = Application.Current?.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive)
                    ?? Application.Current?.MainWindow;
        var dlg = new SubjectEditorDialog(model, isEdit, grades) { Owner = owner };
        dlg.ShowDialog();
        return dlg.Result;
    }

    private void OnConfirmClick(object sender, RoutedEventArgs e)
    {
        var name = NameBox.Text?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(name))
        {
            ShowError("الرجاء إدخال اسم المادة.");
            NameBox.Focus();
            return;
        }

        if (GradeBox.SelectedItem is not SubjectGradeOption grade)
        {
            ShowError("الرجاء اختيار الصف.");
            GradeBox.Focus();
            return;
        }

        if (!TryParseDecimal(MaxMarkBox.Text, out var maxMark) || maxMark <= 0m)
        {
            ShowError("الدرجة الكاملة يجب أن تكون رقماً أكبر من صفر.");
            MaxMarkBox.Focus();
            return;
        }

        if (!TryParseDecimal(PassMarkBox.Text, out var passMark) || passMark < 0m)
        {
            ShowError("درجة النجاح يجب أن تكون رقماً 0 فأكثر.");
            PassMarkBox.Focus();
            return;
        }

        if (passMark > maxMark)
        {
            ShowError("درجة النجاح يجب ألا تتجاوز الدرجة الكاملة.");
            PassMarkBox.Focus();
            return;
        }

        _model.NameAr = name;
        _model.GradeId = grade.Id;
        _model.MaxMark = maxMark;
        _model.PassMark = passMark;

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

    private static bool TryParseDecimal(string? raw, out decimal value)
    {
        var text = (raw ?? string.Empty).Trim();
        // Be lenient: accept both invariant '.' and Arabic-locale ',' separators.
        if (decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out value))
            return true;
        return decimal.TryParse(text, NumberStyles.Number, CultureInfo.CurrentCulture, out value);
    }

    private static string FormatDecimal(decimal value)
    {
        // Drop trailing zeros for cleaner UX (100.00 -> "100", 7.50 -> "7.5").
        var trimmed = value.ToString("0.##", CultureInfo.InvariantCulture);
        return trimmed;
    }
}

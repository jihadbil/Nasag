using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Nasag.Repositories;

namespace Nasag.Views.Pages.Exams.Dialogs;

public partial class ExamEditorDialog : Window
{
    public bool Result { get; private set; }

    private readonly ExamSaveModel _model;
    private readonly IReadOnlyList<ExamYearOption> _years;

    private ExamEditorDialog(ExamSaveModel model, bool isEdit, IReadOnlyList<ExamYearOption> years)
    {
        InitializeComponent();
        _model = model;
        _years = years;
        TitleText.Text = isEdit ? "تعديل الامتحان" : "إضافة امتحان";

        MouseLeftButtonDown += (_, _) =>
        {
            if (Mouse.LeftButton == MouseButtonState.Pressed) DragMove();
        };

        YearBox.ItemsSource = years;
        var selectedYear = years.FirstOrDefault(y => y.Id == model.AcademicYearId)
                           ?? years.FirstOrDefault();
        YearBox.SelectedItem = selectedYear;

        NameBox.Text = model.NameAr;
        WeightBox.Text = model.Weight.ToString("0.##", CultureInfo.InvariantCulture);

        Loaded += (_, _) => NameBox.Focus();
    }

    public static bool Show(ExamSaveModel model, bool isEdit, IReadOnlyList<ExamYearOption> years)
    {
        var owner = Application.Current?.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive)
                    ?? Application.Current?.MainWindow;
        var dlg = new ExamEditorDialog(model, isEdit, years) { Owner = owner };
        dlg.ShowDialog();
        return dlg.Result;
    }

    private void OnConfirmClick(object sender, RoutedEventArgs e)
    {
        var name = NameBox.Text?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(name))
        {
            ShowError("الرجاء إدخال اسم الامتحان.");
            NameBox.Focus();
            return;
        }
        if (name.Length > 80)
        {
            ShowError("اسم الامتحان طويل جداً (الحد الأقصى 80 حرفاً).");
            NameBox.Focus();
            return;
        }

        if (YearBox.SelectedItem is not ExamYearOption year)
        {
            ShowError("الرجاء اختيار السنة الدراسية.");
            YearBox.Focus();
            return;
        }

        var weightText = (WeightBox.Text ?? string.Empty)
            .Trim()
            .Replace('٫', '.')
            .Replace('،', '.');

        if (!decimal.TryParse(
                weightText,
                NumberStyles.Number,
                CultureInfo.InvariantCulture,
                out var weight))
        {
            ShowError("الوزن يجب أن يكون رقماً عشرياً.");
            WeightBox.Focus();
            return;
        }
        if (weight < 0.1m || weight > 10m)
        {
            ShowError("الوزن يجب أن يكون بين 0.1 و 10.");
            WeightBox.Focus();
            return;
        }

        _model.NameAr = name;
        _model.AcademicYearId = year.Id;
        _model.Weight = weight;

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

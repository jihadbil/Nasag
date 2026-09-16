using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Nasag.ViewModels.Pages.Settings;

namespace Nasag.Views.Pages.Settings;

/// <summary>
/// Result returned to the caller when the user presses "حفظ" — null when cancelled.
/// </summary>
public sealed record AcademicYearDialogResult(int? Id, string NameAr, DateTime StartDate, DateTime EndDate);

public partial class AcademicYearDialog : Window
{
    public AcademicYearDialogResult? Result { get; private set; }

    private readonly AcademicYearDialogViewModel _vm;

    private AcademicYearDialog(AcademicYearDialogViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        DataContext = vm;

        Loaded += (_, _) =>
        {
            NameBox.Focus();
            NameBox.SelectAll();
        };
    }

    private void OnHeaderDrag(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
        {
            try { DragMove(); }
            catch { /* ignore double-click resize race */ }
        }
    }

    public static AcademicYearDialogResult? ShowCreate()
    {
        var vm = new AcademicYearDialogViewModel
        {
            Title = "إضافة سنة دراسية",
            IsEditing = false
        };
        return ShowCore(vm);
    }

    public static AcademicYearDialogResult? ShowEdit(int yearId, string nameAr, DateTime startDate, DateTime endDate)
    {
        var vm = new AcademicYearDialogViewModel
        {
            Title = "تعديل السنة الدراسية",
            IsEditing = true,
            EditingYearId = yearId,
            NameAr = nameAr,
            StartDate = startDate == default ? DateTime.Today : startDate,
            EndDate = endDate == default ? DateTime.Today.AddMonths(10) : endDate
        };
        return ShowCore(vm);
    }

    private static AcademicYearDialogResult? ShowCore(AcademicYearDialogViewModel vm)
    {
        var owner = Application.Current?.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive)
                    ?? Application.Current?.MainWindow;
        var dlg = new AcademicYearDialog(vm) { Owner = owner };
        dlg.ShowDialog();
        return dlg.Result;
    }

    private void OnConfirmClick(object sender, RoutedEventArgs e)
    {
        var error = _vm.Validate();
        if (error is not null)
        {
            _vm.ErrorMessage = error;
            NameBox.Focus();
            return;
        }

        Result = new AcademicYearDialogResult(
            _vm.EditingYearId,
            _vm.NameAr.Trim(),
            _vm.StartDate.Date,
            _vm.EndDate.Date);
        Close();
    }

    private void OnCancelClick(object sender, RoutedEventArgs e)
    {
        Result = null;
        Close();
    }
}

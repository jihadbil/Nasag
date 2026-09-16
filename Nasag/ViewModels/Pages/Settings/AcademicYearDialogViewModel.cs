using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Nasag.ViewModels.Pages.Settings;

/// <summary>
/// State holder for <see cref="Nasag.Views.Pages.Settings.AcademicYearDialog"/>.
/// The dialog is a simple Window with code-behind, but its fields live on this VM
/// so they participate in WPF binding and the dialog stays free of layout-level code.
/// </summary>
public sealed partial class AcademicYearDialogViewModel : ObservableObject
{
    [ObservableProperty] private string _nameAr = string.Empty;
    [ObservableProperty] private DateTime _startDate = new DateTime(DateTime.Today.Year, 9, 1);
    [ObservableProperty] private DateTime _endDate = new DateTime(DateTime.Today.Year + 1, 6, 30);
    [ObservableProperty] private string? _errorMessage;
    [ObservableProperty] private bool _isEditing;
    [ObservableProperty] private string _title = "إضافة سنة دراسية";

    public int? EditingYearId { get; set; }

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

    partial void OnErrorMessageChanged(string? value) => OnPropertyChanged(nameof(HasError));

    /// <summary>Validates the form. Returns null when valid, an Arabic error otherwise.</summary>
    public string? Validate()
    {
        if (string.IsNullOrWhiteSpace(NameAr))
            return "اسم السنة الدراسية مطلوب.";
        if (NameAr.Trim().Length > 60)
            return "اسم السنة الدراسية طويل جداً (الحد الأقصى 60 حرفاً).";
        if (StartDate == default || EndDate == default)
            return "تاريخا البداية والنهاية مطلوبان.";
        if (EndDate.Date <= StartDate.Date)
            return "تاريخ النهاية يجب أن يكون بعد تاريخ البداية.";
        return null;
    }
}

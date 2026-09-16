using System.Linq;
using System.Windows;
using System.Windows.Input;
using Nasag.Repositories;

namespace Nasag.Views.Pages.Classes.Dialogs;

public partial class SectionEditorDialog : Window
{
    public bool Result { get; private set; }

    private readonly SectionSaveModel _model;

    private SectionEditorDialog(SectionSaveModel model, bool isEdit, string gradeName)
    {
        InitializeComponent();
        _model = model;
        TitleText.Text = isEdit ? "تعديل الشعبة" : "إضافة شعبة";
        GradeNameText.Text = $"الصف: {gradeName}";

        MouseLeftButtonDown += (_, _) =>
        {
            if (Mouse.LeftButton == MouseButtonState.Pressed) DragMove();
        };

        NameBox.Text = model.NameAr;
        CapacityBox.Text = model.Capacity.ToString();

        Loaded += (_, _) => NameBox.Focus();
    }

    public static bool Show(SectionSaveModel model, bool isEdit, string gradeName)
    {
        var owner = Application.Current?.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive)
                    ?? Application.Current?.MainWindow;
        var dlg = new SectionEditorDialog(model, isEdit, gradeName) { Owner = owner };
        dlg.ShowDialog();
        return dlg.Result;
    }

    private void OnConfirmClick(object sender, RoutedEventArgs e)
    {
        var name = NameBox.Text?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(name))
        {
            ShowError("الرجاء إدخال اسم الشعبة.");
            NameBox.Focus();
            return;
        }
        if (!int.TryParse(CapacityBox.Text, out var capacity) || capacity < 1)
        {
            ShowError("السعة يجب أن تكون رقماً صحيحاً 1 فأكثر.");
            CapacityBox.Focus();
            return;
        }

        _model.NameAr = name;
        _model.Capacity = capacity;

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

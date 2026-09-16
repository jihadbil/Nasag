using System.IO;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;

namespace Nasag.Views.Licensing;

public partial class UpdateSourceDialog : Window
{
    public string ResultValue { get; private set; } = "";

    public UpdateSourceDialog(string initialValue)
    {
        InitializeComponent();
        SourceField.Text = initialValue ?? "";
        Loaded += (_, _) =>
        {
            SourceField.Focus();
            SourceField.CaretIndex = SourceField.Text.Length;
        };
    }

    private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left) DragMove();
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void Browse_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFolderDialog
        {
            Title = "اختر مجلد مصدر التحديثات",
            Multiselect = false,
        };
        if (!string.IsNullOrWhiteSpace(SourceField.Text) && Directory.Exists(SourceField.Text))
            dlg.InitialDirectory = SourceField.Text;
        if (dlg.ShowDialog(this) == true)
            SourceField.Text = dlg.FolderName;
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        var value = SourceField.Text?.Trim() ?? "";
        if (string.IsNullOrWhiteSpace(value))
        {
            MessageBox.Show(this, "يجب إدخال مصدر صالح.", "مصدر التحديثات",
                MessageBoxButton.OK, MessageBoxImage.Warning,
                MessageBoxResult.OK, MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign);
            return;
        }
        ResultValue = value;
        DialogResult = true;
        Close();
    }
}

using System.Windows;
using Nasag.ViewModels.Pages.Students;

namespace Nasag.Views.Pages.Students;

public partial class StudentImportWizard : Window
{
    public StudentImportWizard()
    {
        InitializeComponent();
    }

    private void OnDoneClick(object sender, RoutedEventArgs e)
    {
        // Result=true only if the import actually completed successfully so the
        // parent VM can refresh its list.
        var vm = DataContext as StudentImportWizardViewModel;
        DialogResult = vm?.Success == true;
        Close();
    }

    private void OnCloseClick(object sender, RoutedEventArgs e)
    {
        var vm = DataContext as StudentImportWizardViewModel;
        DialogResult = vm?.Confirmed == true && vm.Success;
        Close();
    }
}

using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Nasag.Repositories;
using Nasag.ViewModels.Pages.Subjects;

namespace Nasag.Views.Pages.Subjects;

public partial class SubjectsView : UserControl
{
    public SubjectsView()
    {
        InitializeComponent();
    }

    private void OnGridDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is not SubjectsViewModel vm) return;
        if (vm.SelectedItem is not SubjectRow row) return;
        if (vm.EditSubjectCommand.CanExecute(row))
            vm.EditSubjectCommand.Execute(row);
    }
}

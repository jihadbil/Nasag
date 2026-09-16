using System.Windows.Controls;
using System.Windows.Input;
using Nasag.Repositories;
using Nasag.ViewModels.Pages.Exams;

namespace Nasag.Views.Pages.Exams;

public partial class ExamsView : UserControl
{
    public ExamsView()
    {
        InitializeComponent();
    }

    private void OnGridDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is not ExamsViewModel vm) return;
        if (vm.SelectedRow is not ExamRow row) return;
        if (vm.EditExamCommand.CanExecute(row))
            vm.EditExamCommand.Execute(row);
    }
}

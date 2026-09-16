using System.Windows.Controls;
using System.Windows.Input;
using Nasag.Repositories;
using Nasag.ViewModels.Pages.Students;

namespace Nasag.Views.Pages.Students;

public partial class StudentsView : UserControl
{
    public StudentsView()
    {
        InitializeComponent();

        // Ctrl+F focuses the search box (the rest of the shortcuts are wired
        // declaratively via UserControl.InputBindings in the XAML).
        var ctrlF = new RoutedCommand();
        ctrlF.InputGestures.Add(new KeyGesture(Key.F, ModifierKeys.Control));
        CommandBindings.Add(new CommandBinding(ctrlF, (_, _) => SearchBox.Focus()));
        InputBindings.Add(new InputBinding(ctrlF, new KeyGesture(Key.F, ModifierKeys.Control)));
    }

    private void OnRowDoubleClick(object sender, MouseButtonEventArgs e)
    {
        // Open the editor for the row the user double-clicked.
        if (DataContext is StudentsViewModel vm
            && StudentsGrid.SelectedItem is StudentRow row)
        {
            vm.EditStudentCommand.Execute(row);
        }
    }

    private void OnPageJumpKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter) return;
        if (sender is not ComboBox combo) return;
        if (DataContext is not StudentsViewModel vm) return;

        if (int.TryParse(combo.Text?.Trim(), out var target))
            vm.JumpToPageCommand.Execute(target);
    }

    private void OnPageJumpSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // Item picked from the dropdown — jump immediately without requiring Enter.
        if (sender is not ComboBox combo) return;
        if (combo.SelectedItem is int page
            && DataContext is StudentsViewModel vm)
        {
            vm.JumpToPageCommand.Execute(page);
        }
    }
}

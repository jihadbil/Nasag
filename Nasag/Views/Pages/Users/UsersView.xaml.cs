using System.Windows.Controls;
using System.Windows.Input;
using Nasag.Repositories;
using Nasag.ViewModels.Pages.Users;

namespace Nasag.Views.Pages.Users;

public partial class UsersView : UserControl
{
    public UsersView()
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
        if (DataContext is UsersViewModel vm
            && UsersGrid.SelectedItem is UserListRow row)
        {
            vm.EditCommand.Execute(row);
        }
    }
}

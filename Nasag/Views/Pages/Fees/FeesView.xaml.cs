using System.Windows.Controls;
using System.Windows.Input;

namespace Nasag.Views.Pages.Fees;

public partial class FeesView : UserControl
{
    public FeesView()
    {
        InitializeComponent();

        // Ctrl+F focuses the quick "search by student number" textbox.
        var focusBinding = new KeyBinding(
            new RelayCommandLite(_ =>
            {
                QuickStudentNumberBox.Focus();
                QuickStudentNumberBox.SelectAll();
            }),
            Key.F,
            ModifierKeys.Control);
        InputBindings.Add(focusBinding);
    }

    private sealed class RelayCommandLite : ICommand
    {
        private readonly System.Action<object?> _execute;
        public RelayCommandLite(System.Action<object?> execute) => _execute = execute;
        public bool CanExecute(object? parameter) => true;
        public void Execute(object? parameter) => _execute(parameter);
        public event System.EventHandler? CanExecuteChanged
        {
            add { }
            remove { }
        }
    }
}

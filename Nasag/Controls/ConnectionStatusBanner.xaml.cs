using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Nasag.Controls;

public partial class ConnectionStatusBanner : UserControl
{
    public ConnectionStatusBanner()
    {
        InitializeComponent();
    }

    public static readonly DependencyProperty IsDisconnectedProperty =
        DependencyProperty.Register(nameof(IsDisconnected), typeof(bool), typeof(ConnectionStatusBanner),
            new PropertyMetadata(false, OnIsDisconnectedChanged));
    public bool IsDisconnected
    {
        get => (bool)GetValue(IsDisconnectedProperty);
        set => SetValue(IsDisconnectedProperty, value);
    }

    public static readonly DependencyProperty MessageProperty =
        DependencyProperty.Register(nameof(Message), typeof(string), typeof(ConnectionStatusBanner),
            new PropertyMetadata("تعذّر الاتصال بقاعدة البيانات. يرجى التحقق من الخادم."));
    public string Message
    {
        get => (string)GetValue(MessageProperty);
        set => SetValue(MessageProperty, value);
    }

    public static readonly DependencyProperty RetryCommandProperty =
        DependencyProperty.Register(nameof(RetryCommand), typeof(ICommand), typeof(ConnectionStatusBanner),
            new PropertyMetadata(null));
    public ICommand? RetryCommand
    {
        get => (ICommand?)GetValue(RetryCommandProperty);
        set => SetValue(RetryCommandProperty, value);
    }

    private static void OnIsDisconnectedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ConnectionStatusBanner banner)
            banner.Visibility = (bool)e.NewValue ? Visibility.Visible : Visibility.Collapsed;
    }

    private void OnRetryClick(object sender, RoutedEventArgs e)
    {
        if (RetryCommand?.CanExecute(null) == true)
            RetryCommand.Execute(null);
    }
}

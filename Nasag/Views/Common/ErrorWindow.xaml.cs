using System;
using System.Windows;

namespace Nasag.Views.Common;

public partial class ErrorWindow : Window
{
    private readonly string _details;

    public ErrorWindow(string title, string userMessage, string details)
    {
        InitializeComponent();
        TitleText.Text = title;
        UserMessageText.Text = userMessage;
        DetailsBox.Text = details;
        _details = details;
    }

    private void OnCopyClick(object sender, RoutedEventArgs e)
    {
        try
        {
            Clipboard.SetText(_details);
            CopyButtonText.Text = "تم النسخ";
        }
        catch
        {
            CopyButtonText.Text = "تعذّر النسخ";
        }
    }

    private void OnCloseClick(object sender, RoutedEventArgs e) => Close();
}

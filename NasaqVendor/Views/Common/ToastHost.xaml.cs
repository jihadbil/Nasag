using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using NasaqVendor.Services;

namespace NasaqVendor.Views.Common;

public partial class ToastHost : UserControl
{
    public ToastHost()
    {
        InitializeComponent();
    }

    public void ShowToast(string message, ToastKind kind)
    {
        var (bg, stroke) = kind switch
        {
            ToastKind.Success => ((Brush)FindResource("SuccessBrush"), (Brush)FindResource("SuccessBrush")),
            ToastKind.Warning => ((Brush)FindResource("WarningBrush"), (Brush)FindResource("WarningBrush")),
            ToastKind.Danger  => ((Brush)FindResource("DangerBrush"),  (Brush)FindResource("DangerBrush")),
            _                 => ((Brush)FindResource("NavyDeepBrush"), (Brush)FindResource("NavyDeepBrush")),
        };

        var card = new Border
        {
            Background = bg,
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(16, 10, 16, 10),
            Margin = new Thickness(0, 6, 0, 0),
            MinWidth = 220,
            MaxWidth = 460,
            Effect = new System.Windows.Media.Effects.DropShadowEffect
            {
                BlurRadius = 18,
                ShadowDepth = 0,
                Opacity = 0.25
            }
        };

        var text = new TextBlock
        {
            Text = message,
            Foreground = Brushes.White,
            FontFamily = (FontFamily)FindResource("TajawalFont"),
            FontSize = (double)FindResource("FontSizeBase"),
            TextWrapping = TextWrapping.Wrap
        };
        card.Child = text;

        ToastList.Items.Add(card);

        var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(4) };
        timer.Tick += (s, e) =>
        {
            timer.Stop();
            ToastList.Items.Remove(card);
        };
        timer.Start();
    }
}

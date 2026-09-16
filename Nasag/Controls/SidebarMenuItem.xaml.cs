using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Nasag.Controls;

public partial class SidebarMenuItem : UserControl
{
    private static readonly Brush DefaultContentBrush = new SolidColorBrush(Color.FromRgb(0x94, 0xA3, 0xB8));
    private static readonly Brush ActiveContentBrush = Brushes.White;
    private static readonly Brush ActiveBackgroundBrush = new SolidColorBrush(Color.FromArgb(0x24, 0x0D, 0x94, 0x88));
    private static readonly Brush HoverBackgroundBrush = new SolidColorBrush(Color.FromArgb(0x18, 0xFF, 0xFF, 0xFF));

    public SidebarMenuItem()
    {
        InitializeComponent();
        ContentBrush = DefaultContentBrush;
        ApplyLayoutState();
        MouseLeftButtonUp += OnMouseLeftButtonUp;
        MouseEnter += (_, _) => ApplyVisualState();
        MouseLeave += (_, _) => ApplyVisualState();
    }

    public static readonly DependencyProperty LabelProperty =
        DependencyProperty.Register(nameof(Label), typeof(string), typeof(SidebarMenuItem),
            new PropertyMetadata(string.Empty, OnLabelChanged));
    public string Label
    {
        get => (string)GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    public static readonly DependencyProperty IconGeometryProperty =
        DependencyProperty.Register(nameof(IconGeometry), typeof(Geometry), typeof(SidebarMenuItem), new PropertyMetadata(null));
    public Geometry? IconGeometry
    {
        get => (Geometry?)GetValue(IconGeometryProperty);
        set => SetValue(IconGeometryProperty, value);
    }

    public static readonly DependencyProperty IsActiveProperty =
        DependencyProperty.Register(nameof(IsActive), typeof(bool), typeof(SidebarMenuItem),
            new PropertyMetadata(false, OnIsActiveChanged));
    public bool IsActive
    {
        get => (bool)GetValue(IsActiveProperty);
        set => SetValue(IsActiveProperty, value);
    }

    public static readonly DependencyProperty IsCollapsedProperty =
        DependencyProperty.Register(nameof(IsCollapsed), typeof(bool), typeof(SidebarMenuItem),
            new PropertyMetadata(false, OnIsCollapsedChanged));
    public bool IsCollapsed
    {
        get => (bool)GetValue(IsCollapsedProperty);
        set => SetValue(IsCollapsedProperty, value);
    }

    public static readonly DependencyProperty ContentBrushProperty =
        DependencyProperty.Register(nameof(ContentBrush), typeof(Brush), typeof(SidebarMenuItem),
            new PropertyMetadata(DefaultContentBrush));
    public Brush ContentBrush
    {
        get => (Brush)GetValue(ContentBrushProperty);
        set => SetValue(ContentBrushProperty, value);
    }

    public static readonly DependencyProperty CommandProperty =
        DependencyProperty.Register(nameof(Command), typeof(ICommand), typeof(SidebarMenuItem), new PropertyMetadata(null));
    public ICommand? Command
    {
        get => (ICommand?)GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }

    public static readonly DependencyProperty CommandParameterProperty =
        DependencyProperty.Register(nameof(CommandParameter), typeof(object), typeof(SidebarMenuItem), new PropertyMetadata(null));
    public object? CommandParameter
    {
        get => GetValue(CommandParameterProperty);
        set => SetValue(CommandParameterProperty, value);
    }

    private static void OnIsActiveChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is SidebarMenuItem item) item.ApplyVisualState();
    }

    private static void OnLabelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is SidebarMenuItem item) item.ApplyLayoutState();
    }

    private static void OnIsCollapsedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is SidebarMenuItem item) item.ApplyLayoutState();
    }

    private void ApplyLayoutState()
    {
        LabelText.Visibility = IsCollapsed ? Visibility.Collapsed : Visibility.Visible;
        LabelText.Margin = IsCollapsed ? new Thickness(0) : new Thickness(0, 0, 12, 0);
        ItemBorder.Padding = IsCollapsed ? new Thickness(10) : new Thickness(14, 10, 14, 10);
        ToolTip = IsCollapsed ? Label : null;
    }

    private void ApplyVisualState()
    {
        if (IsActive)
        {
            ItemBorder.Background = ActiveBackgroundBrush;
            ActiveStrip.Visibility = Visibility.Visible;
            ContentBrush = ActiveContentBrush;
        }
        else if (IsMouseOver)
        {
            ItemBorder.Background = HoverBackgroundBrush;
            ActiveStrip.Visibility = Visibility.Collapsed;
            ContentBrush = ActiveContentBrush;
        }
        else
        {
            ItemBorder.Background = Brushes.Transparent;
            ActiveStrip.Visibility = Visibility.Collapsed;
            ContentBrush = DefaultContentBrush;
        }
    }

    private void OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (Command?.CanExecute(CommandParameter) == true)
            Command.Execute(CommandParameter);
    }
}

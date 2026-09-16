using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Nasag.Controls;

/// <summary>
/// Searchable, clearable, professional combobox.
/// - Type to filter suggestions live (case-insensitive contains).
/// - DisplayMemberPath chooses which property to render and filter on.
/// - SelectedItem is the canonical bound value; setting it to null clears the field.
/// - Arrow keys + Enter navigate suggestions, Escape closes the popup.
/// - The popup width matches the field width; suggestions appear immediately on focus.
/// </summary>
public partial class SearchableComboBox : UserControl
{
    public static readonly DependencyProperty ItemsSourceProperty =
        DependencyProperty.Register(nameof(ItemsSource), typeof(IEnumerable), typeof(SearchableComboBox),
            new FrameworkPropertyMetadata(null, OnItemsSourceChanged));

    public static readonly DependencyProperty SelectedItemProperty =
        DependencyProperty.Register(nameof(SelectedItem), typeof(object), typeof(SearchableComboBox),
            new FrameworkPropertyMetadata(null,
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSelectedItemChanged));

    public static readonly DependencyProperty DisplayMemberPathProperty =
        DependencyProperty.Register(nameof(DisplayMemberPath), typeof(string), typeof(SearchableComboBox),
            new PropertyMetadata(null, OnDisplayMemberPathChanged));

    public static readonly DependencyProperty PlaceholderProperty =
        DependencyProperty.Register(nameof(Placeholder), typeof(string), typeof(SearchableComboBox),
            new PropertyMetadata("اختر…", OnPlaceholderChanged));

    public IEnumerable? ItemsSource
    {
        get => (IEnumerable?)GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    public object? SelectedItem
    {
        get => GetValue(SelectedItemProperty);
        set => SetValue(SelectedItemProperty, value);
    }

    public string? DisplayMemberPath
    {
        get => (string?)GetValue(DisplayMemberPathProperty);
        set => SetValue(DisplayMemberPathProperty, value);
    }

    public string Placeholder
    {
        get => (string)GetValue(PlaceholderProperty);
        set => SetValue(PlaceholderProperty, value);
    }

    private bool _suppressTextSync;
    private readonly List<object> _allItems = new();
    private INotifyCollectionChanged? _observedCollection;

    public SearchableComboBox()
    {
        InitializeComponent();
        SuggestionList.PreviewMouseWheel += OnSuggestionMouseWheel;
        IsKeyboardFocusWithinChanged += OnKeyboardFocusWithinChanged;
    }

    private static void OnItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var ctl = (SearchableComboBox)d;
        if (ctl._observedCollection is not null)
            ctl._observedCollection.CollectionChanged -= ctl.OnCollectionChanged;
        ctl._observedCollection = e.NewValue as INotifyCollectionChanged;
        if (ctl._observedCollection is not null)
            ctl._observedCollection.CollectionChanged += ctl.OnCollectionChanged;

        ctl.RefreshAllItems();
        ctl.SyncTextFromSelectedItem();
    }

    private static void OnSelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var ctl = (SearchableComboBox)d;
        ctl.SyncTextFromSelectedItem();
    }

    private static void OnDisplayMemberPathChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var ctl = (SearchableComboBox)d;
        ctl.RebuildSuggestionTemplate();
        ctl.SyncTextFromSelectedItem();
    }

    private static void OnPlaceholderChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var ctl = (SearchableComboBox)d;
        ctl.PlaceholderText.Text = (string)e.NewValue;
    }

    private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        RefreshAllItems();
        if (DropdownPopup.IsOpen) ApplyFilter(QueryBox.Text);
    }

    private void RefreshAllItems()
    {
        _allItems.Clear();
        if (ItemsSource is not null)
        {
            foreach (var item in ItemsSource)
            {
                if (item is not null) _allItems.Add(item);
            }
        }
    }

    private void RebuildSuggestionTemplate()
    {
        // Use a runtime DataTemplate that renders the configured property,
        // falling back to ToString() when no path was set.
        if (string.IsNullOrEmpty(DisplayMemberPath))
        {
            SuggestionList.ItemTemplate = null;
            SuggestionList.DisplayMemberPath = null;
        }
        else
        {
            SuggestionList.ItemTemplate = null;
            SuggestionList.DisplayMemberPath = DisplayMemberPath;
        }
    }

    private string GetDisplayText(object? item)
    {
        if (item is null) return string.Empty;
        if (string.IsNullOrEmpty(DisplayMemberPath)) return item.ToString() ?? string.Empty;
        var prop = item.GetType().GetProperty(DisplayMemberPath, BindingFlags.Public | BindingFlags.Instance);
        return prop?.GetValue(item)?.ToString() ?? string.Empty;
    }

    private void SyncTextFromSelectedItem()
    {
        _suppressTextSync = true;
        try
        {
            QueryBox.Text = GetDisplayText(SelectedItem);
            UpdatePlaceholder();
            UpdateClearButton();
        }
        finally
        {
            _suppressTextSync = false;
        }
    }

    private void UpdatePlaceholder()
    {
        PlaceholderText.Visibility = string.IsNullOrEmpty(QueryBox.Text)
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    private void UpdateClearButton()
    {
        ClearButton.Visibility = SelectedItem is not null || !string.IsNullOrEmpty(QueryBox.Text)
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    private void OnQueryTextChanged(object sender, TextChangedEventArgs e)
    {
        UpdatePlaceholder();
        UpdateClearButton();
        if (_suppressTextSync) return;

        // Free-typing while a value is selected effectively clears the selection
        // until a new suggestion is committed.
        if (SelectedItem is not null && GetDisplayText(SelectedItem) != QueryBox.Text)
            SetCurrentValue(SelectedItemProperty, null);

        ApplyFilter(QueryBox.Text);
        if (!DropdownPopup.IsOpen) DropdownPopup.IsOpen = true;
    }

    private void OnQueryGotFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        ApplyFilter(QueryBox.Text);
        DropdownPopup.IsOpen = true;
        QueryBox.SelectAll();
    }

    private void OnQueryKeyDown(object sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Down:
                if (!DropdownPopup.IsOpen) DropdownPopup.IsOpen = true;
                MoveSelection(+1);
                e.Handled = true;
                break;
            case Key.Up:
                MoveSelection(-1);
                e.Handled = true;
                break;
            case Key.Enter:
                CommitFromList();
                e.Handled = true;
                break;
            case Key.Escape:
                DropdownPopup.IsOpen = false;
                e.Handled = true;
                break;
        }
    }

    private void OnSuggestionKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            CommitFromList();
            e.Handled = true;
        }
        else if (e.Key == Key.Escape)
        {
            DropdownPopup.IsOpen = false;
            QueryBox.Focus();
            e.Handled = true;
        }
    }

    private void OnSuggestionMouseWheel(object sender, MouseWheelEventArgs e)
    {
        // Allow scrolling without dismissing the popup.
        if (sender is ListBox lb && lb.Items.Count > 0) e.Handled = false;
    }

    private void OnSuggestionClicked(object sender, MouseButtonEventArgs e)
    {
        // Walk up to the ListBoxItem so we commit reliably even when the click
        // lands on a TextBlock / ContentPresenter / padding inside the row.
        var src = e.OriginalSource as DependencyObject;
        while (src is not null && src is not ListBoxItem)
            src = VisualTreeHelper.GetParent(src);
        if (src is ListBoxItem lbi && lbi.DataContext is { } item && SuggestionList.Items.Contains(item))
        {
            Commit(item);
            e.Handled = true;
        }
    }

    private void OnClearClick(object sender, RoutedEventArgs e)
    {
        QueryBox.Text = string.Empty;
        SetCurrentValue(SelectedItemProperty, null);
        UpdatePlaceholder();
        UpdateClearButton();
        QueryBox.Focus();
    }

    private void OnDropDownToggle(object sender, MouseButtonEventArgs e)
    {
        DropdownPopup.IsOpen = !DropdownPopup.IsOpen;
        if (DropdownPopup.IsOpen)
        {
            QueryBox.Focus();
            ApplyFilter(QueryBox.Text);
        }
        e.Handled = true;
    }

    private void OnDropdownClosed(object? sender, EventArgs e)
    {
        if (!IsKeyboardFocusWithin)
            SyncTextFromSelectedItem();
    }

    private void OnKeyboardFocusWithinChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (!(bool)e.NewValue)
        {
            // Lost focus → if user typed text that doesn't match a real item,
            // snap back to the selected item (or clear if nothing selected).
            SyncTextFromSelectedItem();
            DropdownPopup.IsOpen = false;
        }
    }

    private void MoveSelection(int delta)
    {
        if (SuggestionList.Items.Count == 0) return;
        var idx = SuggestionList.SelectedIndex + delta;
        idx = Math.Max(0, Math.Min(SuggestionList.Items.Count - 1, idx));
        SuggestionList.SelectedIndex = idx;
        SuggestionList.ScrollIntoView(SuggestionList.SelectedItem);
    }

    private void CommitFromList()
    {
        if (SuggestionList.SelectedItem is null && SuggestionList.Items.Count > 0)
            SuggestionList.SelectedIndex = 0;
        Commit(SuggestionList.SelectedItem);
    }

    private void Commit(object? item)
    {
        SetCurrentValue(SelectedItemProperty, item);
        DropdownPopup.IsOpen = false;
        SyncTextFromSelectedItem();
    }

    private void ApplyFilter(string? query)
    {
        IEnumerable<object> filtered;
        if (string.IsNullOrWhiteSpace(query))
        {
            filtered = _allItems;
        }
        else
        {
            var q = query.Trim();
            filtered = _allItems
                .Where(it => GetDisplayText(it).IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0)
                .ToList();
        }

        SuggestionList.ItemsSource = filtered;
        var hasItems = filtered.Any();
        SuggestionList.Visibility = hasItems ? Visibility.Visible : Visibility.Collapsed;
        EmptyPanel.Visibility = hasItems ? Visibility.Collapsed : Visibility.Visible;
    }
}

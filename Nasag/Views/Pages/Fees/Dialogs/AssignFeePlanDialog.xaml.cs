using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Nasag.Controls;
using Nasag.Repositories;

namespace Nasag.Views.Pages.Fees.Dialogs;

public sealed record AssignFeePlanResult(int FeePlanId);

public partial class AssignFeePlanDialog : Window
{
    public AssignFeePlanResult? Result { get; private set; }

    private AssignFeePlanDialog(string studentName, string gradeName, IReadOnlyList<FeePlanOption> plans)
    {
        InitializeComponent();

        StudentSummaryText.Text = $"{studentName} — {gradeName}";

        if (plans.Count == 0)
        {
            PlansAvailablePanel.Visibility = Visibility.Collapsed;
            NoPlansPanel.Visibility = Visibility.Visible;
            ConfirmButton.IsEnabled = false;
            UpdateSummary(null);
        }
        else
        {
            PlansAvailablePanel.Visibility = Visibility.Visible;
            NoPlansPanel.Visibility = Visibility.Collapsed;

            PlanBox.ItemsSource = plans;
            PlanBox.SelectedItem = plans[0];
            UpdateSummary(plans[0]);

            var descriptor = DependencyPropertyDescriptor.FromProperty(
                SearchableComboBox.SelectedItemProperty,
                typeof(SearchableComboBox));
            descriptor?.AddValueChanged(PlanBox, (_, _) =>
            {
                UpdateSummary(PlanBox.SelectedItem as FeePlanOption);
            });
        }
    }

    private void OnHeaderDrag(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
        {
            try { DragMove(); }
            catch { /* ignore double-click resize race */ }
        }
    }

    private void UpdateSummary(FeePlanOption? option)
    {
        if (option is null)
        {
            PlanTotalText.Text = "—";
            PlanInstallmentsText.Text = "—";
            return;
        }

        PlanTotalText.Text = Helpers.MoneyFormatter.Format(option.TotalAmount);
        PlanInstallmentsText.Text = option.InstallmentsCount.ToString(CultureInfo.InvariantCulture) + " قسط";
    }

    public static AssignFeePlanResult? Show(string studentName, string gradeName, IReadOnlyList<FeePlanOption> plans)
    {
        var owner = Application.Current?.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive)
                    ?? Application.Current?.MainWindow;
        var dlg = new AssignFeePlanDialog(studentName, gradeName, plans) { Owner = owner };
        dlg.ShowDialog();
        return dlg.Result;
    }

    private void OnConfirmClick(object sender, RoutedEventArgs e)
    {
        ErrorBanner.Visibility = Visibility.Collapsed;

        if (PlanBox.SelectedItem is not FeePlanOption picked)
        {
            ShowError("الرجاء اختيار خطة الرسوم.");
            PlanBox.Focus();
            return;
        }

        Result = new AssignFeePlanResult(picked.Id);
        Close();
    }

    private void OnCancelClick(object sender, RoutedEventArgs e)
    {
        Result = null;
        Close();
    }

    private void ShowError(string message)
    {
        ErrorText.Text = message;
        ErrorBanner.Visibility = Visibility.Visible;
    }
}

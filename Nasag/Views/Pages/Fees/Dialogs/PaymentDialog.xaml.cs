using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Nasag.Controls;
using Nasag.Models;
using Nasag.Repositories;
using Nasag.ViewModels.Pages.Fees;

namespace Nasag.Views.Pages.Fees.Dialogs;

public sealed record PaymentMethodOption(PaymentMethod Method, string DisplayName);

public partial class PaymentDialog : Window
{
    private readonly int _studentFeeId;
    private readonly decimal _studentRemaining;
    private readonly IReadOnlyList<InstallmentChoice> _installments;
    private readonly int _userId;
    private static readonly InstallmentChoice NoneChoice = new(-1, 0, 0m, "بدون قسط محدد");

    public PaymentSaveModel? Result { get; private set; }

    private PaymentDialog(
        int studentFeeId,
        string studentName,
        decimal studentRemaining,
        IReadOnlyList<InstallmentChoice> installments,
        int? preselectedInstallmentId,
        int userId)
    {
        InitializeComponent();
        _studentFeeId = studentFeeId;
        _studentRemaining = studentRemaining;
        _installments = installments;
        _userId = userId;

        StudentSummaryText.Text = studentName;
        RemainingSummaryText.Text = $"المتبقي على الطالب: {FormatCurrency(studentRemaining)}";

        // Payment method options
        var methods = new List<PaymentMethodOption>
        {
            new(PaymentMethod.Cash, "نقدي"),
            new(PaymentMethod.BankTransfer, "تحويل بنكي"),
            new(PaymentMethod.Card, "بطاقة"),
            new(PaymentMethod.Cheque, "شيك"),
            new(PaymentMethod.Other, "أخرى"),
        };
        MethodBox.ItemsSource = methods;
        MethodBox.SelectedItem = methods[0];

        // Installment choices: prepend the "none" option
        var choices = new List<InstallmentChoice> { NoneChoice };
        choices.AddRange(installments);
        InstallmentBox.ItemsSource = choices;

        InstallmentChoice? preselected = null;
        if (preselectedInstallmentId.HasValue)
            preselected = installments.FirstOrDefault(i => i.Id == preselectedInstallmentId.Value);

        InstallmentBox.SelectedItem = preselected ?? NoneChoice;
        UpdateInstallmentSummary(preselected);

        var descriptor = DependencyPropertyDescriptor.FromProperty(
            SearchableComboBox.SelectedItemProperty,
            typeof(SearchableComboBox));
        descriptor?.AddValueChanged(InstallmentBox, (_, _) =>
        {
            var picked = InstallmentBox.SelectedItem as InstallmentChoice;
            var actual = picked is null || picked.Id < 0 ? null : picked;
            UpdateInstallmentSummary(actual);

            // When the user changes the linked installment, refresh AmountBox so the
            // suggested amount tracks the new installment's remaining balance.
            if (actual is null)
            {
                // Switched back to "no installment": suggest the student's remaining.
                AmountBox.Text = FormatAmountForEdit(_studentRemaining);
            }
            else
            {
                AmountBox.Text = FormatAmountForEdit(actual.Remaining);
            }
            AmountBox.SelectAll();
        });

        DateBox.SelectedDate = DateTime.Now;

        // Suggest the remaining for the preselected installment, else the student remaining
        var suggested = preselected?.Remaining ?? studentRemaining;
        AmountBox.Text = FormatAmountForEdit(suggested);

        Loaded += (_, _) =>
        {
            AmountBox.Focus();
            AmountBox.SelectAll();
        };
    }

    private void OnHeaderDrag(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
        {
            try { DragMove(); }
            catch { /* ignore double-click resize race */ }
        }
    }

    private void UpdateInstallmentSummary(InstallmentChoice? choice)
    {
        if (choice is null)
        {
            InstallmentRemainingText.Visibility = Visibility.Collapsed;
            InstallmentRemainingText.Text = string.Empty;
        }
        else
        {
            InstallmentRemainingText.Text = $"متبقي القسط {choice.Number}: {FormatCurrency(choice.Remaining)}";
            InstallmentRemainingText.Visibility = Visibility.Visible;
        }
    }

    private static string FormatAmountForEdit(decimal value)
        => value.ToString("0.##", CultureInfo.InvariantCulture);

    private static string? TryParseAmount(string raw, out decimal amount)
    {
        amount = 0m;
        if (string.IsNullOrWhiteSpace(raw)) return null;
        var cleaned = raw
            .Replace(",", string.Empty)
            .Replace("٬", string.Empty)
            .Replace(" ", string.Empty)
            .Replace('٫', '.')
            .Replace('،', '.');
        if (decimal.TryParse(cleaned, NumberStyles.Number, CultureInfo.InvariantCulture, out amount))
            return null;
        return "المبلغ يجب أن يكون رقماً.";
    }

    private void OnAmountGotFocus(object sender, RoutedEventArgs e)
    {
        // Strip thousand separators while the user edits for cleaner typing.
        if (TryParseAmount(AmountBox.Text, out var amount) is null && amount > 0m)
            AmountBox.Text = FormatAmountForEdit(amount);
    }

    private void OnAmountLostFocus(object sender, RoutedEventArgs e)
    {
        // Pretty-print the amount with thousand separators after edit completes.
        if (TryParseAmount(AmountBox.Text, out var amount) is null && amount > 0m)
        {
            AmountBox.Text = amount.ToString("N2", CultureInfo.InvariantCulture);
        }
    }

    public static PaymentSaveModel? Show(
        int studentFeeId,
        string studentName,
        decimal studentRemaining,
        IReadOnlyList<InstallmentChoice> installments,
        int? preselectedInstallmentId,
        int userId)
    {
        var owner = Application.Current?.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive)
                    ?? Application.Current?.MainWindow;
        var dlg = new PaymentDialog(
            studentFeeId,
            studentName,
            studentRemaining,
            installments,
            preselectedInstallmentId,
            userId)
        {
            Owner = owner
        };
        dlg.ShowDialog();
        return dlg.Result;
    }

    private void OnConfirmClick(object sender, RoutedEventArgs e)
    {
        ErrorBanner.Visibility = Visibility.Collapsed;

        var parseError = TryParseAmount(AmountBox.Text ?? string.Empty, out var amount);
        if (parseError is not null)
        {
            ShowError(parseError);
            AmountBox.Focus();
            return;
        }

        if (amount <= 0m)
        {
            ShowError("المبلغ يجب أن يكون أكبر من صفر.");
            AmountBox.Focus();
            return;
        }

        if (amount > _studentRemaining)
        {
            ShowError($"المبلغ أكبر من المتبقي على الطالب ({FormatCurrency(_studentRemaining)}).");
            AmountBox.Focus();
            return;
        }

        if (MethodBox.SelectedItem is not PaymentMethodOption method)
        {
            ShowError("الرجاء اختيار طريقة الدفع.");
            MethodBox.Focus();
            return;
        }

        var date = DateBox.SelectedDate ?? DateTime.Now;
        if (date.Date > DateTime.Today)
        {
            ShowError("تاريخ الدفع لا يمكن أن يكون في المستقبل.");
            DateBox.Focus();
            return;
        }

        int? installmentId = null;
        if (InstallmentBox.SelectedItem is InstallmentChoice picked && picked.Id > 0)
        {
            if (amount > picked.Remaining)
            {
                ShowError($"المبلغ أكبر من متبقي القسط {picked.Number} ({FormatCurrency(picked.Remaining)}).");
                AmountBox.Focus();
                return;
            }
            installmentId = picked.Id;
        }

        var notes = string.IsNullOrWhiteSpace(NotesBox.Text) ? null : NotesBox.Text.Trim();

        Result = new PaymentSaveModel
        {
            StudentFeeId = _studentFeeId,
            InstallmentId = installmentId,
            Amount = amount,
            PaymentDate = date.ToUniversalTime(),
            Method = method.Method,
            Notes = notes,
            UserId = _userId,
        };

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

    private static string FormatCurrency(decimal value)
        => Helpers.MoneyFormatter.Format(value);
}

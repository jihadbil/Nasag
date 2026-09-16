using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NasaqVendor.Models;

namespace NasaqVendor.ViewModels.Dialogs;

public partial class AuditLogViewModel : ObservableObject
{
    [ObservableProperty] private string headerText = "";

    public ObservableCollection<IssueAudit> Items { get; } = new();

    public event EventHandler<bool>? RequestClose;

    public AuditLogViewModel(LicenseRecord rec, IReadOnlyList<IssueAudit> rows)
    {
        HeaderText = $"سجل التدقيق — الترخيص #{rec.Id} ({rec.CustomerName})";
        foreach (var r in rows) Items.Add(r);
    }

    [RelayCommand]
    private void Close() => RequestClose?.Invoke(this, true);
}

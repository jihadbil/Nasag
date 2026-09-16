using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NasaqVendor.Models;
using NasaqVendor.Repositories;

namespace NasaqVendor.ViewModels.Dialogs;

public partial class CustomerEditorViewModel : ObservableObject
{
    private readonly ICustomersRepository _repo;

    [ObservableProperty] private int customerId;
    [ObservableProperty] private string code = "";
    [ObservableProperty] private string name = "";
    [ObservableProperty] private string? phone;
    [ObservableProperty] private string? email;
    [ObservableProperty] private string? city;
    [ObservableProperty] private string? notes;
    [ObservableProperty] private bool isEditing;
    [ObservableProperty] private string title = "إضافة عميل";
    [ObservableProperty] private string? errorMessage;

    public event EventHandler<bool>? RequestClose;

    public CustomerEditorViewModel(ICustomersRepository repo)
    {
        _repo = repo;
    }

    public async Task PrepareForAddAsync()
    {
        IsEditing = false;
        Title = "إضافة عميل";
        Code = await _repo.NextCodeAsync();
    }

    public async Task PrepareForEditAsync(int id)
    {
        IsEditing = true;
        Title = "تعديل عميل";
        var c = await _repo.GetByIdAsync(id);
        if (c is null) throw new InvalidOperationException("العميل غير موجود.");
        CustomerId = c.Id;
        Code = c.Code;
        Name = c.Name;
        Phone = c.Phone;
        Email = c.Email;
        City = c.City;
        Notes = c.Notes;
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        ErrorMessage = null;
        if (string.IsNullOrWhiteSpace(Name))
        {
            ErrorMessage = "اسم العميل مطلوب.";
            return;
        }
        if (string.IsNullOrWhiteSpace(Code))
            Code = await _repo.NextCodeAsync();

        try
        {
            if (IsEditing)
            {
                await _repo.UpdateAsync(new Customer
                {
                    Id = CustomerId,
                    Code = Code.Trim(),
                    Name = Name.Trim(),
                    Phone = Phone?.Trim(),
                    Email = Email?.Trim(),
                    City = City?.Trim(),
                    Notes = Notes?.Trim()
                });
            }
            else
            {
                await _repo.InsertAsync(new Customer
                {
                    Code = Code.Trim(),
                    Name = Name.Trim(),
                    Phone = Phone?.Trim(),
                    Email = Email?.Trim(),
                    City = City?.Trim(),
                    Notes = Notes?.Trim(),
                    CreatedAtUtc = DateTime.UtcNow
                });
            }
            RequestClose?.Invoke(this, true);
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    [RelayCommand]
    private void Cancel() => RequestClose?.Invoke(this, false);
}

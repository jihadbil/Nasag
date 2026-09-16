using System;
using System.IO;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NasaqVendor.Services;

namespace NasaqVendor.ViewModels.Dialogs;

public partial class InitialKeySetupViewModel : ObservableObject
{
    private readonly IIssuerKeyService _keys;
    private readonly IFileService _files;

    [ObservableProperty] private string? errorMessage;
    [ObservableProperty] private string? successMessage;
    [ObservableProperty] private bool isBusy;

    public event EventHandler<bool>? RequestClose;

    public InitialKeySetupViewModel(IIssuerKeyService keys, IFileService files)
    {
        _keys = keys;
        _files = files;
    }

    [RelayCommand]
    private async Task GenerateAsync()
    {
        ErrorMessage = null;
        SuccessMessage = null;
        try
        {
            IsBusy = true;
            await _keys.GenerateNewKeyPairAsync();

            var path = _files.SaveFile(
                "حفظ المفتاح العام للتضمين في تطبيق Nasag",
                "issuer.public.key",
                "ملفات المفتاح (*.key)|*.key|كل الملفات (*.*)|*.*",
                ".key");
            if (!string.IsNullOrEmpty(path))
            {
                await File.WriteAllBytesAsync(path, _keys.GetPublicKeyBlob());
                SuccessMessage = $"تم توليد المفاتيح وحفظ المفتاح العام في:\n{path}\n\nضع هذا الملف داخل Nasag/Resources/issuer.public.key وضمِّنه كـ EmbeddedResource.";
            }
            else
            {
                SuccessMessage = "تم توليد المفاتيح. تذكّر تصدير المفتاح العام لاحقاً من «إعدادات المفتاح».";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task ImportAsync()
    {
        ErrorMessage = null;
        SuccessMessage = null;
        var path = _files.OpenFile(
            "اختر ملف المفتاح الخاص",
            "ملفات المفتاح (*.key;*.bin)|*.key;*.bin|كل الملفات (*.*)|*.*");
        if (string.IsNullOrEmpty(path)) return;

        try
        {
            IsBusy = true;
            var bytes = await File.ReadAllBytesAsync(path);
            await _keys.ImportPrivateKeyAsync(bytes);
            SuccessMessage = "تم استيراد المفتاح الخاص بنجاح.";
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void Done() => RequestClose?.Invoke(this, _keys.HasKey);

    [RelayCommand]
    private void Skip() => RequestClose?.Invoke(this, false);
}

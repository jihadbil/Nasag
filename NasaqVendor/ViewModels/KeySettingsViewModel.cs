using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NasaqVendor.Services;

namespace NasaqVendor.ViewModels;

public partial class KeySettingsViewModel : ObservableObject
{
    private readonly IIssuerKeyService _keys;
    private readonly IDialogService _dialogs;
    private readonly IToastService _toasts;
    private readonly IFileService _files;

    [ObservableProperty] private string? publicKeyBase64;
    [ObservableProperty] private string? publicKeyFingerprint;
    [ObservableProperty] private bool hasKey;
    [ObservableProperty] private string privateKeyFilePath = "";

    public KeySettingsViewModel(
        IIssuerKeyService keys,
        IDialogService dialogs,
        IToastService toasts,
        IFileService files)
    {
        _keys = keys;
        _dialogs = dialogs;
        _toasts = toasts;
        _files = files;
        _keys.KeyChanged += (_, _) => Refresh();
        Refresh();
    }

    public void Refresh()
    {
        HasKey = _keys.HasKey;
        PublicKeyBase64 = _keys.PublicKeyBase64;
        PublicKeyFingerprint = _keys.PublicKeyFingerprint;
        PrivateKeyFilePath = _keys.PrivateKeyFilePath;
    }

    [RelayCommand]
    private void CopyPublicKey()
    {
        if (string.IsNullOrEmpty(PublicKeyBase64)) return;
        try
        {
            Clipboard.SetText(PublicKeyBase64);
            _toasts.Show("تم نسخ المفتاح العام إلى الحافظة.", ToastKind.Success);
        }
        catch (Exception ex)
        {
            _dialogs.Info("خطأ", $"تعذّر النسخ: {ex.Message}", DialogKind.Danger);
        }
    }

    [RelayCommand]
    private async Task SavePublicKeyAsync()
    {
        if (!_keys.HasKey) return;
        var path = _files.SaveFile(
            "حفظ المفتاح العام",
            "issuer.public.key",
            "ملفات المفتاح (*.key)|*.key|كل الملفات (*.*)|*.*",
            ".key");
        if (string.IsNullOrEmpty(path)) return;
        try
        {
            await File.WriteAllBytesAsync(path, _keys.GetPublicKeyBlob());
            _toasts.Show("تم حفظ المفتاح العام.", ToastKind.Success);
            _dialogs.Info("تذكير",
                "ضع هذا الملف داخل مجلد Nasag/Resources/issuer.public.key وضمّنه كـ EmbeddedResource في مشروع Nasag.Licensing.",
                DialogKind.Info);
        }
        catch (Exception ex)
        {
            _dialogs.Info("خطأ", $"تعذّر الحفظ: {ex.Message}", DialogKind.Danger);
        }
    }

    [RelayCommand]
    private async Task GenerateNewKeyPairAsync()
    {
        if (_keys.HasKey)
        {
            if (!_dialogs.Confirm(
                "خطر — توليد مفاتيح جديدة",
                "سيتم استبدال المفتاح الحالي. جميع التراخيص المُصدَرة سابقاً ستصبح غير قابلة للتحقق ضمن البرنامج المُحدَّث.\nهل تريد المتابعة؟",
                okText: "متابعة",
                kind: DialogKind.Danger))
                return;

            if (!_dialogs.Confirm(
                "تأكيد نهائي",
                "هذه عملية لا رجعة فيها. أكّد رغبتك في توليد مفاتيح جديدة.",
                okText: "نعم، أنشئ",
                kind: DialogKind.Danger))
                return;
        }

        try
        {
            await _keys.GenerateNewKeyPairAsync();
            Refresh();
            _toasts.Show("تم توليد زوج مفاتيح جديد.", ToastKind.Success);
        }
        catch (Exception ex)
        {
            _dialogs.Info("خطأ", $"تعذّر التوليد: {ex.Message}", DialogKind.Danger);
        }
    }

    [RelayCommand]
    private async Task ImportPrivateKeyAsync()
    {
        var path = _files.OpenFile(
            "اختر ملف المفتاح الخاص",
            "ملفات المفتاح (*.key;*.bin)|*.key;*.bin|كل الملفات (*.*)|*.*");
        if (string.IsNullOrEmpty(path)) return;

        if (_keys.HasKey)
        {
            if (!_dialogs.Confirm(
                "تأكيد",
                "سيتم استبدال المفتاح الحالي بمفتاح من الملف. متابعة؟",
                okText: "استيراد",
                kind: DialogKind.Warning))
                return;
        }

        try
        {
            var bytes = await File.ReadAllBytesAsync(path);
            await _keys.ImportPrivateKeyAsync(bytes);
            Refresh();
            _toasts.Show("تم استيراد المفتاح الخاص.", ToastKind.Success);
        }
        catch (Exception ex)
        {
            _dialogs.Info("خطأ", ex.Message, DialogKind.Danger);
        }
    }
}

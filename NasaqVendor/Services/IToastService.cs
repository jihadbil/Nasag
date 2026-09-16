namespace NasaqVendor.Services;

public enum ToastKind { Info, Success, Warning, Danger }

public interface IToastService
{
    void Show(string message, ToastKind kind = ToastKind.Info);
}

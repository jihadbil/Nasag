using System;

namespace Nasag.Services;

public enum ToastKind { Success, Error, Warning, Info }

public sealed record ToastItem(int Id, ToastKind Kind, string Title, string? Message);

public interface IToastService
{
    event EventHandler<ToastItem>? ToastAdded;
    event EventHandler<int>? ToastRemoved;

    void Show(ToastKind kind, string title, string? message = null);
    void Success(string title, string? message = null);
    void Error(string title, string? message = null);
    void Warning(string title, string? message = null);
    void Info(string title, string? message = null);
    void Dismiss(int id);
}

using System;
using Catering.Core.Common;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Catering.Desktop.ViewModels;

/// <summary>Модальный диалог ввода причины отмены заказа (обязательно, не менее 10 символов).</summary>
public partial class CancelReasonViewModel : ViewModelBase
{
    public event EventHandler? RequestClose;

    /// <summary>true, если пользователь подтвердил отмену.</summary>
    public bool Confirmed { get; private set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsReasonValid))]
    [NotifyPropertyChangedFor(nameof(ReasonLengthText))]
    [NotifyPropertyChangedFor(nameof(CanConfirm))]
    private string _reason = string.Empty;

    public bool IsReasonValid => InputValidator.IsValidCancelReason(Reason);

    public string ReasonLengthText => $"{(Reason ?? string.Empty).Trim().Length} / 10 символов минимум";

    public bool CanConfirm => IsReasonValid;

    [RelayCommand(CanExecute = nameof(CanConfirm))]
    private void Confirm()
    {
        Confirmed = true;
        RequestClose?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void Cancel() => RequestClose?.Invoke(this, EventArgs.Empty);

    partial void OnReasonChanged(string value) => ConfirmCommand.NotifyCanExecuteChanged();
}

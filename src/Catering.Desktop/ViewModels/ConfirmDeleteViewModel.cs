using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Catering.Desktop.ViewModels;

/// <summary>Модальное подтверждение удаления данных (например, черновика договора).</summary>
public partial class ConfirmDeleteViewModel : ViewModelBase
{
    public event EventHandler? RequestClose;

    /// <summary>true, если пользователь подтвердил удаление.</summary>
    public bool Confirmed { get; private set; }

    [ObservableProperty]
    private string _message = "Удалить запись?";

    public void Prepare(string message) => Message = message;

    [RelayCommand]
    private void Confirm()
    {
        Confirmed = true;
        RequestClose?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void Cancel() => RequestClose?.Invoke(this, EventArgs.Empty);
}

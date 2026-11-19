using System;
using Catering.Core.Common;
using Catering.Core.Models;
using Catering.Core.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Catering.Desktop.ViewModels;

/// <summary>
/// Быстрый диалог создания клиента (открывается из редактора договора).
/// Телефон — строго по Regex +7 (XXX) XXX-XX-XX.
/// </summary>
public partial class ClientDialogViewModel : ViewModelBase
{
    private readonly ICatalogService _catalogs;

    public ClientDialogViewModel(ICatalogService catalogs)
    {
        _catalogs = catalogs;
    }

    public event EventHandler? RequestClose;

    /// <summary>Созданный клиент (null, если диалог закрыт без сохранения).</summary>
    public CateringClient? CreatedClient { get; private set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanSave))]
    private string _clientName = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanSave))]
    [NotifyPropertyChangedFor(nameof(PhoneHint))]
    private string _phone = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanSave))]
    private string _eventLocation = string.Empty;

    [ObservableProperty]
    private bool _isVip;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private bool _isBusy;

    public bool IsPhoneValid => InputValidator.IsValidPhone(Phone);

    public string PhoneHint => string.IsNullOrWhiteSpace(Phone)
        ? "Формат: +7 (384) 221-30-40 или 8 (913) 455-67-21"
        : (IsPhoneValid ? "Телефон корректен." : "Неверный формат телефона.");

    public bool CanSave =>
        !IsBusy
        && !string.IsNullOrWhiteSpace(ClientName)
        && IsPhoneValid
        && !string.IsNullOrWhiteSpace(EventLocation);

    partial void OnClientNameChanged(string value) => SaveCommand.NotifyCanExecuteChanged();
    partial void OnPhoneChanged(string value) => SaveCommand.NotifyCanExecuteChanged();
    partial void OnEventLocationChanged(string value) => SaveCommand.NotifyCanExecuteChanged();
    partial void OnIsBusyChanged(bool value) => SaveCommand.NotifyCanExecuteChanged();

    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task SaveAsync()
    {
        IsBusy = true;
        ErrorMessage = null;
        try
        {
            CreatedClient = await _catalogs.CreateClientAsync(new CateringClient
            {
                ClientName = ClientName.Trim(),
                Phone = Phone.Trim(),
                EventLocation = EventLocation.Trim(),
                IsVip = IsVip,
            });
            RequestClose?.Invoke(this, EventArgs.Empty);
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
    private void Cancel() => RequestClose?.Invoke(this, EventArgs.Empty);
}

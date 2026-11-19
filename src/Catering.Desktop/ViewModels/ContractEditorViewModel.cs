using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Catering.Core.Common;
using Catering.Core.Models;
using Catering.Core.Services;
using Catering.Desktop.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Catering.Desktop.ViewModels;

/// <summary>
/// Окно «Master-Detail»: реквизиты шапки договора + встроенная DataGrid спецификации.
/// Добавление/редактирование/удаление позиций на лету пересчитывает итоги документа
/// (servings_count * cost_per_person + equipment_rent_fee), с наценкой за площадку,
/// VIP-скидкой и обслуживанием персоналом (+500 руб./персону).
/// </summary>
public partial class ContractEditorViewModel : ViewModelBase
{
    private readonly IContractService _contracts;
    private readonly ICatalogService _catalogs;
    private readonly ICalculationService _calc;
    private readonly SessionState _session;

    private CateringContract _entity = new()
    {
        EventDatetime = DateTime.Now.Date.AddHours(12),
        Status = ContractStatuses.Preliminary,
        GuestsCount = 20,
    };

    public ContractEditorViewModel(
        IContractService contracts,
        ICatalogService catalogs,
        ICalculationService calc,
        SessionState session)
    {
        _contracts = contracts;
        _catalogs = catalogs;
        _calc = calc;
        _session = session;
    }

    public ObservableCollection<CateringClient> Clients { get; } = new();
    public ObservableCollection<EventType> EventTypes { get; } = new();
    public ObservableCollection<BanquetDish> Dishes { get; } = new();
    public ObservableCollection<MenuItemRowViewModel> Lines { get; } = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNew))]
    private int _contractId;

    [ObservableProperty]
    private string _contractNumber = "(назначение при сохранении)";

    [ObservableProperty]
    private string _status = ContractStatuses.Preliminary;

    [ObservableProperty]
    private CateringClient? _selectedClient;

    [ObservableProperty]
    private EventType? _selectedEventType;

    [ObservableProperty]
    private DateTimeOffset? _eventDate = DateTimeOffset.Now.Date.AddHours(12);

    [ObservableProperty]
    private decimal _guestsCount = 20;

    [ObservableProperty]
    private BanquetDish? _dishToAdd;

    [ObservableProperty]
    private decimal _addServings = 30;

    [ObservableProperty]
    private decimal _addEquipmentFee = 0m;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ItemsCountText))]
    private int _itemsCount;

    [ObservableProperty]
    private decimal _amountDishes;

    [ObservableProperty]
    private decimal _discountAmount;

    [ObservableProperty]
    private decimal _staffService;

    [ObservableProperty]
    private decimal _multiplierAmount;

    [ObservableProperty]
    private decimal _grandTotal;

    [ObservableProperty]
    private string? _validationMessage;

    [ObservableProperty]
    private MenuItemRowViewModel? _selectedLine;

    [ObservableProperty]
    private bool _isBusy;

    /// <summary>Запрашивает закрытие окна (true = закрыть).</summary>
    public event EventHandler? RequestClose;

    /// <summary>true, если изменения успешно сохранены.</summary>
    public bool Saved { get; private set; }

    public bool IsNew => ContractId == 0;

    public string ItemsCountText => $"Позиций: {ItemsCount}";

    /// <summary>Принимает клиента, созданного в быстром диалоге, и выбирает его.</summary>
    public void AcceptNewClient(CateringClient client)
    {
        Clients.Add(client);
        SelectedClient = client;
    }

    public string VipBadge => SelectedClient?.IsVip == true ? "VIP-клиент · скидка 10% на блюда" : string.Empty;

    public string WindowTitle => IsNew ? "Новый договор кейтеринга" : $"Договор {ContractNumber}";

    public async Task InitializeAsync(int? contractId, CancellationToken ct = default)
    {
        IsBusy = true;
        try
        {
            List<CateringClient> clients = await _catalogs.GetClientsAsync(ct);
            List<BanquetDish> dishes = await _catalogs.GetBanquetDishesAsync(ct);
            List<EventType> types = await _catalogs.GetEventTypesAsync(ct);

            Clients.Clear(); foreach (CateringClient c in clients) Clients.Add(c);
            Dishes.Clear(); foreach (BanquetDish d in dishes) Dishes.Add(d);
            EventTypes.Clear(); foreach (EventType t in types) EventTypes.Add(t);

            DishToAdd = Dishes.FirstOrDefault();

            if (contractId is int id)
            {
                CateringContract? loaded = await _contracts.GetByIdAsync(id, ct);
                if (loaded is null)
                    throw new InvalidOperationException($"Договор с Id={id} не найден.");

                _entity = loaded;
                ContractId = loaded.Id;
                ContractNumber = loaded.ContractNumber;
                Status = loaded.Status;
                GuestsCount = loaded.GuestsCount;
                EventDate = new DateTimeOffset(loaded.EventDatetime);
                SelectedClient = Clients.FirstOrDefault(c => c.Id == loaded.CateringClientId);
                SelectedEventType = EventTypes.FirstOrDefault(t => t.Id == loaded.EventTypeId);

                foreach (ContractMenuItem item in loaded.MenuItems)
                {
                    BanquetDish? dish = Dishes.FirstOrDefault(d => d.Id == item.BanquetDishId) ?? item.BanquetDish;
                    if (dish is null) continue;
                    Lines.Add(new MenuItemRowViewModel(dish, item.ServingsCount, item.EquipmentRentFee, _calc, RecalculateTotals));
                }
            }
            else
            {
                ContractId = 0;
                ContractNumber = await _contracts.GenerateNextNumberAsync(ct);
                Status = ContractStatuses.Preliminary;
                _entity = new CateringContract
                {
                    ContractNumber = ContractNumber,
                    UserId = _session.CurrentUser?.Id ?? 1,
                    Status = ContractStatuses.Preliminary,
                    EventDatetime = EventDate!.Value.DateTime,
                    GuestsCount = (int)GuestsCount,
                };
                SelectedClient = Clients.FirstOrDefault();
                SelectedEventType = EventTypes.FirstOrDefault();
            }

            RecalculateTotals();
        }
        finally
        {
            IsBusy = false;
        }
    }

    partial void OnGuestsCountChanged(decimal value) => RecalculateTotals();

    partial void OnSelectedClientChanged(CateringClient? value)
    {
        OnPropertyChanged(nameof(VipBadge));
        RecalculateTotals();
    }

    partial void OnSelectedEventTypeChanged(EventType? value) => RecalculateTotals();

    partial void OnEventDateChanged(DateTimeOffset? value) => Validate();

    [RelayCommand]
    private void AddLine()
    {
        ValidationMessage = null;
        if (DishToAdd is null)
        {
            ValidationMessage = "Выберите блюдо для добавления в спецификацию.";
            return;
        }
        if (AddServings <= 0)
        {
            ValidationMessage = "Количество порций должно быть больше нуля.";
            return;
        }
        if (AddEquipmentFee < 0)
        {
            ValidationMessage = "Аренда оборудования не может быть отрицательной.";
            return;
        }

        Lines.Add(new MenuItemRowViewModel(DishToAdd, (int)AddServings, AddEquipmentFee, _calc, RecalculateTotals));
        RecalculateTotals();
    }

    [RelayCommand]
    private void RemoveSelectedLine()
    {
        if (SelectedLine is null) return;
        Lines.Remove(SelectedLine);
        SelectedLine = null;
        RecalculateTotals();
    }

    [RelayCommand]
    private void Cancel() => RequestClose?.Invoke(this, EventArgs.Empty);

    private bool Validate()
    {
        ValidationMessage = null;
        if (GuestsCount <= 0)
        {
            ValidationMessage = "Количество гостей должно быть больше нуля.";
            return false;
        }
        if (EventDate is null)
        {
            ValidationMessage = "Укажите дату и время выезда.";
            return false;
        }
        if (SelectedClient is null)
        {
            ValidationMessage = "Выберите клиента (контрагента).";
            return false;
        }
        if (!InputValidator.IsValidPhone(SelectedClient.Phone))
        {
            ValidationMessage = "У клиента некорректный телефон. Ожидается +7 (XXX) XXX-XX-XX.";
            return false;
        }
        if (SelectedEventType is null)
        {
            ValidationMessage = "Выберите тип мероприятия.";
            return false;
        }
        if (!DocumentNumberGenerator.IsValid(ContractNumber) && !IsNew)
        {
            ValidationMessage = $"Номер документа «{ContractNumber}» не соответствует маске ПР23-КТР-ГГГГ-00000.";
            return false;
        }
        if (Lines.Count == 0)
        {
            // ТЗ: утвердить смету нельзя с 0 позиций; сам черновик сохранить можно.
            ValidationMessage = "Спецификация пуста: утверждение сметы будет недоступно до добавления блюд.";
        }
        return true;
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (!Validate())
            return;
        if (IsBusy) return;

        IsBusy = true;
        try
        {
            _entity.CateringClientId = SelectedClient!.Id;
            _entity.EventTypeId = SelectedEventType!.Id;
            _entity.EventDatetime = EventDate!.Value.DateTime;
            _entity.GuestsCount = (int)GuestsCount;
            _entity.CateringClient = SelectedClient;
            _entity.EventType = SelectedEventType;
            _entity.UserId = _entity.UserId == 0 ? _session.CurrentUser?.Id ?? 1 : _entity.UserId;
            _entity.MenuItems = Lines.Select(l => l.ToEntity()).ToList();

            await _contracts.SaveAsync(_entity);
            Saved = true;
            RequestClose?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            ValidationMessage = "Ошибка сохранения: " + ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>Пересчёт итогов по всем строкам: скидка VIP → наценка площадки → персонал.</summary>
    private void RecalculateTotals()
    {
        decimal dishes = Lines.Sum(l => l.LineCost);
        decimal discount = SelectedClient?.IsVip == true ? dishes * CateringCalculationService.VipDiscountPercent / 100m : 0m;
        decimal multiplier = SelectedEventType?.ServiceMultiplier ?? 1.00m;
        decimal staff = CateringCalculationService.StaffServicePerGuest * Math.Max(0, (int)GuestsCount);
        decimal multiplierPart = (dishes - discount) * (multiplier - 1m);
        decimal total = (dishes - discount) * multiplier + staff;

        ItemsCount = Lines.Count;
        AmountDishes = Math.Round(dishes, 2);
        DiscountAmount = Math.Round(discount, 2);
        MultiplierAmount = Math.Round(multiplierPart, 2);
        StaffService = Math.Round(staff, 2);
        GrandTotal = Math.Round(total, 2);
    }
}

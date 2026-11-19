using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using Catering.Core.Common;
using Catering.Core.Models;
using Catering.Core.Services;
using Catering.Desktop.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Catering.Desktop.ViewModels;

/// <summary>
/// Главное окно: ФИО и роль сотрудника, список договоров (поиск, фильтр по статусам,
/// сортировка по столбцам, цветовая индикация), DispatcherTimer с обратным отсчётом
/// до ближайшего выезда и выделением просроченных обязательств.
/// </summary>
public partial class MainViewModel : ViewModelBase
{
    private readonly IContractService _contracts;
    private readonly IDocumentExportService _export;
    private readonly SessionState _session;
    private readonly DispatcherTimer _timer;

    private readonly List<CateringContract> _source = new();

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private string _statusFilter = "Все";

    [ObservableProperty]
    private string _sortColumn = "Дата мероприятия";

    [ObservableProperty]
    private bool _sortDescending = false;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelection))]
    [NotifyPropertyChangedFor(nameof(CanEdit))]
    [NotifyPropertyChangedFor(nameof(CanDelete))]
    [NotifyPropertyChangedFor(nameof(CanCancel))]
    [NotifyPropertyChangedFor(nameof(CanAdvance))]
    [NotifyPropertyChangedFor(nameof(NextStatus))]
    private CateringContract? _selectedContract;

    [ObservableProperty]
    private string _statusMessage = "Готово";

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _nowText = string.Empty;

    [ObservableProperty]
    private string _nextEventText = "Ближайший выезд: —";

    [ObservableProperty]
    private int _overdueCount;

    public ObservableCollection<CateringContract> Contracts { get; } = new();

    public ObservableCollection<string> StatusFilters { get; } = new(StatusCatalog.AllFilterValues);

    public ObservableCollection<string> SortColumns { get; } =
        new(new[] { "Дата мероприятия", "Номер", "Клиент", "Сумма", "Гости" });

    public bool HasSelection => SelectedContract is not null;

    public bool CanEdit => SelectedContract is not null && !SelectedContract.IsClosed;

    public bool CanDelete => SelectedContract?.Status == ContractStatuses.Preliminary;

    public bool CanCancel => SelectedContract is not null && !SelectedContract.IsClosed;

    public bool CanAdvance => NextStatus is not null;

    public string? NextStatus =>
        SelectedContract is null ? null : StatusCatalog.NextForwardStatus(SelectedContract.Status);

    public string UserDisplayName => _session.DisplayName;

    public string UserRoleName => _session.RoleName;

    public event EventHandler? SignOutRequested;

    public MainViewModel(IContractService contracts, IDocumentExportService export, SessionState session)
    {
        _contracts = contracts;
        _export = export;
        _session = session;

        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _timer.Tick += (_, _) => RefreshTemporalIndicators();
        _timer.Start();
        RefreshTemporalIndicators();
    }

    partial void OnSearchTextChanged(string value) => _ = ReloadAsync();

    partial void OnStatusFilterChanged(string value) => _ = ReloadAsync();

    partial void OnSortColumnChanged(string value) => ApplySort();

    partial void OnSortDescendingChanged(bool value) => ApplySort();

    partial void OnSelectedContractChanged(CateringContract? value)
    {
        StatusMessage = value is null
            ? "Договор не выбран"
            : $"Выбран {value.ContractNumber} — {value.CateringClient?.ClientName}";
        AdvanceStatusCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand]
    private Task ReloadAsync() => LoadAsyncSafe();

    [RelayCommand]
    private void ToggleSortDirection() => SortDescending = !SortDescending;

    public async Task LoadAsyncSafe()
    {
        try
        {
            IsBusy = true;
            List<CateringContract> loaded = await _contracts.GetAllAsync(SearchText, StatusFilter);
            _source.Clear();
            _source.AddRange(loaded);
            ApplySort(preserveSelection: SelectedContract?.Id);
        }
        catch (Exception ex)
        {
            StatusMessage = "Ошибка загрузки: " + ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>Сортировка списка по выбранному столбцу (привязана к столбцам DataGrid).</summary>
    private void ApplySort(int? preserveSelection = null)
    {
        IEnumerable<CateringContract> query = SortColumn switch
        {
            "Номер" => _source.OrderBy(x => x.ContractNumber, StringComparer.Ordinal),
            "Клиент" => _source.OrderBy(x => x.CateringClient?.ClientName ?? string.Empty, StringComparer.OrdinalIgnoreCase),
            "Сумма" => _source.OrderBy(x => x.TotalBudget),
            "Гости" => _source.OrderBy(x => x.GuestsCount),
            _ => _source.OrderBy(x => x.EventDatetime),
        };
        if (SortDescending)
            query = query.Reverse();

        Contracts.Clear();
        foreach (CateringContract item in query)
            Contracts.Add(item);

        if (preserveSelection is int id)
            SelectedContract = Contracts.FirstOrDefault(x => x.Id == id);
    }

    [RelayCommand(CanExecute = nameof(CanAdvance))]
    private async Task AdvanceStatusAsync()
    {
        if (SelectedContract is null) return;
        string? next = NextStatus;
        if (next is null) return;
        try
        {
            IsBusy = true;
            await _contracts.TransitionAsync(SelectedContract.Id, next, null);
            StatusMessage = $"Статус изменён на «{next}».";
            await LoadAsyncSafe();
        }
        catch (Exception ex)
        {
            StatusMessage = "Переход отклонён: " + ex.Message;
        }
        finally { IsBusy = false; }
    }

    /// <summary>Отмена договора: причина передаётся диалогом (минимум 10 символов).</summary>
    public async Task CancelContractAsync(string reason)
    {
        if (SelectedContract is null) return;
        try
        {
            IsBusy = true;
            await _contracts.TransitionAsync(SelectedContract.Id, ContractStatuses.Cancelled, reason);
            StatusMessage = "Договор отменён клиентом.";
            await LoadAsyncSafe();
        }
        catch (Exception ex)
        {
            StatusMessage = "Отмена отклонена: " + ex.Message;
        }
        finally { IsBusy = false; }
    }

    public async Task DeleteSelectedAsync()
    {
        if (SelectedContract is null) return;
        try
        {
            IsBusy = true;
            await _contracts.DeleteAsync(SelectedContract.Id);
            StatusMessage = "Черновик удалён.";
            await LoadAsyncSafe();
        }
        catch (Exception ex)
        {
            StatusMessage = "Удаление отклонено: " + ex.Message;
        }
        finally { IsBusy = false; }
    }

    /// <summary>Экспорт выбранного договора в файл (.txt / .csv / .html) с SHA256-отпечатком.</summary>
    public async Task<string?> ExportToFileAsync(string filePath)
    {
        if (SelectedContract is null) return null;
        try
        {
            IsBusy = true;
            CateringContract? full = await _contracts.GetByIdAsync(SelectedContract.Id);
            if (full is null)
            {
                StatusMessage = "Договор не найден в базе.";
                return null;
            }

            string signature = _export.Export(full, filePath);
            StatusMessage = $"Экспорт выполнен. Отпечаток SHA256: {signature[..16]}…";
            return signature;
        }
        catch (Exception ex)
        {
            StatusMessage = "Ошибка экспорта: " + ex.Message;
            return null;
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private void SignOut() => SignOutRequested?.Invoke(this, EventArgs.Empty);

    public bool IsOverdue(CateringContract contract) =>
        contract.EventDatetime < DateTime.Now && !ContractStatuses.IsTerminal(contract.Status);

    /// <summary>DispatcherTimer: часы, обратный отсчёт до ближайшего выезда, счётчик просрочек.</summary>
    private void RefreshTemporalIndicators()
    {
        DateTime now = DateTime.Now;
        NowText = now.ToString("dd.MM.yyyy HH:mm:ss", CultureInfo.InvariantCulture);

        CateringContract? nearest = _source
            .Where(x => x.EventDatetime >= now && !ContractStatuses.IsTerminal(x.Status))
            .OrderBy(x => x.EventDatetime)
            .FirstOrDefault();
        NextEventText = nearest is null
            ? "Ближайший выезд: —"
            : $"Ближайший выезд: {nearest.EventDatetime:dd.MM.yyyy HH:mm} · {nearest.CateringClient?.ClientName} · T−{FormatCountdown(nearest.EventDatetime - now)}";

        int overdue = _source.Count(IsOverdue);
        if (overdue != OverdueCount)
            OverdueCount = overdue;
    }

    private static string FormatCountdown(TimeSpan delta) =>
        delta.TotalDays >= 1
            ? $"{(int)delta.TotalDays}д {delta.Hours:D2}:{delta.Minutes:D2}:{delta.Seconds:D2}"
            : $"{delta.Hours:D2}:{delta.Minutes:D2}:{delta.Seconds:D2}";
}

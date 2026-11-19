using Catering.Core.Common;
using Catering.Core.Data;
using Catering.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace Catering.Core.Services;

/// <summary>
/// Реализация сервиса договоров. Все операции сохранения идут
/// через явную транзакцию EF Core (using var transaction = await context.Database.BeginTransactionAsync()).
/// Чтение заголовка с позициями — через Include(x => x.MenuItems).
/// </summary>
public sealed class ContractService : IContractService
{
    private readonly Func<AppDbContext> _contextFactory;
    private readonly ICalculationService _calculation;
    private readonly IContractStateMachine _fsm;

    public ContractService(
        Func<AppDbContext> contextFactory,
        ICalculationService calculation,
        IContractStateMachine fsm)
    {
        _contextFactory = contextFactory;
        _calculation = calculation;
        _fsm = fsm;
    }

    public async Task<List<CateringContract>> GetAllAsync(
        string? search, string? statusFilter, CancellationToken ct = default)
    {
        using AppDbContext context = _contextFactory();
        IQueryable<CateringContract> query = context.CateringContracts
            .Include(x => x.CateringClient)
            .Include(x => x.EventType)
            .Include(x => x.User)
            .Include(x => x.MenuItems)
                .ThenInclude(i => i.BanquetDish)
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(statusFilter) && statusFilter != "Все")
            query = query.Where(x => x.Status == statusFilter);

        if (!string.IsNullOrWhiteSpace(search))
        {
            string pattern = $"%{search.Trim()}%";
            query = query.Where(x =>
                EF.Functions.Like(x.ContractNumber, pattern) ||
                (x.CateringClient != null && EF.Functions.Like(x.CateringClient.ClientName, pattern)) ||
                (x.EventType != null && EF.Functions.Like(x.EventType.TypeTitle, pattern)));
        }

        return await query.OrderByDescending(x => x.EventDatetime).ToListAsync(ct);
    }

    public async Task<CateringContract?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        using AppDbContext context = _contextFactory();
        return await context.CateringContracts
            .Include(x => x.CateringClient)
            .Include(x => x.EventType)
            .Include(x => x.User)
            .Include(x => x.MenuItems)
                .ThenInclude(i => i.BanquetDish)
            .FirstOrDefaultAsync(x => x.Id == id, ct);
    }

    public async Task<string> GenerateNextNumberAsync(CancellationToken ct = default)
    {
        using AppDbContext context = _contextFactory();
        int year = DateTime.Now.Year;
        string prefix = DocumentNumberGenerator.Prefix + "-" + year + "-";
        int count = await context.CateringContracts.CountAsync(x => x.ContractNumber.StartsWith(prefix), ct);
        return DocumentNumberGenerator.Generate(year, count + 1);
    }

    public void Recalculate(CateringContract contract)
    {
        ArgumentNullException.ThrowIfNull(contract);
        Dictionary<int, BanquetDish> dishesById = contract.MenuItems
            .Where(i => i.BanquetDish != null)
            .GroupBy(i => i.BanquetDishId)
            .ToDictionary(g => g.Key, g => g.First().BanquetDish!);

        foreach (ContractMenuItem item in contract.MenuItems)
        {
            BanquetDish? dish = item.BanquetDish
                ?? dishesById.GetValueOrDefault(item.BanquetDishId);
            decimal cost = dish?.CostPerPerson ?? 0m;
            item.LineCost = _calculation.CalculateLineCost(item.ServingsCount, cost, item.EquipmentRentFee);
        }

        contract.TotalBudget = _calculation.CalculateTotal(
            contract.MenuItems.ToList(),
            dishesById,
            contract.EventType?.ServiceMultiplier ?? 1.00m,
            contract.GuestsCount,
            contract.CateringClient?.IsVip ?? false);
    }

    public async Task<CateringContract> SaveAsync(CateringContract contract, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(contract);
        using AppDbContext context = _contextFactory();
        using var transaction = await context.Database.BeginTransactionAsync(ct);

        CateringContract? existing = await context.CateringContracts
            .Include(x => x.MenuItems)
            .FirstOrDefaultAsync(x => x.Id == contract.Id, ct);

        if (existing is null)
        {
            if (string.IsNullOrWhiteSpace(contract.ContractNumber))
                contract.ContractNumber = await GenerateNextNumberAsync(ct);
            Recalculate(contract);
            DetachNavigationReferences(contract);
            context.CateringContracts.Add(contract);
        }
        else
        {
            _fsm.EnsureEditable(existing);
            if (!InputValidator.IsPositive(contract.GuestsCount))
                throw new InvalidOperationException("Количество гостей должно быть больше нуля.");
            if (contract.TotalBudget < 0)
                throw new InvalidOperationException("Сумма договора не может быть отрицательной.");

            existing.CateringClientId = contract.CateringClientId;
            existing.EventTypeId = contract.EventTypeId;
            existing.EventDatetime = contract.EventDatetime;
            existing.GuestsCount = contract.GuestsCount;

            // Заменяем спецификацию целиком: старые строки удаляются, новые добавляются каскадом.
            context.ContractMenuItems.RemoveRange(existing.MenuItems);
            existing.MenuItems = contract.MenuItems;
            Recalculate(existing);
            DetachItemNavigationReferences(existing);
        }

        await context.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return contract;
    }

    /// <summary>
    /// Снимает неотслеживаемые навигационные ссылки перед вставкой: строки создавались вне
    /// контекста, и EF Core иначе принял бы их за новые сущности (повторный INSERT справочников).
    /// Значения внешних ключей (ContractId, BanquetDishId, ...) сохраняются.
    /// </summary>
    private static void DetachNavigationReferences(CateringContract contract)
    {
        contract.CateringClient = null;
        contract.EventType = null;
        contract.User = null;
        DetachItemNavigationReferences(contract);
    }

    private static void DetachItemNavigationReferences(CateringContract contract)
    {
        foreach (ContractMenuItem item in contract.MenuItems)
            item.BanquetDish = null;
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        using AppDbContext context = _contextFactory();
        using var transaction = await context.Database.BeginTransactionAsync(ct);

        CateringContract? existing = await context.CateringContracts
            .Include(x => x.MenuItems)
            .FirstOrDefaultAsync(x => x.Id == id, ct);
        if (existing is null)
            return;
        if (existing.Status != ContractStatuses.Preliminary)
            throw new InvalidOperationException("Удалить можно только договор в статусе «Предварительный расчет».");

        context.CateringContracts.Remove(existing);
        await context.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
    }

    public async Task<CateringContract> TransitionAsync(
        int id, string targetStatus, string? cancelReason, CancellationToken ct = default)
    {
        using AppDbContext context = _contextFactory();
        using var transaction = await context.Database.BeginTransactionAsync(ct);

        CateringContract? contract = await context.CateringContracts
            .Include(x => x.CateringClient)
            .Include(x => x.EventType)
            .Include(x => x.User)
            .Include(x => x.MenuItems)
                .ThenInclude(i => i.BanquetDish)
            .FirstOrDefaultAsync(x => x.Id == id, ct);
        if (contract is null)
            throw new InvalidOperationException($"Договор с Id={id} не найден.");

        int minGuests = contract.EventType?.MinGuests ?? 1;
        _fsm.ValidateTransition(contract, targetStatus, minGuests, cancelReason);

        contract.Status = targetStatus;
        if (targetStatus == ContractStatuses.Cancelled)
            contract.CancelReason = cancelReason!.Trim();

        Recalculate(contract);
        await context.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return contract;
    }
}

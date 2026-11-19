using Catering.Core.Models;

namespace Catering.Core.Services;

/// <summary>
/// Операции над договорами кейтеринга (Master-Detail): список с поиском/фильтром,
/// реквизиты шапки и спецификация, пересчет итогов, переходы FSM, отмена с причиной.
/// Сохранение — транзакционное (BeginTransactionAsync).
/// </summary>
public interface IContractService
{
    /// <summary>Загружает договоры с Include(x => x.MenuItems) и справочниками. Поиск по номеру/клиенту, фильтр по статусу.</summary>
    Task<List<CateringContract>> GetAllAsync(string? search, string? statusFilter, CancellationToken ct = default);

    /// <summary>Загружает один договор со спецификацией, блюдами и клиентом.</summary>
    Task<CateringContract?> GetByIdAsync(int id, CancellationToken ct = default);

    /// <summary>Следующий номер документа по маске ПР23-КТР-{Год}-{Номер:D5}.</summary>
    Task<string> GenerateNextNumberAsync(CancellationToken ct = default);

    /// <summary>Пересчитывает LineCost каждой строки и TotalBudget шапки. Не сохраняет.</summary>
    void Recalculate(CateringContract contract);

    /// <summary>
    /// Транзакционно сохраняет договор со спецификацией:
    /// валидация FSM/редактирования, пересчет итогов, INSERT/UPDATE шапки и строк.
    /// </summary>
    Task<CateringContract> SaveAsync(CateringContract contract, CancellationToken ct = default);

    /// <summary>Удаляет договор и спецификацию (только в статусе «Предварительный расчет»). Транзакционно.</summary>
    Task DeleteAsync(int id, CancellationToken ct = default);

    /// <summary>Переход FSM с валидацией; для Cancelled требуется причина отмены.</summary>
    Task<CateringContract> TransitionAsync(int id, string targetStatus, string? cancelReason, CancellationToken ct = default);
}

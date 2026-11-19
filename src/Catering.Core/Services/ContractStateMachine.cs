using Catering.Core.Common;
using Catering.Core.Models;

namespace Catering.Core.Services;

/// <summary>
/// Реализация FSM договора:
///   Предварительный расчет -> Смета утверждена -> Закупка сырья -> Банкет проведен
///   из любого незакрытого статуса возможна Отмена клиентом.
/// </summary>
public sealed class ContractStateMachine : IContractStateMachine
{
    private static readonly IReadOnlyDictionary<string, IReadOnlyList<string>> AllowedTransitions =
        new Dictionary<string, IReadOnlyList<string>>
        {
            [ContractStatuses.Preliminary] = new[] { ContractStatuses.Approved, ContractStatuses.Cancelled },
            [ContractStatuses.Approved] = new[] { ContractStatuses.Procurement, ContractStatuses.Cancelled },
            [ContractStatuses.Procurement] = new[] { ContractStatuses.Completed, ContractStatuses.Cancelled },
            [ContractStatuses.Completed] = Array.Empty<string>(),
            [ContractStatuses.Cancelled] = Array.Empty<string>(),
        };

    public bool CanTransition(string currentStatus, string targetStatus)
    {
        if (string.IsNullOrWhiteSpace(currentStatus) || string.IsNullOrWhiteSpace(targetStatus))
            return false;
        return AllowedTransitions.TryGetValue(currentStatus, out IReadOnlyList<string>? next)
               && next.Contains(targetStatus);
    }

    public void ValidateTransition(
        CateringContract contract,
        string targetStatus,
        int minGuests,
        string? cancelReason = null)
    {
        ArgumentNullException.ThrowIfNull(contract);

        if (!ContractStatuses.All.Contains(targetStatus))
            throw new InvalidOperationException($"Неизвестный целевой статус: '{targetStatus}'.");

        if (ContractStatuses.IsTerminal(contract.Status))
            throw new InvalidOperationException(
                $"Документ {contract.ContractNumber} закрыт (статус '{contract.Status}') и не может изменяться.");

        if (!CanTransition(contract.Status, targetStatus))
            throw new InvalidOperationException(
                $"Переход '{contract.Status}' -> '{targetStatus}' запрещен конечным автоматом.");

        // Пустой заказ (0 строк) нельзя переводить в оплату/утверждение и дальше по цепочке.
        bool hasItems = contract.MenuItems.Count > 0;
        if ((targetStatus == ContractStatuses.Approved ||
             targetStatus == ContractStatuses.Procurement ||
             targetStatus == ContractStatuses.Completed) && !hasItems)
        {
            throw new InvalidOperationException(
                "Нельзя переводить пустой договор (0 позиций спецификации) в статус оплаты/утверждения.");
        }

        // Ограничение FSM из ТЗ: guests_count < min_guests блокирует «Смета утверждена».
        if (targetStatus == ContractStatuses.Approved && contract.GuestsCount < minGuests)
        {
            throw new InvalidOperationException(
                $"Гостей {contract.GuestsCount} меньше минимального ({minGuests}) для выбранного типа мероприятия.");
        }

        // Отмена требует обязательную причину длиной не менее 10 знаков.
        if (targetStatus == ContractStatuses.Cancelled && !InputValidator.IsValidCancelReason(cancelReason))
        {
            throw new InvalidOperationException(
                "Для отмены договора требуется причина отмены длиной не менее 10 символов.");
        }
    }

    public void EnsureEditable(CateringContract contract)
    {
        ArgumentNullException.ThrowIfNull(contract);
        if (contract.IsClosed)
        {
            throw new InvalidOperationException(
                $"Документ {contract.ContractNumber} закрыт (статус '{contract.Status}'): редактирование запрещено.");
        }
    }
}

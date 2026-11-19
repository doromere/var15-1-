using Catering.Core.Common;
using Catering.Core.Models;
using Catering.Core.Services;
using Xunit;

namespace Catering.Tests;

public class ContractStateMachineTests
{
    private static readonly ContractStateMachine Fsm = new();

    private static CateringContract Draft(int guests, int itemsCount) => new()
    {
        Id = 1,
        ContractNumber = "ПР23-КТР-2026-00001",
        GuestsCount = guests,
        Status = ContractStatuses.Preliminary,
        MenuItems = Enumerable.Range(1, itemsCount)
            .Select(i => new ContractMenuItem { Id = i, BanquetDishId = i, ServingsCount = 10, LineCost = 1_000m })
            .ToList(),
    };

    [Fact] // Тест 3 (ТЗ): проверка успешного перевода заказа по разрешённой цепочке статусов FSM.
    public void ValidTransition_Chain_Passes()
    {
        CateringContract contract = Draft(guests: 40, itemsCount: 3);

        Fsm.ValidateTransition(contract, ContractStatuses.Approved, minGuests: 20);   // не бросает
        contract.Status = ContractStatuses.Approved;
        Fsm.ValidateTransition(contract, ContractStatuses.Procurement, minGuests: 20);
        contract.Status = ContractStatuses.Procurement;
        Fsm.ValidateTransition(contract, ContractStatuses.Completed, minGuests: 20);

        Assert.True(Fsm.CanTransition(ContractStatuses.Preliminary, ContractStatuses.Approved));
        Assert.True(Fsm.CanTransition(ContractStatuses.Approved, ContractStatuses.Procurement));
        Assert.True(Fsm.CanTransition(ContractStatuses.Procurement, ContractStatuses.Completed));
    }

    [Fact] // Тест 4 (ТЗ): недопустимый переход FSM → InvalidOperationException.
    public void InvalidTransition_Throws_InvalidOperationException()
    {
        CateringContract draft = Draft(guests: 40, itemsCount: 2);

        // Нельзя перепрыгнуть цепочку: «Предварительный расчет» -> «Закупка сырья».
        Assert.Throws<InvalidOperationException>(
            () => Fsm.ValidateTransition(draft, ContractStatuses.Procurement, minGuests: 20));

        // Нельзя перепрыгнуть в «Банкет проведен» напрямую.
        Assert.Throws<InvalidOperationException>(
            () => Fsm.ValidateTransition(draft, ContractStatuses.Completed, minGuests: 20));

        // Закрытый документ (Банкет проведен) не может быть изменён вообще.
        CateringContract closed = Draft(guests: 40, itemsCount: 2);
        closed.Status = ContractStatuses.Completed;
        Assert.Throws<InvalidOperationException>(
            () => Fsm.ValidateTransition(closed, ContractStatuses.Approved, minGuests: 20));

        Assert.Throws<InvalidOperationException>(() => Fsm.EnsureEditable(closed));
    }

    [Fact] // Ограничение FSM из ТЗ: guests_count < min_guests блокирует «Смета утверждена».
    public void Approve_Blocked_When_GuestsBelowMinGuests()
    {
        CateringContract contract = Draft(guests: 10, itemsCount: 2);

        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(
            () => Fsm.ValidateTransition(contract, ContractStatuses.Approved, minGuests: 20));

        Assert.Contains("меньше минимального", ex.Message);
    }

    [Fact] // Пустой заказ (0 строк) нельзя перевести в статус утверждения/оплаты.
    public void EmptyContract_CannotBeApproved()
    {
        CateringContract empty = Draft(guests: 50, itemsCount: 0);

        Assert.Throws<InvalidOperationException>(
            () => Fsm.ValidateTransition(empty, ContractStatuses.Approved, minGuests: 20));
    }

    [Fact] // Отмена требует причину длиной не менее 10 символов.
    public void Cancel_Requires_ReasonOfAtLeast10Chars()
    {
        CateringContract contract = Draft(guests: 40, itemsCount: 1);

        Assert.Throws<InvalidOperationException>(
            () => Fsm.ValidateTransition(contract, ContractStatuses.Cancelled, minGuests: 20, cancelReason: "коротко"));
        Assert.Throws<InvalidOperationException>(
            () => Fsm.ValidateTransition(contract, ContractStatuses.Cancelled, minGuests: 20, cancelReason: null));

        Fsm.ValidateTransition(contract, ContractStatuses.Cancelled, minGuests: 20,
            cancelReason: "Клиент перенёс мероприятие на сентябрь 2026"); // не бросает
    }
}

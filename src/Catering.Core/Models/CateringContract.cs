namespace Catering.Core.Models;

/// <summary>
/// Договор кейтерингового обслуживания — заголовок документа (таблица catering_contracts).
/// Status — одно из значений <see cref="ContractStatuses"/>; переходы проверяются FSM.
/// TotalBudget пересчитывается сервисом калькуляции при каждом изменении спецификации.
/// </summary>
public class CateringContract
{
    public int Id { get; set; }

    /// <summary>Номер документа по маске ПР23-КТР-{Год}-{Номер:D5}.</summary>
    public string ContractNumber { get; set; } = string.Empty;

    public int UserId { get; set; }

    public int CateringClientId { get; set; }

    public int EventTypeId { get; set; }

    public DateTime EventDatetime { get; set; }

    public int GuestsCount { get; set; }

    public decimal TotalBudget { get; set; }

    public string Status { get; set; } = ContractStatuses.Preliminary;

    public string? CancelReason { get; set; }

    public User? User { get; set; }

    public CateringClient? CateringClient { get; set; }

    public EventType? EventType { get; set; }

    public ICollection<ContractMenuItem> MenuItems { get; set; } = new List<ContractMenuItem>();

    /// <summary>Закрытый документ (банкет проведен / отмена клиентом) редактировать запрещено.</summary>
    public bool IsClosed => Status == ContractStatuses.Completed || Status == ContractStatuses.Cancelled;
}

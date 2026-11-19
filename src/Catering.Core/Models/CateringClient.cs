namespace Catering.Core.Models;

/// <summary>
/// Клиент кейтеринговой компании (таблица catering_clients).
/// IsVip — VIP-клиент: на сумму блюд применяется скидка 10% (см. CateringCalculationService).
/// </summary>
public class CateringClient
{
    public int Id { get; set; }

    public string ClientName { get; set; } = string.Empty;

    public string Phone { get; set; } = string.Empty;

    public string EventLocation { get; set; } = string.Empty;

    public bool IsVip { get; set; }

    public ICollection<CateringContract> Contracts { get; set; } = new List<CateringContract>();
}

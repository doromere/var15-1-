namespace Catering.Core.Models;

/// <summary>
/// Тип мероприятия (таблица event_types) — справочник шеф-менеджера.
/// ServiceMultiplier — наценка за сложность площадки
/// (например, 1.25 для открытых площадок без стационарной кухни).
/// MinGuests — минимальное число гостей для утверждения сметы (FSM).
/// </summary>
public class EventType
{
    public int Id { get; set; }

    public string TypeTitle { get; set; } = string.Empty;

    public decimal ServiceMultiplier { get; set; } = 1.00m;

    public int MinGuests { get; set; }

    public ICollection<CateringContract> Contracts { get; set; } = new List<CateringContract>();
}

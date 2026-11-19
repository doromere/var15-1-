namespace Catering.Core.Common;

/// <summary>
/// Статусы жизненного цикла договора кейтерингового обслуживания (FSM).
/// </summary>
public static class ContractStatuses
{
    /// <summary>Начальный статус: смета еще не утверждена клиентом.</summary>
    public const string Preliminary = "Предварительный расчет";

    /// <summary>Смета утверждена (требуется guests_count &gt;= min_guests и непустая спецификация).</summary>
    public const string Approved = "Смета утверждена";

    /// <summary>Идет закупка сырья по утвержденной смете.</summary>
    public const string Procurement = "Закупка сырья";

    /// <summary>Терминальный статус: банкет проведен, документ закрыт.</summary>
    public const string Completed = "Банкет проведен";

    /// <summary>Терминальный статус: отмена клиентом (требуется причина отмены от 10 символов).</summary>
    public const string Cancelled = "Отмена клиентом";

    public static readonly IReadOnlyList<string> All = new[]
    {
        Preliminary, Approved, Procurement, Completed, Cancelled
    };

    public static bool IsTerminal(string status) =>
        status == Completed || status == Cancelled;
}

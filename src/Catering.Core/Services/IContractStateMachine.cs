using Catering.Core.Models;

namespace Catering.Core.Services;

/// <summary>
/// Конечный автомат состояний договора (FSM).
/// Любой переход проверяется валидатором; недопустимый переход
/// выбрасывает <see cref="InvalidOperationException"/>.
/// </summary>
public interface IContractStateMachine
{
    /// <summary>Можно ли перейти из статуса <paramref name="currentStatus"/> в <paramref name="targetStatus"/>.</summary>
    bool CanTransition(string currentStatus, string targetStatus);

    /// <summary>
    /// Проверяет переход и при нарушении правил выбрасывает InvalidOperationException.
    /// Правила: пустой договор нельзя переводить в «Смета утверждена»/дальше;
    /// «Смета утверждена» блокируется при guests_count &lt; min_guests типа мероприятия;
    /// отмена требует причину от 10 символов; закрытый документ не переходит никуда.
    /// </summary>
    void ValidateTransition(
        CateringContract contract,
        string targetStatus,
        int minGuests,
        string? cancelReason = null);

    /// <summary>Проверяет, что документ можно редактировать (не закрыт).</summary>
    void EnsureEditable(CateringContract contract);
}

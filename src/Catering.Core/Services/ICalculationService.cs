using Catering.Core.Models;

namespace Catering.Core.Services;

/// <summary>
/// Сервис калькуляции сметы кейтерингового обслуживания (ПК 1.1, ПК 1.2).
/// Не зависит от UI-библиотек и может тестироваться изолированно.
/// </summary>
public interface ICalculationService
{
    /// <summary>Сумма строки: servings_count * cost_per_person + equipment_rent_fee.</summary>
    decimal CalculateLineCost(int servingsCount, decimal costPerPerson, decimal equipmentRentFee);

    /// <summary>
    /// Итоговая сумма договора:
    /// (сумма строк с учетом VIP-скидки 10%) * service_multiplier + 500 руб. * guests_count.
    /// </summary>
    decimal CalculateTotal(
        IReadOnlyCollection<ContractMenuItem> items,
        IReadOnlyDictionary<int, BanquetDish> dishesById,
        decimal serviceMultiplier,
        int guestsCount,
        bool isVip);
}

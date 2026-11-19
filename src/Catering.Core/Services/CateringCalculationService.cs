using Catering.Core.Models;

namespace Catering.Core.Services;

/// <summary>
/// Реализация калькуляции сметы (ТЗ, вариант 15):
///   line_cost = servings_count * cost_per_person + equipment_rent_fee;
///   total     = (SUM(line_cost) с VIP-скидкой 10%) * service_multiplier
///               + 500 руб. * guests_count   (обслуживание персоналом).
/// service_multiplier — наценка за сложность площадки
/// (например, 1.25 для открытых площадок без стационарной кухни).
/// </summary>
public sealed class CateringCalculationService : ICalculationService
{
    /// <summary>Обслуживание персоналом, руб. на персону.</summary>
    public const decimal StaffServicePerGuest = 500m;

    /// <summary>Скидка VIP-клиенту на сумму блюд, %.</summary>
    public const decimal VipDiscountPercent = 10m;

    public decimal CalculateLineCost(int servingsCount, decimal costPerPerson, decimal equipmentRentFee)
    {
        if (servingsCount < 0)
            throw new ArgumentOutOfRangeException(nameof(servingsCount), "Количество порций не может быть отрицательным.");
        if (costPerPerson < 0)
            throw new ArgumentOutOfRangeException(nameof(costPerPerson), "Стоимость на персону не может быть отрицательной.");
        if (equipmentRentFee < 0)
            throw new ArgumentOutOfRangeException(nameof(equipmentRentFee), "Аренда оборудования не может быть отрицательной.");

        return Round(servingsCount * costPerPerson + equipmentRentFee);
    }

    public decimal CalculateTotal(
        IReadOnlyCollection<ContractMenuItem> items,
        IReadOnlyDictionary<int, BanquetDish> dishesById,
        decimal serviceMultiplier,
        int guestsCount,
        bool isVip)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(dishesById);
        if (serviceMultiplier <= 0)
            throw new ArgumentOutOfRangeException(nameof(serviceMultiplier), "Коэффициент площадки должен быть > 0.");
        if (guestsCount < 0)
            throw new ArgumentOutOfRangeException(nameof(guestsCount), "Количество гостей не может быть отрицательным.");

        decimal dishesSubtotal = 0m;
        foreach (ContractMenuItem item in items)
        {
            if (!dishesById.TryGetValue(item.BanquetDishId, out BanquetDish? dish))
                throw new InvalidOperationException($"Блюдо с Id={item.BanquetDishId} не найдено в справочнике.");
            dishesSubtotal += CalculateLineCost(item.ServingsCount, dish.CostPerPerson, item.EquipmentRentFee);
        }

        // Льгота: VIP-клиент получает скидку 10% на сумму блюд (до наценки площадки).
        decimal afterDiscount = isVip
            ? dishesSubtotal * (1m - VipDiscountPercent / 100m)
            : dishesSubtotal;

        // Надбавка за сложность площадки + обслуживание персоналом.
        decimal total = afterDiscount * serviceMultiplier + StaffServicePerGuest * guestsCount;
        return Round(total);
    }

    private static decimal Round(decimal value) =>
        Math.Round(value, 2, MidpointRounding.AwayFromZero);
}

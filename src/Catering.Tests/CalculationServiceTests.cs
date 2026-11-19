using Catering.Core.Models;
using Catering.Core.Services;
using Xunit;

namespace Catering.Tests;

public class CalculationServiceTests
{
    private static readonly CateringCalculationService Service = new();

    private static ContractMenuItem Line(int dishId, int servings, decimal rent) => new()
    {
        BanquetDishId = dishId,
        ServingsCount = servings,
        EquipmentRentFee = rent,
    };

    private static Dictionary<int, BanquetDish> DishCatalog() => new()
    {
        [1] = new BanquetDish { Id = 1, DishTitle = "Брускетта с лососем", Category = "Холодные закуски", PortionWeightG = 90, CostPerPerson = 185.00m },
        [3] = new BanquetDish { Id = 3, DishTitle = "Тартар из тунца", Category = "Холодные закуски", PortionWeightG = 110, CostPerPerson = 320.00m },
    };

    [Fact] // Тест 1 (ТЗ): математическая формула расчёта позиции и итоговой суммы заказа при стандартных параметрах.
    public void LineCost_And_Total_WithStandardParameters_AreCalculatedCorrectly()
    {
        // line_cost = servings_count * cost_per_person + equipment_rent_fee
        decimal line1 = Service.CalculateLineCost(servingsCount: 20, costPerPerson: 185.00m, equipmentRentFee: 5_000m);
        decimal line2 = Service.CalculateLineCost(servingsCount: 10, costPerPerson: 320.00m, equipmentRentFee: 3_000m);

        Assert.Equal(8_700.00m, line1);   // 20*185 + 5000
        Assert.Equal(6_200.00m, line2);   // 10*320 + 3000

        // total = SUM(line_cost) * service_multiplier + 500 * guests_count
        List<ContractMenuItem> items = new()
        {
            Line(1, 20, 5_000m),
            Line(3, 10, 3_000m),
        };

        decimal total = Service.CalculateTotal(
            items,
            DishCatalog(),
            serviceMultiplier: 1.25m,
            guestsCount: 30,
            isVip: false);

        // (8700 + 6200) * 1.25 + 500*30 = 14900*1.25 + 15000 = 18625 + 15000 = 33625.00
        Assert.Equal(33_625.00m, total);
    }

    [Fact] // Тест 2 (ТЗ): проверка применения скидок (VIP 10%) и наценок (площадка ×1.25).
    public void Total_WithVipDiscountAndVenueMultiplier_AppliesBoth()
    {
        List<ContractMenuItem> items = new()
        {
            Line(1, 20, 5_000m),   // 8700
            Line(3, 10, 3_000m),   // 6200
        };

        decimal vipTotal = Service.CalculateTotal(items, DishCatalog(), 1.25m, guestsCount: 30, isVip: true);

        // (8700+6200) * 0.9 = 13410; 13410 * 1.25 = 16762.50; + 500*30 = 31762.50
        Assert.Equal(31_762.50m, vipTotal);

        decimal plainTotal = Service.CalculateTotal(items, DishCatalog(), 1.00m, guestsCount: 30, isVip: false);
        // 14900 * 1.00 + 15000 = 29900.00
        Assert.Equal(29_900.00m, plainTotal);

        // Наценка открытой площадки без стационарной кухни — +25% к сумме блюд.
        decimal openAirTotal = Service.CalculateTotal(items, DishCatalog(), 1.25m, guestsCount: 0, isVip: false);
        Assert.Equal(18_625.00m, openAirTotal);
    }

    [Fact]
    public void LineCost_RejectsNegativeValues()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Service.CalculateLineCost(-1, 100m, 0m));
        Assert.Throws<ArgumentOutOfRangeException>(() => Service.CalculateLineCost(10, -100m, 0m));
        Assert.Throws<ArgumentOutOfRangeException>(() => Service.CalculateLineCost(10, 100m, -1m));
    }
}

namespace Catering.Core.Models;

/// <summary>
/// Блюдо банкетного меню / технологическая карта (таблица banquet_dishes).
/// CostPerPerson — себестоимость/тариф на одну персону, руб.
/// </summary>
public class BanquetDish
{
    public int Id { get; set; }

    public string DishTitle { get; set; } = string.Empty;

    public string Category { get; set; } = string.Empty;

    public int PortionWeightG { get; set; }

    public decimal CostPerPerson { get; set; }

    public ICollection<ContractMenuItem> MenuItems { get; set; } = new List<ContractMenuItem>();
}

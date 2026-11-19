namespace Catering.Core.Models;

/// <summary>
/// Строка спецификации договора (таблица contract_menu_items).
/// LineCost = ServingsCount * CostPerPerson + EquipmentRentFee.
/// </summary>
public class ContractMenuItem
{
    public int Id { get; set; }

    public int ContractId { get; set; }

    public int BanquetDishId { get; set; }

    public int ServingsCount { get; set; }

    public decimal EquipmentRentFee { get; set; }

    public decimal LineCost { get; set; }

    public CateringContract? Contract { get; set; }

    public BanquetDish? BanquetDish { get; set; }
}

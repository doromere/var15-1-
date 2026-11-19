using System;
using Catering.Core.Models;
using Catering.Core.Services;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Catering.Desktop.ViewModels;

/// <summary>Строка спецификации в редакторе Master-Detail. Изменения сразу пересчитывают итоги.</summary>
public partial class MenuItemRowViewModel : ViewModelBase
{
    private readonly ICalculationService _calc;
    private readonly Action _onChanged;

    public MenuItemRowViewModel(BanquetDish dish, int servingsCount, decimal equipmentRentFee,
        ICalculationService calc, Action onChanged)
    {
        Dish = dish;
        _servingsCount = servingsCount;
        _equipmentRentFee = equipmentRentFee;
        _calc = calc;
        _onChanged = onChanged;
        RefreshLineCost();
    }

    public BanquetDish Dish { get; }

    public string DishTitle => Dish.DishTitle;

    public string Category => Dish.Category;

    public decimal CostPerPerson => Dish.CostPerPerson;

    [ObservableProperty]
    private int _servingsCount;

    [ObservableProperty]
    private decimal _equipmentRentFee;

    [ObservableProperty]
    private decimal _lineCost;

    partial void OnServingsCountChanged(int value) { RefreshLineCost(); _onChanged(); }

    partial void OnEquipmentRentFeeChanged(decimal value) { RefreshLineCost(); _onChanged(); }

    public void RefreshLineCost() =>
        LineCost = _calc.CalculateLineCost(ServingsCount, CostPerPerson, EquipmentRentFee);

    public ContractMenuItem ToEntity() => new()
    {
        BanquetDishId = Dish.Id,
        BanquetDish = Dish,
        ServingsCount = ServingsCount,
        EquipmentRentFee = EquipmentRentFee,
        LineCost = LineCost,
    };
}

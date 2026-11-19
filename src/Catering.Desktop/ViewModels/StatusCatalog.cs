using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Threading;
using Catering.Core.Common;

namespace Catering.Desktop.ViewModels;

/// <summary>
/// Общие временные/FSM-утилиты для ViewModels (тесты над UI не тянут).
/// </summary>
public static class StatusCatalog
{
    public static readonly IReadOnlyList<string> AllFilterValues =
        new[] { "Все" }.Concat(ContractStatuses.All).ToList();

    /// <summary>Разрешённый «прямой» переход FSM для выбранного договора (без отмены).</summary>
    public static string? NextForwardStatus(string current) => current switch
    {
        ContractStatuses.Preliminary => ContractStatuses.Approved,
        ContractStatuses.Approved => ContractStatuses.Procurement,
        ContractStatuses.Procurement => ContractStatuses.Completed,
        _ => null,
    };
}

using Catering.Core.Common;
using Catering.Core.Models;
using System.Text;

namespace Catering.Core.Services;

/// <summary>
/// Экспорт полной калькуляционной сметы кейтерингового обслуживания с тайм-планом подачи курсов.
/// При выгрузке в файл в нижнюю строку документа печатается цифровая подпись SHA256
/// от конкатенации полей: [Номер]+[Дата]+[Контрагент]+[Сумма]+[КоличествоПозиций].
/// </summary>
public sealed class DocumentExportService : IDocumentExportService
{
    private static readonly (string Category, string Time)[] CourseSchedule =
    {
        ("Холодные закуски", "11:00"),
        ("Горячие закуски",  "11:30"),
        ("Первые блюда",     "11:45"),
        ("Салаты",           "12:00"),
        ("Горячие блюда",    "12:20"),
        ("Десерты",          "12:50"),
        ("Напитки",          "весь банкет"),
    };

    public string BuildEstimateText(CateringContract contract)
    {
        ArgumentNullException.ThrowIfNull(contract);

        var sb = new StringBuilder();
        string dash = new('═', 64);
        string line = new('─', 64);

        sb.AppendLine(dash);
        sb.AppendLine("  ПОЛНАЯ КАЛЬКУЛЯЦИОННАЯ СМЕТА КЕЙТЕРИНГОВОГО ОБСЛУЖИВАНИЯ");
        sb.AppendLine("  с тайм-планом подачи курсов блюд");
        sb.AppendLine(dash);
        sb.AppendLine($"Документ №      : {contract.ContractNumber}");
        sb.AppendLine($"Дата мероприятия: {contract.EventDatetime:dd.MM.yyyy HH:mm}");
        sb.AppendLine($"Ответственный   : {contract.User?.FullName ?? $"user_id={contract.UserId}"}");
        sb.AppendLine($"Контрагент      : {contract.CateringClient?.ClientName ?? $"client_id={contract.CateringClientId}"}");
        sb.AppendLine($"Телефон         : {contract.CateringClient?.Phone}");
        sb.AppendLine($"Площадка        : {contract.CateringClient?.EventLocation}");
        sb.AppendLine($"Тип мероприятия : {contract.EventType?.TypeTitle} (коэффициент площадки x{contract.EventType?.ServiceMultiplier:0.00})");
        sb.AppendLine($"Гостей          : {contract.GuestsCount}");
        sb.AppendLine($"Статус          : {contract.Status}");
        if (!string.IsNullOrWhiteSpace(contract.CancelReason))
            sb.AppendLine($"Причина отмены  : {contract.CancelReason}");
        sb.AppendLine(line);
        sb.AppendLine("СПЕЦИФИКАЦИЯ БЛЮД");
        sb.AppendLine(line);
        sb.AppendLine("  #  Блюдо                              Порций   Аренда,р   Сумма,р");
        int i = 1;
        decimal dishesSum = 0m;
        foreach (ContractMenuItem item in contract.MenuItems)
        {
            string title = (item.BanquetDish?.DishTitle ?? $"dish_id={item.BanquetDishId}");
            if (title.Length > 33) title = title[..30] + "...";
            sb.AppendLine($"  {i++,-2} {title,-33} {item.ServingsCount,6} {item.EquipmentRentFee,10:0.00} {item.LineCost,9:0.00}");
            dishesSum += item.LineCost;
        }
        sb.AppendLine(line);
        sb.AppendLine($"Сумма блюд                          : {dishesSum,14:0.00} руб.");
        if (contract.CateringClient?.IsVip == true)
            sb.AppendLine($"VIP-скидка 10%                      : -{dishesSum * 0.10m,13:0.00} руб.");
        sb.AppendLine($"Наценка за площадку x{contract.EventType?.ServiceMultiplier ?? 1m,0:0.00}"
                      + $"           :   включена в итог");
        sb.AppendLine($"Обслуживание персоналом (500 x {contract.GuestsCount})");
        sb.AppendLine($"ИТОГО ПО ДОГОВОРУ                   : {contract.TotalBudget,14:0.00} руб.");
        sb.AppendLine(line);
        sb.AppendLine("ТАЙМ-ПЛАН ПОДАЧИ КУРСОВ");
        sb.AppendLine(line);
        foreach ((string category, string time) in CourseSchedule)
        {
            bool present = contract.MenuItems.Any(m =>
                string.Equals(m.BanquetDish?.Category, category, StringComparison.OrdinalIgnoreCase));
            string marker = present ? "ЕСТЬ В МЕНЮ" : "—";
            sb.AppendLine($"  {time,-12} {category,-25} {marker}");
        }
        sb.AppendLine(line);
        sb.AppendLine($"ПОДПИСЬ SHA256: {ComputeSignature(contract)}");
        sb.AppendLine("(номер|дата|контрагент|сумма|кол-во позиций)");
        sb.AppendLine(dash);
        return sb.ToString();
    }

    public string Export(CateringContract contract, string filePath)
    {
        ArgumentNullException.ThrowIfNull(contract);
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        string ext = Path.GetExtension(filePath).ToLowerInvariant();
        string content = ext switch
        {
            ".csv" => BuildCsv(contract),
            ".html" or ".htm" => BuildHtml(contract),
            _ => BuildEstimateText(contract),
        };

        File.WriteAllText(filePath, content, Encoding.UTF8);
        return ComputeSignature(contract);
    }

    public static string ComputeSignature(CateringContract contract) =>
        Sha256Helper.ComputeDocumentSignature(
            contract.ContractNumber,
            contract.EventDatetime,
            contract.CateringClient?.ClientName ?? string.Empty,
            contract.TotalBudget,
            contract.MenuItems.Count);

    private string BuildCsv(CateringContract contract)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Номер;Дата;Контрагент;Позиция;Порций;Аренда оборудования;Сумма строки");
        foreach (ContractMenuItem item in contract.MenuItems)
        {
            string title = (item.BanquetDish?.DishTitle ?? $"dish_id={item.BanquetDishId}").Replace(';', ',');
            sb.AppendLine(string.Join(";",
                contract.ContractNumber,
                contract.EventDatetime.ToString("dd.MM.yyyy HH:mm"),
                (contract.CateringClient?.ClientName ?? string.Empty).Replace(';', ','),
                title,
                item.ServingsCount,
                item.EquipmentRentFee.ToString("0.00"),
                item.LineCost.ToString("0.00")));
        }
        sb.AppendLine($"ИТОГО;;;;;;{contract.TotalBudget:0.00}");
        sb.AppendLine($"SHA256;{ComputeSignature(contract)}");
        return sb.ToString();
    }

    private string BuildHtml(CateringContract contract)
    {
        string signature = ComputeSignature(contract);
        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html><html lang=\"ru\"><head><meta charset=\"utf-8\">");
        sb.AppendLine("<title>Смета " + contract.ContractNumber + "</title>");
        sb.AppendLine("<style>body{font-family:Georgia,serif;max-width:900px;margin:32px auto;color:#222}" +
                      "table{border-collapse:collapse;width:100%}th,td{border:1px solid #999;padding:6px 10px}" +
                      "th{background:#e8e0cf}h1{font-size:20px} .sig{font-family:Consolas,monospace;font-size:12px;word-break:break-all;background:#f4f1e8;padding:10px;border:1px dashed #888}</style>");
        sb.AppendLine("</head><body>");
        sb.AppendLine($"<h1>Полная калькуляционная смета кейтерингового обслуживания</h1>");
        sb.AppendLine($"<p><b>Документ №:</b> {contract.ContractNumber}<br>" +
                      $"<b>Дата мероприятия:</b> {contract.EventDatetime:dd.MM.yyyy HH:mm}<br>" +
                      $"<b>Контрагент:</b> {contract.CateringClient?.ClientName}<br>" +
                      $"<b>Площадка:</b> {contract.CateringClient?.EventLocation}<br>" +
                      $"<b>Ответственный:</b> {contract.User?.FullName}<br>" +
                      $"<b>Гостей:</b> {contract.GuestsCount} | <b>Статус:</b> {contract.Status}</p>");
        sb.AppendLine("<table><thead><tr><th>Блюдо</th><th>Порций</th><th>Аренда</th><th>Сумма</th></tr></thead><tbody>");
        foreach (ContractMenuItem item in contract.MenuItems)
        {
            sb.AppendLine($"<tr><td>{item.BanquetDish?.DishTitle}</td><td>{item.ServingsCount}</td>" +
                          $"<td>{item.EquipmentRentFee:0.00}</td><td>{item.LineCost:0.00}</td></tr>");
        }
        sb.AppendLine("</tbody></table>");
        sb.AppendLine($"<h2>Итого: {contract.TotalBudget:0.00} руб.</h2>");
        sb.AppendLine("<h3>Тайм-план подачи курсов</h3><ul>");
        foreach ((string category, string time) in CourseSchedule)
            sb.AppendLine($"<li>{time} — {category}</li>");
        sb.AppendLine("</ul>");
        sb.AppendLine($"<div class=\"sig\">SHA256: {signature}</div>");
        sb.AppendLine("</body></html>");
        return sb.ToString();
    }
}

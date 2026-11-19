using Catering.Core.Models;

namespace Catering.Core.Services;

/// <summary>
/// Экспорт итогового документа (калькуляционная смета с тайм-планом подачи курсов блюд)
/// в текстовый файл с сохранением реквизитов и SHA256-отпечатком.
/// </summary>
public interface IDocumentExportService
{
    /// <summary>Формирует текст сметы с тайм-планом и подписью SHA256.</summary>
    string BuildEstimateText(CateringContract contract);

    /// <summary>Сохраняет документ в .txt / .csv / .html по расширению пути. Возвращает путь и подпись.</summary>
    string Export(CateringContract contract, string filePath);
}

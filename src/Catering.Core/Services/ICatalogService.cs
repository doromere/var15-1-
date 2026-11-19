using Catering.Core.Models;

namespace Catering.Core.Services;

/// <summary>
/// Справочники: клиенты, блюда, типы мероприятий; создание нового клиента
/// (с Regex-валидацией телефона) прямо из окна оформления договора.
/// </summary>
public interface ICatalogService
{
    Task<List<CateringClient>> GetClientsAsync(CancellationToken ct = default);

    Task<List<BanquetDish>> GetBanquetDishesAsync(CancellationToken ct = default);

    Task<List<EventType>> GetEventTypesAsync(CancellationToken ct = default);

    Task<CateringClient> CreateClientAsync(CateringClient client, CancellationToken ct = default);
}

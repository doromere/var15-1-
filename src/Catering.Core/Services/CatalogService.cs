using Catering.Core.Common;
using Catering.Core.Data;
using Catering.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace Catering.Core.Services;

public sealed class CatalogService : ICatalogService
{
    private readonly Func<AppDbContext> _contextFactory;

    public CatalogService(Func<AppDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<List<CateringClient>> GetClientsAsync(CancellationToken ct = default)
    {
        using AppDbContext context = _contextFactory();
        return await context.CateringClients.AsNoTracking()
            .OrderBy(x => x.ClientName).ToListAsync(ct);
    }

    public async Task<List<BanquetDish>> GetBanquetDishesAsync(CancellationToken ct = default)
    {
        using AppDbContext context = _contextFactory();
        return await context.BanquetDishes.AsNoTracking()
            .OrderBy(x => x.Category).ThenBy(x => x.DishTitle).ToListAsync(ct);
    }

    public async Task<List<EventType>> GetEventTypesAsync(CancellationToken ct = default)
    {
        using AppDbContext context = _contextFactory();
        return await context.EventTypes.AsNoTracking()
            .OrderBy(x => x.TypeTitle).ToListAsync(ct);
    }

    public async Task<CateringClient> CreateClientAsync(CateringClient client, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(client);
        if (string.IsNullOrWhiteSpace(client.ClientName))
            throw new ArgumentException("Наименование клиента обязательно.", nameof(client));
        if (!InputValidator.IsValidPhone(client.Phone))
            throw new ArgumentException("Телефон не соответствует формату +7 (XXX) XXX-XX-XX.", nameof(client));
        if (string.IsNullOrWhiteSpace(client.EventLocation))
            throw new ArgumentException("Адрес площадки обязателен.", nameof(client));

        using AppDbContext context = _contextFactory();
        context.CateringClients.Add(client);
        await context.SaveChangesAsync(ct);
        return client;
    }
}

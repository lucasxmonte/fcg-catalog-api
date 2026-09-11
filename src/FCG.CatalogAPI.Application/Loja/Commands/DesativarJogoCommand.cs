using FCG.CatalogAPI.Application.Comum.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FCG.CatalogAPI.Application.Loja.Commands;

public class DesativarJogoHandler
{
    private const string CacheKey = "catalog:jogos:ativos";

    private readonly ICatalogDbContext _db;
    private readonly ICacheService _cache;

    public DesativarJogoHandler(ICatalogDbContext db, ICacheService cache)
    {
        _db = db;
        _cache = cache;
    }

    public async Task<bool> HandleAsync(Guid id, CancellationToken ct)
    {
        var jogo = await _db.Jogos.FirstOrDefaultAsync(j => j.Id == id, ct);
        if (jogo is null) return false;

        jogo.Desativar();
        await _db.SaveChangesAsync(ct);
        await _cache.RemoveAsync(CacheKey, ct);
        return true;
    }
}

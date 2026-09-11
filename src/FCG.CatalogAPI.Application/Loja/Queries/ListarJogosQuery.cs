using FCG.CatalogAPI.Application.Comum.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FCG.CatalogAPI.Application.Loja.Queries;

public record JogoDto(Guid Id, string Titulo, string Descricao, string Genero, decimal Preco);

public class ListarJogosHandler
{
    private const string CacheKey = "catalog:jogos:ativos";
    private static readonly TimeSpan CacheExpiry = TimeSpan.FromMinutes(5);

    private readonly ICatalogDbContext _db;
    private readonly ICacheService _cache;

    public ListarJogosHandler(ICatalogDbContext db, ICacheService cache)
    {
        _db = db;
        _cache = cache;
    }

    public async Task<List<JogoDto>> HandleAsync(CancellationToken ct)
    {
        // 1. Tenta retornar do cache
        var cached = await _cache.GetAsync<List<JogoDto>>(CacheKey, ct);
        if (cached is not null)
            return cached;

        // 2. Cache miss: consulta o banco
        var jogos = await _db.Jogos
            .AsNoTracking()
            .Where(j => j.Ativo)
            .OrderBy(j => j.Titulo)
            .Select(j => new JogoDto(j.Id, j.Titulo, j.Descricao, j.Genero, j.Preco))
            .ToListAsync(ct);

        // 3. Armazena no cache para próximas requisições
        await _cache.SetAsync(CacheKey, jogos, CacheExpiry, ct);

        return jogos;
    }
}

using FCG.CatalogAPI.Application.Comum.Interfaces;
using FCG.CatalogAPI.Domain.Loja.Entidades;

namespace FCG.CatalogAPI.Application.Loja.Commands;

public record CriarJogoCommand(string Titulo, string Descricao, string Genero, decimal Preco);

public record CriarJogoResult(Guid Id, string Titulo, decimal Preco);

public class CriarJogoHandler
{
    private const string CacheKey = "catalog:jogos:ativos";

    private readonly ICatalogDbContext _db;
    private readonly ICacheService _cache;
    private readonly ISearchService _search;

    public CriarJogoHandler(ICatalogDbContext db, ICacheService cache, ISearchService search)
    {
        _db     = db;
        _cache  = cache;
        _search = search;
    }

    public async Task<CriarJogoResult> HandleAsync(CriarJogoCommand cmd, CancellationToken ct)
    {
        var jogo = Jogo.Criar(cmd.Titulo, cmd.Descricao, cmd.Genero, cmd.Preco);
        _db.Jogos.Add(jogo);
        await _db.SaveChangesAsync(ct);
        await _cache.RemoveAsync(CacheKey, ct);
        await _search.IndexarJogoAsync(jogo, ct);
        return new CriarJogoResult(jogo.Id, jogo.Titulo, jogo.Preco);
    }
}

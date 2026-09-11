using FCG.CatalogAPI.Application.Comum.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FCG.CatalogAPI.Application.Loja.Commands;

public record AtualizarJogoCommand(string Titulo, string Descricao, string Genero, decimal Preco);
public record AtualizarJogoResult(Guid Id, string Titulo, decimal Preco);

public class AtualizarJogoHandler
{
    private const string CacheKey = "catalog:jogos:ativos";

    private readonly ICatalogDbContext _db;
    private readonly ICacheService _cache;

    public AtualizarJogoHandler(ICatalogDbContext db, ICacheService cache)
    {
        _db = db;
        _cache = cache;
    }

    public async Task<AtualizarJogoResult?> HandleAsync(Guid id, AtualizarJogoCommand cmd, CancellationToken ct)
    {
        var jogo = await _db.Jogos.FirstOrDefaultAsync(j => j.Id == id, ct);
        if (jogo is null) return null;

        jogo.Atualizar(cmd.Titulo, cmd.Descricao, cmd.Genero, cmd.Preco);
        await _db.SaveChangesAsync(ct);
        await _cache.RemoveAsync(CacheKey, ct);

        return new AtualizarJogoResult(jogo.Id, jogo.Titulo, jogo.Preco);
    }
}

using FCG.CatalogAPI.Application.Comum.Interfaces;
using FCG.CatalogAPI.Domain.Loja.Entidades;

namespace FCG.CatalogAPI.Application.Comum.Cache;

/// <summary>
/// Implementação no-op para ambientes de teste ou sem Elasticsearch configurado.
/// </summary>
public class NullSearchService : ISearchService
{
    public Task IndexarJogoAsync(Jogo jogo, CancellationToken ct = default) => Task.CompletedTask;
    public Task RemoverJogoAsync(Guid id, CancellationToken ct = default) => Task.CompletedTask;
    public Task<IReadOnlyList<JogoSearchResult>> BuscarAsync(string termo, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<JogoSearchResult>>(Array.Empty<JogoSearchResult>());
}

using FCG.CatalogAPI.Application.Comum.Interfaces;

namespace FCG.CatalogAPI.Application.Loja.Queries;

public class BuscarJogosPorTermoHandler
{
    private readonly ISearchService _search;

    public BuscarJogosPorTermoHandler(ISearchService search) => _search = search;

    public Task<IReadOnlyList<JogoSearchResult>> HandleAsync(string termo, CancellationToken ct)
        => _search.BuscarAsync(termo, ct);
}

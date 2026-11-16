using FCG.CatalogAPI.Domain.Loja.Entidades;

namespace FCG.CatalogAPI.Application.Comum.Interfaces;

public interface ISearchService
{
    Task IndexarJogoAsync(Jogo jogo, CancellationToken ct = default);
    Task RemoverJogoAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<JogoSearchResult>> BuscarAsync(string termo, CancellationToken ct = default);
}

public record JogoSearchResult(
    Guid Id,
    string Titulo,
    string Descricao,
    string Genero,
    decimal Preco,
    bool Ativo,
    double Score);

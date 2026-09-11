using FCG.CatalogAPI.Domain.Avaliacoes;

namespace FCG.CatalogAPI.Application.Comum.Interfaces;

public interface IAvaliacaoRepository
{
    Task<Avaliacao?> GetByJogoAndUsuarioAsync(Guid jogoId, Guid usuarioId, CancellationToken ct = default);
    Task<List<Avaliacao>> ListByJogoAsync(Guid jogoId, CancellationToken ct = default);
    Task<double> GetMediaNotaAsync(Guid jogoId, CancellationToken ct = default);
    Task UpsertAsync(Avaliacao avaliacao, CancellationToken ct = default);
    Task<bool> DeleteAsync(string id, Guid usuarioId, CancellationToken ct = default);
}

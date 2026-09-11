namespace FCG.CatalogAPI.Application.Comum.Interfaces;

/// <summary>
/// Abstração de cache distribuído (implementada com Redis em produção,
/// no-op em testes para não precisar de infra).
/// </summary>
public interface ICacheService
{
    Task<T?> GetAsync<T>(string key, CancellationToken ct = default);
    Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken ct = default);
    Task RemoveAsync(string key, CancellationToken ct = default);
    Task RemoveByPatternAsync(string pattern, CancellationToken ct = default);
}

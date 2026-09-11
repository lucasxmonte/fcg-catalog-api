using FCG.CatalogAPI.Application.Comum.Interfaces;

namespace FCG.CatalogAPI.Application.Comum.Cache;

/// <summary>
/// Implementação no-op de ICacheService para uso em testes e ambientes sem Redis.
/// Nunca armazena nem retorna dados — cada operação é um no-op.
/// </summary>
public class NullCacheService : ICacheService
{
    public Task<T?> GetAsync<T>(string key, CancellationToken ct = default) => Task.FromResult<T?>(default);
    public Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken ct = default) => Task.CompletedTask;
    public Task RemoveAsync(string key, CancellationToken ct = default) => Task.CompletedTask;
    public Task RemoveByPatternAsync(string pattern, CancellationToken ct = default) => Task.CompletedTask;
}

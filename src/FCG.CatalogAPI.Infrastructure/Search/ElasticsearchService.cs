using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.QueryDsl;
using FCG.CatalogAPI.Application.Comum.Interfaces;
using FCG.CatalogAPI.Domain.Loja.Entidades;
using Microsoft.Extensions.Logging;

namespace FCG.CatalogAPI.Infrastructure.Search;

public class ElasticsearchService : ISearchService
{
    private const string IndexName = "jogos";

    private readonly ElasticsearchClient _client;
    private readonly ILogger<ElasticsearchService> _logger;

    public ElasticsearchService(ElasticsearchClient client, ILogger<ElasticsearchService> logger)
    {
        _client = client;
        _logger = logger;
    }

    public async Task IndexarJogoAsync(Jogo jogo, CancellationToken ct = default)
    {
        var doc = new JogoDocument
        {
            Id      = jogo.Id,
            Titulo  = jogo.Titulo,
            Descricao = jogo.Descricao,
            Genero  = jogo.Genero,
            Preco   = jogo.Preco,
            Ativo   = jogo.Ativo
        };

        var response = await _client.IndexAsync(doc, i => i
            .Index(IndexName)
            .Id(jogo.Id.ToString()), ct);

        if (!response.IsValidResponse)
            _logger.LogWarning("Falha ao indexar jogo {Id}: {Error}", jogo.Id, response.ElasticsearchServerError?.Error?.Reason);
    }

    public async Task RemoverJogoAsync(Guid id, CancellationToken ct = default)
    {
        var response = await _client.DeleteAsync<JogoDocument>(IndexName, id.ToString(), cancellationToken: ct);

        if (!response.IsValidResponse && response.Result != Result.NotFound)
            _logger.LogWarning("Falha ao remover jogo {Id} do índice: {Error}", id, response.ElasticsearchServerError?.Error?.Reason);
    }

    public async Task<IReadOnlyList<JogoSearchResult>> BuscarAsync(string termo, CancellationToken ct = default)
    {
        var response = await _client.SearchAsync<JogoDocument>(s => s
            .Index(IndexName)
            .Size(20)
            .Query(q => q
                .Bool(b => b
                    .Must(m => m
                        .MultiMatch(mm => mm
                            .Fields(new[] { "titulo^3", "descricao", "genero^2" })
                            .Query(termo)
                            .Fuzziness(new Fuzziness("AUTO"))
                            .Type(TextQueryType.BestFields)
                        )
                    )
                )
            ), ct);

        if (!response.IsValidResponse)
        {
            _logger.LogWarning("Falha na busca por '{Termo}': {Error}", termo, response.ElasticsearchServerError?.Error?.Reason);
            return Array.Empty<JogoSearchResult>();
        }

        return response.Hits
            .Select(h => new JogoSearchResult(
                h.Source!.Id,
                h.Source.Titulo,
                h.Source.Descricao,
                h.Source.Genero,
                h.Source.Preco,
                h.Source.Ativo,
                h.Score ?? 0))
            .ToList();
    }
}

/// <summary>Documento Elasticsearch para a entidade Jogo.</summary>
internal class JogoDocument
{
    public Guid    Id        { get; set; }
    public string  Titulo    { get; set; } = string.Empty;
    public string  Descricao { get; set; } = string.Empty;
    public string  Genero    { get; set; } = string.Empty;
    public decimal Preco     { get; set; }
    public bool    Ativo     { get; set; }
}

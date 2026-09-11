using FCG.CatalogAPI.Application.Comum.Interfaces;
using FCG.CatalogAPI.Domain.Avaliacoes;
using MongoDB.Bson;
using MongoDB.Driver;

namespace FCG.CatalogAPI.Infrastructure.Persistencia;

public class MongoAvaliacaoRepository : IAvaliacaoRepository
{
    private readonly IMongoCollection<Avaliacao> _collection;

    public MongoAvaliacaoRepository(IMongoDatabase database)
    {
        _collection = database.GetCollection<Avaliacao>("avaliacoes");
        EnsureIndexes();
    }

    private void EnsureIndexes()
    {
        var indexJogoUsuario = Builders<Avaliacao>.IndexKeys
            .Ascending(a => a.JogoId)
            .Ascending(a => a.UsuarioId);
        _collection.Indexes.CreateOne(new CreateIndexModel<Avaliacao>(
            indexJogoUsuario,
            new CreateIndexOptions { Unique = true, Name = "idx_jogo_usuario" }));

        var indexJogo = Builders<Avaliacao>.IndexKeys.Ascending(a => a.JogoId);
        _collection.Indexes.CreateOne(new CreateIndexModel<Avaliacao>(
            indexJogo,
            new CreateIndexOptions { Name = "idx_jogo" }));
    }

    public async Task<Avaliacao?> GetByJogoAndUsuarioAsync(Guid jogoId, Guid usuarioId, CancellationToken ct = default)
        => await _collection
            .Find(a => a.JogoId == jogoId && a.UsuarioId == usuarioId)
            .FirstOrDefaultAsync(ct);

    public async Task<List<Avaliacao>> ListByJogoAsync(Guid jogoId, CancellationToken ct = default)
        => await _collection
            .Find(a => a.JogoId == jogoId)
            .SortByDescending(a => a.CriadoEm)
            .ToListAsync(ct);

    public async Task<double> GetMediaNotaAsync(Guid jogoId, CancellationToken ct = default)
    {
        var avaliacoes = await ListByJogoAsync(jogoId, ct);
        return avaliacoes.Count == 0 ? 0.0 : avaliacoes.Average(a => a.Nota);
    }

    public async Task UpsertAsync(Avaliacao avaliacao, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(avaliacao.Id))
            avaliacao.Id = ObjectId.GenerateNewId().ToString();
        else
            avaliacao.AtualizadoEm = DateTime.UtcNow;

        var filter = Builders<Avaliacao>.Filter.And(
            Builders<Avaliacao>.Filter.Eq(a => a.JogoId, avaliacao.JogoId),
            Builders<Avaliacao>.Filter.Eq(a => a.UsuarioId, avaliacao.UsuarioId));

        await _collection.ReplaceOneAsync(
            filter,
            avaliacao,
            new ReplaceOptions { IsUpsert = true },
            ct);
    }

    public async Task<bool> DeleteAsync(string id, Guid usuarioId, CancellationToken ct = default)
    {
        var result = await _collection.DeleteOneAsync(
            a => a.Id == id && a.UsuarioId == usuarioId, ct);
        return result.DeletedCount > 0;
    }
}

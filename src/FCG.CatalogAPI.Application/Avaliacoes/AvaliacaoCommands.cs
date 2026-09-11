using FCG.CatalogAPI.Application.Comum.Interfaces;
using FCG.CatalogAPI.Domain.Avaliacoes;
using Microsoft.EntityFrameworkCore;

namespace FCG.CatalogAPI.Application.Avaliacoes;

// ── DTOs ───────────────────────────────────────────────────────────

public record AvaliarJogoCommand(Guid JogoId, Guid UsuarioId, string NomeUsuario, int Nota, string Comentario);

public record AvaliacaoDto(
    string Id,
    Guid JogoId,
    Guid UsuarioId,
    string NomeUsuario,
    int Nota,
    string Comentario,
    DateTime CriadoEm,
    DateTime? AtualizadoEm);

public record ListarAvaliacoesResult(List<AvaliacaoDto> Avaliacoes, double MediaNota, int Total);

// ── Handler: Criar / Atualizar avaliação ─────────────────────────

public class AvaliarJogoHandler
{
    private readonly IAvaliacaoRepository _repo;
    private readonly ICatalogDbContext _db;

    public AvaliarJogoHandler(IAvaliacaoRepository repo, ICatalogDbContext db)
    {
        _repo = repo;
        _db = db;
    }

    public async Task<AvaliacaoDto> HandleAsync(AvaliarJogoCommand cmd, CancellationToken ct)
    {
        // Valida se o jogo existe e está ativo
        var jogoExiste = await _db.Jogos.FirstOrDefaultAsync(j => j.Id == cmd.JogoId, ct);
        if (jogoExiste is null || !jogoExiste.Ativo)
            throw new ArgumentException($"Jogo {cmd.JogoId} não encontrado ou inativo.");

        // Verifica se já existe avaliação deste usuário (upsert)
        var existente = await _repo.GetByJogoAndUsuarioAsync(cmd.JogoId, cmd.UsuarioId, ct);

        Avaliacao avaliacao;
        if (existente is not null)
        {
            existente.Nota = cmd.Nota;
            existente.Comentario = cmd.Comentario.Trim();
            existente.AtualizadoEm = DateTime.UtcNow;
            avaliacao = existente;
        }
        else
        {
            avaliacao = Avaliacao.Criar(cmd.JogoId, cmd.UsuarioId, cmd.NomeUsuario, cmd.Nota, cmd.Comentario);
        }

        await _repo.UpsertAsync(avaliacao, ct);
        return ToDto(avaliacao);
    }

    private static AvaliacaoDto ToDto(Avaliacao a) =>
        new(a.Id, a.JogoId, a.UsuarioId, a.NomeUsuario, a.Nota, a.Comentario, a.CriadoEm, a.AtualizadoEm);
}

// ── Handler: Listar avaliações de um jogo ────────────────────────

public class ListarAvaliacoesHandler
{
    private readonly IAvaliacaoRepository _repo;

    public ListarAvaliacoesHandler(IAvaliacaoRepository repo) => _repo = repo;

    public async Task<ListarAvaliacoesResult> HandleAsync(Guid jogoId, CancellationToken ct)
    {
        var avaliacoes = await _repo.ListByJogoAsync(jogoId, ct);
        var media = avaliacoes.Count > 0
            ? avaliacoes.Average(a => a.Nota)
            : 0.0;

        var dtos = avaliacoes.Select(a =>
            new AvaliacaoDto(a.Id, a.JogoId, a.UsuarioId, a.NomeUsuario, a.Nota, a.Comentario, a.CriadoEm, a.AtualizadoEm))
            .ToList();

        return new ListarAvaliacoesResult(dtos, Math.Round(media, 2), dtos.Count);
    }
}

// ── Handler: Remover avaliação ────────────────────────────────────

public class RemoverAvaliacaoHandler
{
    private readonly IAvaliacaoRepository _repo;

    public RemoverAvaliacaoHandler(IAvaliacaoRepository repo) => _repo = repo;

    public async Task<bool> HandleAsync(string id, Guid usuarioId, CancellationToken ct)
        => await _repo.DeleteAsync(id, usuarioId, ct);
}

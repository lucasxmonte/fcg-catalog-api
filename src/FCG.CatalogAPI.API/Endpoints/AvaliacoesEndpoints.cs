using FCG.CatalogAPI.Application.Avaliacoes;
using FCG.CatalogAPI.API.Comum;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FCG.CatalogAPI.API.Endpoints;

public static class AvaliacoesEndpoints
{
    public static void MapAvaliacoesEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/jogos/{jogoId:guid}/avaliacoes")
            .WithTags("Avaliações");

        // GET /api/jogos/{jogoId}/avaliacoes — Lista avaliações de um jogo (público)
        group.MapGet("/", async (
            Guid jogoId,
            ListarAvaliacoesHandler handler,
            CancellationToken ct) =>
        {
            var result = await handler.HandleAsync(jogoId, ct);
            return Results.Ok(result);
        })
        .WithSummary("Lista avaliações de um jogo")
        .WithDescription("Retorna todas as avaliações do jogo com média de notas. Acesso público.");

        // POST /api/jogos/{jogoId}/avaliacoes — Cria ou atualiza avaliação (autenticado)
        group.MapPost("/", async (
            Guid jogoId,
            [FromBody] AvaliarRequest req,
            AvaliarJogoHandler handler,
            ClaimsPrincipal user,
            CancellationToken ct) =>
        {
            var idClaim = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (idClaim is null)
                return Results.Unauthorized();

            var usuarioId = Guid.Parse(idClaim);
            var nomeUsuario = user.FindFirstValue(ClaimTypes.Name) ?? "Usuário";

            var cmd = new AvaliarJogoCommand(jogoId, usuarioId, nomeUsuario, req.Nota, req.Comentario);
            try
            {
                var result = await handler.HandleAsync(cmd, ct);
                return Results.Ok(result);
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new ErroResponse(ex.Message));
            }
        })
        .RequireAuthorization()
        .WithSummary("Avalia um jogo")
        .WithDescription("Cria ou atualiza a avaliação do usuário autenticado. Uma avaliação por usuário por jogo.");

        // DELETE /api/jogos/{jogoId}/avaliacoes/{id} — Remove avaliação (autenticado, própria)
        group.MapDelete("/{id}", async (
            Guid jogoId,
            string id,
            RemoverAvaliacaoHandler handler,
            ClaimsPrincipal user,
            CancellationToken ct) =>
        {
            var idClaim = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (idClaim is null)
                return Results.Unauthorized();

            var usuarioId = Guid.Parse(idClaim);
            var removido = await handler.HandleAsync(id, usuarioId, ct);
            return removido
                ? Results.NoContent()
                : Results.NotFound(new ErroResponse("Avaliação não encontrada."));
        })
        .RequireAuthorization()
        .WithSummary("Remove avaliação")
        .WithDescription("Remove a avaliação do usuário autenticado. Apenas o autor pode remover.");
    }
}

public record AvaliarRequest(int Nota, string Comentario);

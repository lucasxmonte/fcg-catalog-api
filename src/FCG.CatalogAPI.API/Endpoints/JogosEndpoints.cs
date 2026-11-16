using FCG.CatalogAPI.API.Comum;
using FCG.CatalogAPI.Application.Comum.Interfaces;
using FCG.CatalogAPI.Application.Loja.Commands;
using FCG.CatalogAPI.Application.Loja.Queries;
using Microsoft.AspNetCore.Mvc;

namespace FCG.CatalogAPI.API.Endpoints;

public static class JogosEndpoints
{
    public static IEndpointRouteBuilder MapJogosEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/jogos").WithTags("Jogos");

        group.MapGet("/", async (ListarJogosHandler handler, CancellationToken ct) =>
        {
            var jogos = await handler.HandleAsync(ct);
            return Results.Ok(jogos);
        })
        .WithName("ListarJogos")
        .WithSummary("Listar catálogo de jogos")
        .WithDescription(
            "Retorna todos os jogos ativos disponíveis no catálogo. Acesso público, não requer autenticação.")
        .Produces<List<JogoDto>>(200);

        group.MapGet("/{id:guid}", async (Guid id, BuscarJogoHandler handler, CancellationToken ct) =>
        {
            var jogo = await handler.HandleAsync(id, ct);
            return jogo is null
                ? Results.NotFound(new ErroResponse("Jogo não encontrado."))
                : Results.Ok(jogo);
        })
        .WithName("BuscarJogoPorId")
        .WithSummary("Buscar jogo por ID")
        .WithDescription(
            "Retorna os detalhes de um jogo específico pelo seu GUID. Acesso público, não requer autenticação.\n\n" +
            "**404** – nenhum jogo encontrado com o ID informado.")
        .Produces<JogoDto>(200)
        .Produces<ErroResponse>(404);

        group.MapPost("/", async (
            [FromBody] CriarJogoCommand cmd,
            CriarJogoHandler handler,
            CancellationToken ct) =>
        {
            var result = await handler.HandleAsync(cmd, ct);
            return Results.Created($"/api/jogos/{result.Id}", result);
        })
        .WithName("CriarJogo")
        .WithSummary("Cadastrar novo jogo")
        .WithDescription(
            "Adiciona um novo jogo ao catálogo da plataforma. Requer perfil Administrador.\n\n" +
            "**400** – título ausente, preço negativo ou campos obrigatórios inválidos.\n\n" +
            "**401** – token ausente ou inválido.\n\n" +
            "**403** – usuário autenticado não tem perfil Administrador.")
        .RequireAuthorization("Admin")
        .Produces<CriarJogoResult>(201)
        .Produces<ErroResponse>(400)
        .Produces<ErroResponse>(401)
        .Produces<ErroResponse>(403);

        group.MapPut("/{id:guid}", async (
            Guid id,
            [FromBody] AtualizarJogoCommand cmd,
            AtualizarJogoHandler handler,
            CancellationToken ct) =>
        {
            var result = await handler.HandleAsync(id, cmd, ct);
            return result is null
                ? Results.NotFound(new ErroResponse("Jogo não encontrado."))
                : Results.Ok(result);
        })
        .WithName("AtualizarJogo")
        .WithSummary("Atualizar dados do jogo")
        .WithDescription(
            "Atualiza título, descrição, gênero e preço de um jogo existente. Requer perfil Administrador.\n\n" +
            "**400** – dados de entrada inválidos (ex.: preço negativo).\n\n" +
            "**401** – token ausente ou inválido.\n\n" +
            "**403** – usuário autenticado não tem perfil Administrador.\n\n" +
            "**404** – nenhum jogo encontrado com o ID informado.")
        .RequireAuthorization("Admin")
        .Produces<AtualizarJogoResult>(200)
        .Produces<ErroResponse>(400)
        .Produces<ErroResponse>(401)
        .Produces<ErroResponse>(403)
        .Produces<ErroResponse>(404);

        group.MapPatch("/{id:guid}/desativar", async (Guid id, DesativarJogoHandler handler, CancellationToken ct) =>
        {
            var ok = await handler.HandleAsync(id, ct);
            return ok
                ? Results.NoContent()
                : Results.NotFound(new ErroResponse("Jogo não encontrado."));
        })
        .WithName("DesativarJogo")
        .WithSummary("Desativar jogo do catálogo")
        .WithDescription(
            "Remove o jogo da listagem pública sem excluí-lo permanentemente. Pode ser reativado a qualquer momento. Requer perfil Administrador.\n\n" +
            "**401** – token ausente ou inválido.\n\n" +
            "**403** – usuário autenticado não tem perfil Administrador.\n\n" +
            "**404** – nenhum jogo encontrado com o ID informado.")
        .RequireAuthorization("Admin")
        .Produces(204)
        .Produces<ErroResponse>(401)
        .Produces<ErroResponse>(403)
        .Produces<ErroResponse>(404);

        group.MapPatch("/{id:guid}/ativar", async (Guid id, AtivarJogoHandler handler, CancellationToken ct) =>
        {
            var ok = await handler.HandleAsync(id, ct);
            return ok
                ? Results.NoContent()
                : Results.NotFound(new ErroResponse("Jogo não encontrado."));
        })
        .WithName("AtivarJogo")
        .WithSummary("Reativar jogo no catálogo")
        .WithDescription(
            "Torna um jogo previamente desativado visível novamente no catálogo público. Requer perfil Administrador.\n\n" +
            "**401** – token ausente ou inválido.\n\n" +
            "**403** – usuário autenticado não tem perfil Administrador.\n\n" +
            "**404** – nenhum jogo encontrado com o ID informado.")
        .RequireAuthorization("Admin")
        .Produces(204)
        .Produces<ErroResponse>(401)
        .Produces<ErroResponse>(403)
        .Produces<ErroResponse>(404);

        // ── Busca avançada com Elasticsearch (fuzzy + relevância) ──────────
        group.MapGet("/search", async (
            [FromQuery] string q,
            BuscarJogosPorTermoHandler handler,
            CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(q))
                return Results.BadRequest(new ErroResponse("O parâmetro 'q' é obrigatório."));

            var resultados = await handler.HandleAsync(q, ct);
            return Results.Ok(resultados);
        })
        .WithName("BuscarJogosPorTermo")
        .WithSummary("Busca avançada de jogos (Elasticsearch)")
        .WithDescription(
            "Realiza busca fuzzy nos campos título, descrição e gênero via Elasticsearch. " +
            "Tolera erros de digitação e ordena por relevância.\n\n" +
            "**Exemplo:** `GET /api/jogos/search?q=zelda`\n\n" +
            "**400** – parâmetro 'q' ausente.")
        .Produces<List<JogoSearchResult>>(200)
        .Produces<ErroResponse>(400);

        return app;
    }
}

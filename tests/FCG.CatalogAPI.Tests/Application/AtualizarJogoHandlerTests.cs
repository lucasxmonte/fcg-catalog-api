using FluentAssertions;
using FCG.CatalogAPI.Application.Comum.Cache;
using FCG.CatalogAPI.Application.Loja.Commands;
using FCG.CatalogAPI.Domain.Loja.Entidades;

namespace FCG.CatalogAPI.Tests.Application;

public class AtualizarJogoHandlerTests
{
    private static readonly NullCacheService Cache = new();

    private static async Task<TestCatalogDbContext> DbComJogo(Jogo jogo)
    {
        var db = TestCatalogDbContext.Criar();
        db.Jogos.Add(jogo);
        await db.SaveChangesAsync(CancellationToken.None);
        return db;
    }

    [Fact]
    public async Task HandleAsync_IdNaoExiste_DeveRetornarNull()
    {
        using var db = TestCatalogDbContext.Criar();
        var handler = new AtualizarJogoHandler(db, Cache);

        var result = await handler.HandleAsync(
            Guid.NewGuid(),
            new AtualizarJogoCommand("Novo", "desc", "RPG", 50m),
            CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task HandleAsync_IdValido_DeveAtualizarERetornarResult()
    {
        var jogo = Jogo.Criar("Nome Antigo", "desc antiga", "RPG", 30m);
        using var db = await DbComJogo(jogo);
        var handler = new AtualizarJogoHandler(db, Cache);

        var result = await handler.HandleAsync(
            jogo.Id,
            new AtualizarJogoCommand("Nome Novo", "desc nova", "Ação", 99m),
            CancellationToken.None);

        result.Should().NotBeNull();
        result!.Titulo.Should().Be("Nome Novo");
        result.Preco.Should().Be(99m);

        var jogoAtualizado = db.Jogos.Single(j => j.Id == jogo.Id);
        jogoAtualizado.Titulo.Should().Be("Nome Novo");
        jogoAtualizado.Genero.Should().Be("Ação");
    }

    [Fact]
    public async Task HandleAsync_ComTituloVazio_DeveLancarArgumentException()
    {
        var jogo = Jogo.Criar("Nome Original", "desc", "RPG", 50m);
        using var db = await DbComJogo(jogo);
        var handler = new AtualizarJogoHandler(db, Cache);

        Func<Task> act = () => handler.HandleAsync(
            jogo.Id,
            new AtualizarJogoCommand("", "desc", "RPG", 50m),
            CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task HandleAsync_PrecoNegativo_DeveLancarArgumentException()
    {
        var jogo = Jogo.Criar("Jogo Válido", "desc", "Ação", 49.90m);
        using var db = await DbComJogo(jogo);
        var handler = new AtualizarJogoHandler(db, Cache);

        Func<Task> act = () => handler.HandleAsync(
            jogo.Id,
            new AtualizarJogoCommand("Jogo Válido", "desc", "Ação", -1m),
            CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task HandleAsync_PrecoZero_DevePermitir()
    {
        var jogo = Jogo.Criar("Jogo Pago", "desc", "Casual", 29.90m);
        using var db = await DbComJogo(jogo);
        var handler = new AtualizarJogoHandler(db, Cache);

        var result = await handler.HandleAsync(
            jogo.Id,
            new AtualizarJogoCommand("Jogo Gratuito", "desc", "Casual", 0m),
            CancellationToken.None);

        result.Should().NotBeNull();
        result!.Preco.Should().Be(0m);
    }

    [Fact]
    public async Task HandleAsync_JogoInativo_DeveAtualizarNormalmente()
    {
        var jogo = Jogo.Criar("Jogo Inativo", "desc original", "RPG", 20m);
        jogo.Desativar();
        using var db = await DbComJogo(jogo);
        var handler = new AtualizarJogoHandler(db, Cache);

        var result = await handler.HandleAsync(
            jogo.Id,
            new AtualizarJogoCommand("Jogo Reativado", "nova desc", "RPG", 20m),
            CancellationToken.None);

        result.Should().NotBeNull();
        result!.Titulo.Should().Be("Jogo Reativado");

        var jogoAtualizado = db.Jogos.Single(j => j.Id == jogo.Id);
        jogoAtualizado.Titulo.Should().Be("Jogo Reativado");
        jogoAtualizado.Ativo.Should().BeFalse();
    }

    [Fact]
    public async Task HandleAsync_TituloComEspacos_DeveLancarArgumentException()
    {
        var jogo = Jogo.Criar("Título Válido", "desc", "Esporte", 15m);
        using var db = await DbComJogo(jogo);
        var handler = new AtualizarJogoHandler(db, Cache);

        Func<Task> act = () => handler.HandleAsync(
            jogo.Id,
            new AtualizarJogoCommand("   ", "desc", "Esporte", 15m),
            CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>();
    }
}

using FluentAssertions;
using FCG.CatalogAPI.Application.Comum.Cache;
using FCG.CatalogAPI.Application.Loja.Commands;

namespace FCG.CatalogAPI.Tests.Application;

public class CriarJogoHandlerTests
{
    private static readonly NullCacheService Cache = new();

    [Fact]
    public async Task HandleAsync_ComDadosValidos_DevePersistirERetornarResult()
    {
        using var db = TestCatalogDbContext.Criar();
        var handler = new CriarJogoHandler(db, Cache);

        var result = await handler.HandleAsync(
            new CriarJogoCommand("Cyber Adventure", "RPG cyberpunk", "RPG", 199.90m),
            CancellationToken.None);

        result.Titulo.Should().Be("Cyber Adventure");
        result.Preco.Should().Be(199.90m);
        result.Id.Should().NotBeEmpty();

        var jogo = db.Jogos.Single(j => j.Id == result.Id);
        jogo.Titulo.Should().Be("Cyber Adventure");
        jogo.Ativo.Should().BeTrue();
    }

    [Fact]
    public async Task HandleAsync_ComPrecoZero_DevePersistir()
    {
        using var db = TestCatalogDbContext.Criar();
        var handler = new CriarJogoHandler(db, Cache);

        var result = await handler.HandleAsync(
            new CriarJogoCommand("Jogo Gratuito", "desc", "Free", 0m),
            CancellationToken.None);

        result.Preco.Should().Be(0m);
    }

    [Fact]
    public async Task HandleAsync_ComTituloVazio_DeveLancarArgumentException()
    {
        using var db = TestCatalogDbContext.Criar();
        var handler = new CriarJogoHandler(db, Cache);

        Func<Task> act = () => handler.HandleAsync(
            new CriarJogoCommand("", "desc", "RPG", 100m),
            CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*Título*");
    }

    [Fact]
    public async Task HandleAsync_ComPrecoNegativo_DeveLancarArgumentException()
    {
        using var db = TestCatalogDbContext.Criar();
        var handler = new CriarJogoHandler(db, Cache);

        Func<Task> act = () => handler.HandleAsync(
            new CriarJogoCommand("Título", "desc", "RPG", -10m),
            CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*negativo*");
    }
}

using FluentAssertions;
using FCG.CatalogAPI.Application.Comum.Cache;
using FCG.CatalogAPI.Application.Loja.Commands;
using FCG.CatalogAPI.Domain.Loja.Entidades;

namespace FCG.CatalogAPI.Tests.Application;

public class DesativarAtivarJogoHandlerTests
{
    private static readonly NullCacheService Cache = new();

    private static async Task<TestCatalogDbContext> DbComJogo(Jogo jogo)
    {
        var db = TestCatalogDbContext.Criar();
        db.Jogos.Add(jogo);
        await db.SaveChangesAsync(CancellationToken.None);
        return db;
    }

    // ---- DesativarJogoHandler ----

    [Fact]
    public async Task Desativar_IdNaoExiste_DeveRetornarFalse()
    {
        using var db = TestCatalogDbContext.Criar();
        var handler = new DesativarJogoHandler(db, Cache);

        var ok = await handler.HandleAsync(Guid.NewGuid(), CancellationToken.None);

        ok.Should().BeFalse();
    }

    [Fact]
    public async Task Desativar_JogoAtivo_DeveDesativarERetornarTrue()
    {
        var jogo = Jogo.Criar("Cyber RPG", "desc", "RPG", 50m);
        jogo.Ativo.Should().BeTrue();
        using var db = await DbComJogo(jogo);
        var handler = new DesativarJogoHandler(db, Cache);

        var ok = await handler.HandleAsync(jogo.Id, CancellationToken.None);

        ok.Should().BeTrue();
        db.Jogos.Single(j => j.Id == jogo.Id).Ativo.Should().BeFalse();
    }

    // ---- AtivarJogoHandler ----

    [Fact]
    public async Task Ativar_IdNaoExiste_DeveRetornarFalse()
    {
        using var db = TestCatalogDbContext.Criar();
        var handler = new AtivarJogoHandler(db, Cache);

        var ok = await handler.HandleAsync(Guid.NewGuid(), CancellationToken.None);

        ok.Should().BeFalse();
    }

    [Fact]
    public async Task Ativar_JogoDesativado_DeveAtivarERetornarTrue()
    {
        var jogo = Jogo.Criar("Cyber RPG", "desc", "RPG", 50m);
        jogo.Desativar();
        using var db = await DbComJogo(jogo);
        var handler = new AtivarJogoHandler(db, Cache);

        var ok = await handler.HandleAsync(jogo.Id, CancellationToken.None);

        ok.Should().BeTrue();
        db.Jogos.Single(j => j.Id == jogo.Id).Ativo.Should().BeTrue();
    }

    [Fact]
    public async Task Desativar_EntaoAtivar_DeveCiclarStatusCorreto()
    {
        var jogo = Jogo.Criar("Cyber RPG", "desc", "RPG", 50m);
        using var db = await DbComJogo(jogo);

        var desativarHandler = new DesativarJogoHandler(db, Cache);
        await desativarHandler.HandleAsync(jogo.Id, CancellationToken.None);
        db.Jogos.Single(j => j.Id == jogo.Id).Ativo.Should().BeFalse();

        var ativarHandler = new AtivarJogoHandler(db, Cache);
        await ativarHandler.HandleAsync(jogo.Id, CancellationToken.None);
        db.Jogos.Single(j => j.Id == jogo.Id).Ativo.Should().BeTrue();
    }
}

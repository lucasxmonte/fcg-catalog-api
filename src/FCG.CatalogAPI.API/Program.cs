using FCG.CatalogAPI.Application.Avaliacoes;
using FCG.CatalogAPI.Application.Biblioteca.Commands;
using FCG.CatalogAPI.Application.Comum.Interfaces;
using FCG.CatalogAPI.Application.Loja.Commands;
using FCG.CatalogAPI.Application.Loja.Queries;
using FCG.CatalogAPI.API.Endpoints;
using FCG.CatalogAPI.API.Middlewares;
using FCG.CatalogAPI.Application.Comum.Cache;
using FCG.CatalogAPI.Infrastructure.Cache;
using FCG.CatalogAPI.Infrastructure.Mensageria;
using FCG.CatalogAPI.Infrastructure.Persistencia;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using MongoDB.Driver;
using Prometheus;
using Serilog;
using StackExchange.Redis;
using System.Text;

// Em Testing, NÃO inicializa o Serilog bootstrap logger nem o UseSerilog.
// Motivo: xUnit executa classes de teste em paralelo; cada classe cria sua própria
// WebApplicationFactory, que chama Program.cs novamente. O ReloadableLogger global
// (Log.Logger) só pode ser "frozen" uma vez por processo — a segunda tentativa lança
// InvalidOperationException: "The logger is already frozen."
var isTestEnv = string.Equals(
    Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"),
    "Testing",
    StringComparison.OrdinalIgnoreCase);

if (!isTestEnv)
{
    Log.Logger = new LoggerConfiguration()
        .WriteTo.Console()
        .CreateBootstrapLogger();
}

var builder = WebApplication.CreateBuilder(args);

if (!isTestEnv)
{
    builder.Host.UseSerilog((ctx, lc) => lc
        .ReadFrom.Configuration(ctx.Configuration)
        .Enrich.FromLogContext()
        .WriteTo.Console());
}

// ── EF Core — usa InMemory se o ambiente for "Testing" OU se UseInMemoryDatabase=true no config
var useInMemory = builder.Environment.IsEnvironment("Testing")
               || builder.Configuration.GetValue<bool>("UseInMemoryDatabase");

if (useInMemory)
    builder.Services.AddDbContext<CatalogDbContext>(opt =>
        opt.UseInMemoryDatabase("fcg-catalog-tests"));
else
    builder.Services.AddDbContext<CatalogDbContext>(opt =>
        opt.UseNpgsql(builder.Configuration.GetConnectionString("Postgres")));
builder.Services.AddScoped<ICatalogDbContext>(p => p.GetRequiredService<CatalogDbContext>());

// ── Redis ─────────────────────────────────────────────────────────
if (useInMemory)
{
    // Ambiente de testes: usa cache no-op para não precisar de Redis
    builder.Services.AddSingleton<ICacheService, NullCacheService>();
}
else
{
    var redisConnection = builder.Configuration["Redis:ConnectionString"] ?? "localhost:6379";
    builder.Services.AddSingleton<IConnectionMultiplexer>(
        ConnectionMultiplexer.Connect(redisConnection));
    builder.Services.AddSingleton<ICacheService, RedisCacheService>();
}

// ── MongoDB ───────────────────────────────────────────────────────
if (!useInMemory)
{
    var mongoConnStr = builder.Configuration["MongoDB:ConnectionString"] ?? "mongodb://localhost:27017";
    var mongoDbName = builder.Configuration["MongoDB:DatabaseName"] ?? "fcg_reviews";
    builder.Services.AddSingleton<IMongoClient>(new MongoClient(mongoConnStr));
    builder.Services.AddSingleton<IMongoDatabase>(sp =>
        sp.GetRequiredService<IMongoClient>().GetDatabase(mongoDbName));
    builder.Services.AddSingleton<IAvaliacaoRepository, MongoAvaliacaoRepository>();
}

// ── Application Handlers ───────────────────────────────────────────
builder.Services.AddScoped<CriarJogoHandler>();
builder.Services.AddScoped<AtualizarJogoHandler>();
builder.Services.AddScoped<DesativarJogoHandler>();
builder.Services.AddScoped<AtivarJogoHandler>();
builder.Services.AddScoped<IniciarAquisicaoHandler>();
builder.Services.AddScoped<ListarJogosHandler>();
builder.Services.AddScoped<BuscarJogoHandler>();
builder.Services.AddScoped<BuscarPedidoHandler>();
builder.Services.AddScoped<ListarPedidosHandler>();
builder.Services.AddScoped<ReprocessarPedidoHandler>();
builder.Services.AddScoped<RegistrarItemBibliotecaHandler>();
builder.Services.AddScoped<CriarPromocaoHandler>();
builder.Services.AddScoped<AtualizarPromocaoHandler>();
builder.Services.AddScoped<EncerrarPromocaoHandler>();
builder.Services.AddScoped<ListarPromocoesHandler>();

// Avaliações (MongoDB — só registra fora do ambiente de testes)
if (!useInMemory)
{
    builder.Services.AddScoped<AvaliarJogoHandler>();
    builder.Services.AddScoped<ListarAvaliacoesHandler>();
    builder.Services.AddScoped<RemoverAvaliacaoHandler>();
}

// ── MassTransit + RabbitMQ ─────────────────────────────────────────
builder.Services.AddScoped<IEventBus, MassTransitEventBus>();

builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<PaymentProcessedConsumer>();

    x.UsingRabbitMq((ctx, cfg) =>
    {
        cfg.Host(
            builder.Configuration["RabbitMQ:Host"] ?? "localhost",
            builder.Configuration["RabbitMQ:VirtualHost"] ?? "/",
            h =>
            {
                h.Username(builder.Configuration["RabbitMQ:Username"] ?? "guest");
                h.Password(builder.Configuration["RabbitMQ:Password"] ?? "guest");
            });

        cfg.ConfigureEndpoints(ctx);
    });
});

// ── JWT (valida tokens emitidos pelo UsersAPI) ─────────────────────
var jwtSecret = builder.Configuration["Jwt:Secret"]
    ?? throw new InvalidOperationException("Jwt:Secret não configurado.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opt =>
    {
        opt.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"],
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret))
        };
    });

builder.Services.AddAuthorization(opt =>
{
    opt.AddPolicy("Admin", p => p.RequireRole("Administrador"));
});

// ── Swagger ────────────────────────────────────────────────────────
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "FCG Catalog API",
        Version = "v1",
        Description =
            "API de catálogo da plataforma FCG. Gerencia jogos, promoções, biblioteca pessoal e avaliações.\n\n" +
            "### Autenticação\n" +
            "Obtenha um token JWT em `POST /api/auth/login` na **Users API** e inclua-o no cabeçalho `Authorization: Bearer {token}`.\n\n" +
            "### Novidades Fase 3\n" +
            "- `GET /api/jogos` agora retorna com **cache Redis** (TTL 5 min)\n" +
            "- `GET /api/jogos/{id}/avaliacoes` — avaliações via **MongoDB**\n" +
            "- `POST /api/jogos/{id}/avaliacoes` — cria/atualiza avaliação (autenticado)\n\n" +
            "### Resposta de erro padrão\n" +
            "Todos os erros retornam o schema `ErroResponse` com o campo `erro` descrevendo o problema."
    });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "Informe apenas o token JWT obtido na Users API (`POST /api/auth/login`), sem o prefixo \"Bearer\". O Swagger adiciona o prefixo automaticamente."
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
    c.UseAllOfToExtendReferenceSchemas();
});

var app = builder.Build();

app.UseMiddleware<ErrorHandlingMiddleware>();

// Swagger disponível em todos os ambientes exceto Production
if (!app.Environment.IsProduction())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "FCG Catalog API v1");
        c.DocumentTitle = "FCG Catalog API";
        c.DefaultModelsExpandDepth(2);
        c.DisplayRequestDuration();
    });
}

app.UseAuthentication();
app.UseAuthorization();

// ── Prometheus metrics ─────────────────────────────────────────────
app.UseHttpMetrics();
app.MapMetrics();

app.MapGet("/health", () => Results.Ok(new
{
    status = "healthy",
    service = "FCG.CatalogAPI",
    timestamp = DateTime.UtcNow
}));

app.MapJogosEndpoints();
app.MapBibliotecaEndpoints();
app.MapPromocoesEndpoints();

if (!useInMemory)
    app.MapAvaliacoesEndpoints();

// ── Migrations automáticas ─────────────────────────────────────────
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
    if (db.Database.IsRelational())
    {
        try
        {
            await db.Database.EnsureCreatedAsync();
            if (!isTestEnv) Log.Information("✅ Schema criado/verificado (loja + biblioteca)");
        }
        catch (Exception ex)
        {
            if (!isTestEnv) Log.Fatal(ex, "❌ Falha ao criar schema. Postgres está rodando?");
            Console.Error.WriteLine($"FATAL: Falha ao criar schema: {ex.Message}");
            throw;
        }
    }
}

if (!isTestEnv) Log.Information("🚀 FCG.CatalogAPI iniciando...");
app.Run();

public partial class Program { }

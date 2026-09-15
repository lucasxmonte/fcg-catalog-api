# FCG Catalog API

Microsserviço responsável pelo **catálogo de jogos e avaliações** da plataforma FCG (Full Cycle Gaming).

## Visão Geral

| | |
|---|---|
| **Runtime** | .NET 8 — Minimal API |
| **Porta local** | `8082` (Docker: `8082:8080`) |
| **Banco principal** | PostgreSQL 16 (jogos, categorias) |
| **Cache** | Redis 7.2 (cache-aside, TTL 5 min) |
| **Avaliações** | MongoDB 7.0 (documentos) |
| **Autenticação** | JWT Bearer |
| **Swagger** | http://localhost:8082/swagger |

## Estrutura do Projeto

```
fcg-catalog-api/
├── src/
│   ├── FCG.CatalogAPI.Domain/
│   │   ├── Jogos/            # Entidade Jogo, IJogoRepository
│   │   └── Avaliacoes/       # Entidade Avaliacao (MongoDB document)
│   ├── FCG.CatalogAPI.Application/
│   │   ├── Loja/Queries/     # ListarJogosQuery (com cache-aside)
│   │   ├── Loja/Commands/    # CriarJogo, AtualizarJogo, RemoverJogo (invalida cache)
│   │   ├── Avaliacoes/       # AvaliarJogo, ListarAvaliacoes, RemoverAvaliacao
│   │   └── Comum/
│   │       ├── Interfaces/   # ICacheService
│   │       └── Cache/        # NullCacheService (no-op para testes)
│   ├── FCG.CatalogAPI.Infrastructure/
│   │   ├── Cache/            # RedisCacheService (StackExchange.Redis)
│   │   ├── Persistence/      # EF Core + PostgreSQL
│   │   └── MongoDB/          # MongoDbContext, AvaliacaoRepository
│   └── FCG.CatalogAPI.API/
│       ├── Endpoints/        # JogosEndpoints, AvaliacoesEndpoints
│       └── Program.cs
├── tests/
│   ├── FCG.CatalogAPI.Tests/           # Testes unitários
│   └── FCG.CatalogAPI.IntegrationTests/# Testes de integração
└── Dockerfile
```

## Endpoints — Jogos

| Método | Rota | Auth | Descrição |
|--------|------|:---:|---|
| `GET` | `/jogos` | ❌ | Lista todos os jogos ativos (**cacheado no Redis**) |
| `GET` | `/jogos/{id}` | ❌ | Detalhe de um jogo |
| `POST` | `/jogos` | ✅ Admin | Cadastra novo jogo (invalida cache) |
| `PUT` | `/jogos/{id}` | ✅ Admin | Atualiza jogo (invalida cache) |
| `DELETE` | `/jogos/{id}` | ✅ Admin | Remove jogo (invalida cache) |

## Endpoints — Avaliações (MongoDB)

| Método | Rota | Auth | Descrição |
|--------|------|:---:|---|
| `POST` | `/jogos/{jogoId}/avaliacoes` | ✅ | Cria/atualiza avaliação (upsert) |
| `GET` | `/jogos/{jogoId}/avaliacoes` | ❌ | Lista avaliações do jogo |
| `DELETE` | `/jogos/{jogoId}/avaliacoes` | ✅ | Remove avaliação do usuário |

## Endpoints — Infraestrutura

| Método | Rota | Descrição |
|--------|------|---|
| `GET` | `/health` | Health check |
| `GET` | `/metrics` | Métricas Prometheus |

## Cache Redis (Fase 3)

Padrão **cache-aside** implementado em `ListarJogosHandler`:

```
GET /jogos
  └─► Redis GET "catalog:jogos:ativos"
        ├─ HIT  → retorna JSON do cache
        └─ MISS → consulta PostgreSQL → Redis SET (TTL 5 min) → retorna
```

Invalidação automática ao criar, atualizar ou remover um jogo.

## Avaliações MongoDB (Fase 3)

- **Collection:** `avaliacoes`
- **Índice único:** `{ jogoId, usuarioId }` — uma avaliação por usuário por jogo
- **Operação:** `upsert` — atualiza se já existir
- **Schema:** `{ _id, jogoId, usuarioId, nota (1-5), comentario, criadoEm, atualizadoEm }`

## Variáveis de Ambiente

```env
ConnectionStrings__Postgres=Host=postgres;Database=fcg;Username=fcg_app;Password=fcg_app_dev
Redis__ConnectionString=redis:6379
MongoDB__ConnectionString=mongodb://fcg_app:fcg_app_dev@mongodb:27017
MongoDB__DatabaseName=fcg_reviews
Jwt__Secret=fcg-super-secret-key-change-in-production-min32chars!
Jwt__Issuer=fcg-users-api
Jwt__Audience=fcg-platform
RabbitMQ__Host=rabbitmq
```

## Executar Localmente

```bash
# Via Docker Compose (recomendado — sobe Redis e MongoDB automaticamente)
cd ../fcg-orchestration
docker compose up -d catalog-api

# Verificar cache no Redis
docker exec -it fcg-redis redis-cli GET "catalog:jogos:ativos"
```

## Executar Testes

```bash
dotnet test
```

---

> **FIAP Pós-Tech — Software Architecture | Tech Challenge**
> Lucas Monte Ferreri Castilho

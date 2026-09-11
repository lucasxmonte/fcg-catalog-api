# FCG.CatalogAPI

Microsserviço de **Catálogo e Biblioteca** da plataforma Fiap Cloud Games.

## Responsabilidades

- CRUD de jogos (Admin)
- Listagem pública do catálogo
- Início de aquisição de jogo → publica `OrderPlacedEvent` (retorna 202 Accepted)
- Consome `PaymentProcessedEvent` → adiciona jogo à biblioteca ou marca pedido como rejeitado

## Fluxo de compra async

```
POST /api/biblioteca/aquisicoes
        │
        ▼
  Cria Pedido (status=Pendente)
  Publica OrderPlacedEvent
        │
        ▼
  [PaymentsAPI processa]
        │
        ▼
  PaymentProcessedConsumer (CatalogAPI)
    ├─ Approved → ItemBiblioteca criado, Pedido=Aprovado
    └─ Rejected → Pedido=Rejeitado
```

## Endpoints

| Método | Rota                           | Auth    | Descrição                      |
|--------|--------------------------------|---------|--------------------------------|
| GET    | /health                        | —       | Health check                   |
| GET    | /api/jogos                     | —       | Lista catálogo ativo           |
| POST   | /api/jogos                     | Admin   | Cria novo jogo                 |
| POST   | /api/biblioteca/aquisicoes     | JWT     | Inicia compra (202 Accepted)   |
| GET    | /api/biblioteca                | JWT     | Lista biblioteca do usuário    |

## Variáveis de Ambiente

| Variável                        | Descrição                          |
|---------------------------------|------------------------------------|
| `ConnectionStrings__Postgres`   | String de conexão PostgreSQL       |
| `Jwt__Secret`                   | Segredo JWT (igual ao UsersAPI)    |
| `Jwt__Issuer`                   | `FCG.UsersAPI`                     |
| `Jwt__Audience`                 | `FCG.Platform`                     |
| `RabbitMQ__Host`                | Host do RabbitMQ                   |
| `RabbitMQ__Password`            | Senha RabbitMQ *(via Secret)*      |

## Executar localmente

```bash
docker compose up -d
# Swagger: http://localhost:8082/swagger
```

## EF Core Migrations

```bash
cd src/FCG.CatalogAPI.API
dotnet ef migrations add InitialCreate \
  --project ../FCG.CatalogAPI.Infrastructure \
  --startup-project .
dotnet ef database update
```

## Deploy Kubernetes

```bash
kubectl apply -f k8s/namespace.yaml
kubectl apply -f k8s/secret.yaml
kubectl apply -f k8s/configmap.yaml
kubectl apply -f k8s/deployment.yaml
kubectl apply -f k8s/service.yaml
```
## Grupo 17 — Pos-Tech FIAP
- Letícia Lopes Ribeiro Vasconcelos
- Lucas Monte Ferreri Castilho
- Marcelo Henrique Cornelis Rei
- Rafael Ribeiro Arantes
- Vinícius Calixto Real

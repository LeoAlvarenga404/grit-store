# Grit Store

**English summary.** Grit Store is a portfolio e-commerce for sports and outdoor gear: a **.NET 10 modular monolith** built with DDD (Identity, Catalog, Basket, Inventory, Ordering, Payments) and a minimal **Next.js** front behind a BFF. The checkout is a **MassTransit saga** with a transactional outbox: it reserves stock, authorizes payment through an `IPaymentGateway` port (fake gateway today, Stripe later) and compensates on failure. Search uses PostgreSQL full text + trigram with facets and keyset paging. Everything runs locally with `dotnet run --project src/Grit.AppHost` (.NET Aspire), with no secrets to configure. Tests run against real Postgres and Redis through Testcontainers.

---

## O que é

E-commerce de esportes e outdoor feito para portfolio. O backend .NET é a parte central: monólito modular com DDD, um schema Postgres por módulo, mensageria com outbox e uma saga de checkout com compensação. O front Next.js é mínimo e fala com a API por um BFF que guarda os tokens em cookies httpOnly.

## Como rodar

Pré-requisitos: .NET SDK 10.0.402, Docker Desktop e Node 24.

```bash
cd web && npm ci && cd ..
dotnet run --project src/Grit.AppHost
```

O AppHost sobe Postgres, Redis, a API e o Next.js, e abre o dashboard do Aspire. Em Development, a API aplica as migrations, carrega o catálogo de `seed/catalog.json` (4 categorias, 16 produtos, 30 SKUs) e dá 10 unidades de estoque para cada SKU. A chave de assinatura do JWT é gerada pelo próprio AppHost: não há segredo para configurar.

Para testar o checkout: crie uma conta, adicione itens ao carrinho e finalize. O cartão `tok_decline` é recusado, a reserva de estoque é liberada e o pedido é cancelado; qualquer outro valor é aprovado.

## Testes

```bash
dotnet test
```

Os testes de integração usam Testcontainers (Postgres e Redis reais, nunca mocks do banco), então o Docker precisa estar rodando. Hoje são 442 testes. O teste vitrine é `tests/Grit.IntegrationTests/CheckoutSagaTests.cs`: caminho feliz do checkout e pagamento recusado que libera a reserva. `StockReservationRaceTests.cs` prova que dois compradores não levam a última unidade.

## Arquitetura

- **Módulos:** `Grit.Modules.<Nome>` (`Domain/`, `Features/<CasoDeUso>/`, `Infrastructure/`) + `Grit.Modules.<Nome>.Contracts`. Um módulo só enxerga outro pelo `.Contracts`; testes de arquitetura (NetArchTest) garantem as fronteiras.
- **Dados:** um `DbContext` e um schema por módulo; EF Core para escrita, Dapper nas leituras pesadas do catálogo; concorrência otimista por `xmin`.
- **Mensagens:** MassTransit em memória com outbox EF por módulo; a saga do checkout coordena Ordering, Inventory e Payments.
- **Erros:** `Result`/`Result<T>` para falhas esperadas, ProblemDetails com `code` estável na API.
- **Identidade:** JWT de 15 minutos, refresh com rotação e detecção de reuso (a família inteira é revogada), limite de tentativas no login.

Decisões e seus motivos: `docs/architecture/design.md` e os ADRs em `docs/adr/`.

## Ordem de leitura sugerida

1. `docs/architecture/design.md`
2. `src/Grit.SharedKernel` (`Result`, `Money`, `AggregateRoot`)
3. `src/Grit.Modules.Catalog` (padrão de módulo, busca)
4. `src/Grit.Modules.Ordering` (agregado `Order`, checkout com `Idempotency-Key`, saga em `Features/Checkout/CheckoutStateMachine.cs`)
5. `src/Grit.Modules.Inventory` (reserva com `SELECT … FOR UPDATE`)
6. `tests/Grit.IntegrationTests/CheckoutSagaTests.cs` e `CheckoutTests.cs`
7. `web/lib/session` (BFF, cookies e refresh)

## Fora do escopo por enquanto

Imagens de produto em armazenamento S3 e importador do Mercado Livre, Stripe real, telas de admin, cache distribuído e testes de carga com k6. Estão no roadmap.

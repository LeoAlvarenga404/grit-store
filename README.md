# Grit Store

**English summary.** Grit Store is a portfolio e-commerce for sports and outdoor gear: a **.NET 10 modular monolith** built with DDD (Identity, Catalog, Basket, Inventory, Ordering, Payments) and a minimal **Next.js** front (store + admin) behind a BFF. The checkout is a **MassTransit saga** with a transactional outbox: it reserves stock, authorizes payment through an `IPaymentGateway` port (fake gateway by default, Stripe test mode by configuration, with a signed idempotent webhook) and compensates on failure. Search uses PostgreSQL full text + trigram with facets and keyset paging; public catalog reads go through HybridCache (memory + Redis) with post-commit tag invalidation, measured with k6 on 50k products. Product images live in S3-compatible storage (SeaweedFS) behind presigned URLs. Everything runs locally with `dotnet run --project src/Grit.AppHost` (.NET Aspire), with no secrets to configure. Tests run against real Postgres and Redis through Testcontainers.

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

Para testar o checkout: crie uma conta, adicione itens ao carrinho e finalize. Com o gateway fake (padrão), o cartão `tok_decline` é recusado, a reserva de estoque é liberada e o pedido é cancelado; qualquer outro valor, como o `pm_card_visa` que vem preenchido, é aprovado.

**Admin:** entre com `admin@grit.local`. A senha é gerada pelo AppHost na primeira execução e fica nos user secrets dele (`dotnet user-secrets list --project src/Grit.AppHost`, chave `Parameters:admin-password`), também visível nos parâmetros do dashboard do Aspire. Em `/admin` dá para:
- ajustar o estoque por SKU;
- enviar fotos dos produtos (jpeg, png ou webp até 2 MB, guardadas no SeaweedFS por URL pré-assinada);
- marcar pedidos confirmados como enviados, o que baixa o estoque.

## Como subir com Docker Compose

O AppHost gera `deploy/compose/` (`docker-compose.yaml` e `web.Dockerfile`) com `aspire publish`, e o CI falha se o arquivo versionado estiver desatualizado. Para regerar:

```bash
dotnet run --project src/Grit.AppHost --no-build -- --operation publish --step publish --output-path ../../deploy/compose
```

Para subir sem o .NET Aspire em execução:

```bash
dotnet publish src/Grit.Api -c Release -t:PublishContainer -p:ContainerRepository=grit-api
docker build -f deploy/compose/web.Dockerfile -t grit-web:latest web
cp deploy/compose/.env.example deploy/compose/.env   # troque os valores; JWT_SIGNING_KEY com 32 bytes ou mais, senhas sem ';' nem '='
docker compose -f deploy/compose/docker-compose.yaml up -d
```

A loja fica em http://localhost:3000, e o admin entra com `admin@grit.local` e o `ADMIN_PASSWORD` do `.env`. O dashboard do Aspire fica em http://localhost:18888; o token de login aparece em `docker compose -f deploy/compose/docker-compose.yaml logs compose-dashboard`.

É um ambiente de demonstração:
- a API roda em Development, para aplicar as migrations e carregar o seed;
- não há volumes, então os dados somem no `down`;
- as URLs das imagens apontam para `storage:8333`, que o navegador não alcança;
- o cookie de sessão `Secure` só funciona em `localhost`.

## Como ligar o Stripe

O padrão é o gateway fake. Para usar o Stripe em modo de teste:

1. Crie uma conta de teste no Stripe e instale o Stripe CLI (`stripe login`).
2. Rode `stripe listen --forward-to localhost:5080/api/v1/payments/stripe/webhook` e copie o `whsec_…` que ele imprime.
3. Grave as chaves nos user secrets do AppHost (nunca no repositório):
   ```bash
   dotnet user-secrets set --project src/Grit.AppHost "Payments:Provider" "Stripe"
   dotnet user-secrets set --project src/Grit.AppHost "Payments:Stripe:SecretKey" "sk_test_..."
   dotnet user-secrets set --project src/Grit.AppHost "Payments:Stripe:WebhookSecret" "whsec_..."
   ```
   Para voltar ao fake, basta `dotnet user-secrets remove --project src/Grit.AppHost "Payments:Provider"`.
4. Rode `dotnet run --project src/Grit.AppHost`. No checkout:
   - `pm_card_visa` aprova;
   - `pm_card_chargeDeclined` recusa;
   - `pm_card_visa_chargeDeclinedInsufficientFunds` mostra o motivo da recusa;
   - `pm_card_threeDSecure2Required` é recusado, porque não há 3-D Secure.
5. `stripe trigger payment_intent.succeeded` gera um evento no log da API. Reenviar o mesmo evento pelo dashboard responde 200 sem gravar nada de novo em `payments.stripe_events`.

A API não sobe com `Payments:Provider=Stripe` sem as duas chaves. O compose sempre usa o gateway fake.

## Como regerar o seed

O `seed/catalog.json` versionado foi escrito à mão. O importador `tools/Grit.MlImporter` gera um novo a partir da API de catálogo do Mercado Livre, com até 2 imagens WebP por produto em `seed/images/`. Ele precisa do token OAuth do app, que vale 6 horas:

```bash
dotnet user-secrets set "MercadoLivre:AccessToken" "<token>" --project tools/Grit.MlImporter
dotnet run --project tools/Grit.MlImporter -- --category "Calçados=tênis corrida" --category "Vestuário=camiseta dry fit" --category "Camping=barraca camping" --category "Ciclismo=capacete ciclismo" --count 5 --output seed
```

Códigos de saída: 0 ok, 1 argumentos inválidos, 2 sem token, 3 falha HTTP. O arquivo só é gravado se passar na mesma validação do seeder. As imagens aparecem quando o AppHost sobe com o armazenamento; o compose publica só o `catalog.json`.

## Testes

```bash
dotnet test
```

Os testes de integração usam Testcontainers (Postgres e Redis reais, nunca mocks do banco), então o Docker precisa estar rodando. Hoje são 625 testes. O teste vitrine é `tests/Grit.IntegrationTests/CheckoutSagaTests.cs`: caminho feliz do checkout e pagamento recusado que libera a reserva. `StockReservationRaceTests.cs` prova que dois compradores não levam a última unidade.

## Arquitetura

- **Módulos:** `Grit.Modules.<Nome>` (`Domain/`, `Features/<CasoDeUso>/`, `Infrastructure/`) + `Grit.Modules.<Nome>.Contracts`. Um módulo só enxerga outro pelo `.Contracts`; testes de arquitetura (NetArchTest) garantem as fronteiras.
- **Dados:** um `DbContext` e um schema por módulo; EF Core para escrita, Dapper nas leituras pesadas do catálogo; concorrência otimista por `xmin`.
- **Mensagens:** MassTransit em memória com outbox EF por módulo; a saga do checkout coordena Ordering, Inventory e Payments.
- **Erros:** `Result`/`Result<T>` para falhas esperadas, ProblemDetails com `code` estável na API.
- **Identidade:** JWT de 15 minutos, refresh com rotação e detecção de reuso (a família inteira é revogada), limite de tentativas no login.

Diagramas C4 e a sequência da saga: `docs/architecture/diagramas.md`. Linguagem ubíqua: `docs/glossary.md`. Decisões e seus motivos: `docs/architecture/design.md` e os ADRs em `docs/adr/`.

## Performance

As leituras públicas do catálogo passam por HybridCache (memória + Redis), invalidado depois do commit de cada escrita do admin (ADR 0017). Medição com k6 (`bench/`), em 2026-10-17, numa máquina de desenvolvimento Windows com Docker Desktop. O catálogo tinha 50.016 produtos e 100.058 SKUs, gerados com `--Seed:VolumeProducts=50000`; a primeira subida leva cerca de 70 s. A navegação no catálogo roda a 20 iterações/s por 1 minuto, com 5 requisições por iteração. Os p95 abaixo são do cenário de catálogo.

| p95 | com cache | sem cache |
|---|---|---|
| Facetas sem filtro | 43,6 ms | 32,2 s |
| Listagem sem `q` | 1,85 ms | 5,69 s |
| Busca | 1,96 ms | 168,7 ms |
| Sugestões | 1,15 ms | 136,5 ms |
| Página do produto | 2,36 ms | 141,7 ms |
| Requisições com sucesso | 100% | 0,9% |

Sem cache, uma consulta de facetas sem filtro varre todos os SKUs e leva cerca de 1,1 s mesmo com o sistema ocioso. Com 20 iterações/s, o Postgres satura, as requisições se acumulam e as conexões se esgotam. O erro se espalha para todos os endpoints, inclusive o checkout que roda logo depois: só 5% dos pedidos chegam a ser criados, e nenhum é confirmado durante a medição; com cache, 100% dos pedidos passam.

Para repetir a medição (o Docker precisa estar rodando):

```bash
dotnet run --project src/Grit.AppHost -- --Seed:VolumeProducts=50000
sh bench/run.sh with-cache
# reinicie com o cache desligado
dotnet run --project src/Grit.AppHost -- --Seed:VolumeProducts=50000 --Caching:Enabled=false
sh bench/run.sh without-cache
```

Os resultados ficam em `bench/results/`, que não é versionado.

## Ordem de leitura sugerida

1. `docs/architecture/design.md`
2. `src/Grit.SharedKernel` (`Result`, `Money`, `AggregateRoot`)
3. `src/Grit.Modules.Catalog` (padrão de módulo, busca)
4. `src/Grit.Modules.Ordering` (agregado `Order`, checkout com `Idempotency-Key`, saga em `Features/Checkout/CheckoutStateMachine.cs`)
5. `src/Grit.Modules.Inventory` (reserva com `SELECT … FOR UPDATE`)
6. `tests/Grit.IntegrationTests/CheckoutSagaTests.cs` e `CheckoutTests.cs`
7. `web/lib/session` (BFF, cookies e refresh)

## Fora do escopo por enquanto

Rodar o importador com o token real, ligar o Stripe com uma conta de teste (o código e os testes já existem; ver as seções acima) e um GIF do trace no Aspire.

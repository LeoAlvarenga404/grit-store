# Grit Store — Design

Fonte da verdade de todas as decisões do projeto. Fechado em 2026-10-14 numa sessão de perguntas e respostas (22 decisões). Os ADRs em `docs/adr/` detalham cada decisão no marco em que ela aparece no código.

## 1. Objetivo

E-commerce de esportes e outdoor para portfolio: backend .NET maduro, rápido e fácil de manter, com front Next.js mínimo. Não vai para produção, mas a infraestrutura fica pronta para subir (Aspire gera o compose).

## 2. Arquitetura

- **Monólito modular** em **.NET 10 (LTS)**. Uma API, seis módulos, um por bounded context.
- **Módulos da v1:** Identity, Catalog, Basket, Ordering, Inventory, Payments.
- **Por módulo, dois projetos:** `Grit.Modules.<Nome>` (o módulo inteiro) e `Grit.Modules.<Nome>.Contracts` (eventos de integração e DTOs públicos).
- **Dentro do módulo:** `Domain/` (agregados, value objects, eventos de domínio), `Features/<CasoDeUso>/` (vertical slice: endpoint, command/query, handler, validator), `Infrastructure/` (DbContext, configurações EF, consumers, adaptadores).
- **Fronteiras:**
  - Entre módulos: o compilador. Um módulo só referencia o `.Contracts` de outro.
  - Entre camadas: testes de arquitetura (NetArchTest). `Domain/` não referencia EF Core, MediatR, MassTransit nem ASP.NET.
- **Fora de escopo de propósito:** microserviços, Event Sourcing, Clean Architecture com 4 projetos por módulo.

## 3. Dados

- **PostgreSQL** como fonte da verdade. Um banco, **um schema por módulo** (`catalog`, `basket`, `ordering`, `inventory`, `payments`, `identity`).
- **Um `DbContext` por módulo**, que só enxerga o próprio schema, com migrations próprias. Nenhum JOIN entre schemas.
- **EF Core** para escrita e leituras simples (`AsNoTracking` por padrão nas leituras). **Dapper** só nas leituras pesadas: listagem do catálogo com filtros e facetas, "meus pedidos".
- **Redis:** carrinho (Basket) e cache L2.
- **Armazenamento S3-compatível** para imagens de produto (MinIO no plano original; ver roadmap Q-M1), com upload por URL pré-assinada.
- **Concorrência otimista** pela coluna `xmin` do Postgres.
- **Auditoria** (criado por/em, alterado por/em) preenchida por um interceptor do EF Core.

## 4. Comunicação

- **Dentro do módulo:** endpoint → `ISender` (MediatR) → handler → agregado. Pipeline behaviors: validação (FluentValidation), logging, transação.
- **Entre módulos:** eventos de integração do MassTransit v8, gravados no **outbox do EF Core** na mesma transação. Transporte **em memória** na v1; RabbitMQ trocando só a configuração.
- **Saga do checkout:** `MassTransitStateMachine` no módulo Ordering (seção 8).
- **Licenças:** MediatR em versão Apache 2.0 ou Community (sem produção, sem custo); MassTransit **v8** (Apache 2.0). O README explica a escolha.

## 5. Falhas

- **Result pattern:** `Result` / `Result<T>` próprios no SharedKernel (umas 40 linhas). `Error` com código, mensagem e tipo (`Validation`, `NotFound`, `Conflict`, `Unauthorized`, `Forbidden`, `Failure`).
- **Validação de entrada** (formato) no FluentValidation, dentro do pipeline. **Invariantes de negócio** no agregado, que retorna `Result`.
- **Exceções** só para o que é excepcional: infraestrutura fora do ar, bug.
- O endpoint converte `Error` em `ProblemDetails` (RFC 9457) com o status certo; um `IExceptionHandler` global trata o resto.

## 6. API HTTP

- **Minimal APIs.** Cada módulo expõe `Map<Modulo>Endpoints()`, e o host só compõe.
- Rotas em `/api/v1/...`, sem biblioteca de versionamento.
- **Rate limiting** nativo no login e no checkout.
- **OpenAPI nativo** (`Microsoft.AspNetCore.OpenApi`) em `/openapi/v1.json` + **Scalar** em `/scalar`, só em Development. Uma tag por módulo, `.WithSummary()`/`.WithDescription()` em cada endpoint, todos os status documentados com `ProblemDetails`, header `Idempotency-Key` documentado no checkout, botão de autenticação.
- A mesma spec gera os tipos do front (seção 11).

## 7. Identidade

- **ASP.NET Core Identity** no módulo Identity: cadastro, hash de senha, papéis (`customer`, `admin`), lockout.
- **JWT próprio:** access token de 15 min; **refresh token com rotação e detecção de reuso** (reuso de um token já trocado revoga a família inteira), guardado no banco.
- **Next.js como BFF:** tokens em cookies httpOnly, gerenciados por route handlers e Server Actions. O token nunca chega ao JavaScript do navegador.
- **Fora da v1:** login social, MFA.

## 8. Funcionalidades da v1

**Catálogo e busca**
- Produto com **SKUs** (cor × tamanho), cada SKU com preço e estoque próprios. Atributos heterogêneos por categoria em `jsonb`.
- **Busca full-text em português:** `tsvector` com dicionário `portuguese`, `unaccent` ("cafe" acha "café"), `pg_trgm` (tolerância a erro de digitação).
- **Autocomplete** (prefixo + trigram, cache no Redis).
- **Facetas com contagem:** categoria, faixa de preço, atributos (`Azul (12)`).
- **Slugs** para SEO e **paginação por keyset**.

**Carrinho**
- Anônimo, com **merge no login**. **Snapshot de preço**: o checkout avisa se o preço mudou desde que o item entrou no carrinho.

**Checkout e pedido**
- **`Idempotency-Key`** no checkout: dois cliques geram um pedido.
- **`Money`** como value object (decimal + moeda, nunca `double`).
- **CEP via ViaCEP** e **frete por tabela** (região × peso) atrás de uma interface.
- **Saga do checkout:** pedido criado → estoque reservado (expira em 15 min, mensagem agendada) → pagamento confirmado → estoque baixado → pedido confirmado. Pagamento recusado ou reserva expirada → reserva liberada, pedido cancelado.
- **Cancelamento antes do envio:** estorno + liberação de estoque (o saga ao contrário).
- **Linha do tempo do pedido** (cada mudança de estado registrada) e **número legível** (`#2026-000123`) separado do ID interno.

**Pagamento**
- `IPaymentGateway` (ACL) com duas implementações: **Stripe** (modo de teste, Stripe CLI para webhooks) e `FakePaymentGateway` (testes e uso offline).
- **Webhook com verificação de assinatura e processamento idempotente** (o Stripe reenvia eventos).

**Admin mínimo**
- Cadastro de produto/SKU, ajuste de estoque, lista de pedidos com mudança de status, upload de imagens.

**Fica para v2:** cupons e promoções, avaliações, frete real por API, parcelamento, notificações por e-mail, wishlist, relatórios, recomendações, devolução e troca, rastreio, Mercado Pago (segunda implementação do `IPaymentGateway`).

## 9. Performance

- **HybridCache** (L1 memória + L2 Redis) com proteção contra stampede e **invalidação por tag** (`catalog`), disparada por um filtro no grupo admin do Catalog depois do commit (emenda 2026-10-17, ADR 0017). **Estoque nunca vai para o cache.**
- **Sem output caching** (emenda 2026-10-17, ADR 0017): redundante com o HybridCache e congelaria as URLs pré-assinadas das imagens.
- Compiled queries no EF Core onde o perfil mostrar ganho.
- **k6** em `bench/`: carga no catálogo e no checkout, números (p95, req/s, com e sem cache) publicados no README.
- **Fora:** CDN, réplicas de leitura (uma linha no README diz onde entrariam), AOT.

## 10. Infraestrutura e observabilidade

- **.NET Aspire:** o `AppHost` declara Postgres, Redis, armazenamento S3, a API e o Next.js. `dotnet run --project src/Grit.AppHost` sobe tudo, com o dashboard do Aspire.
- **OpenTelemetry** (traces, métricas, logs) cobrindo HTTP, EF Core, Npgsql, Redis e MassTransit: um checkout aparece como **um trace** atravessando os módulos, inclusive pelo outbox.
- **Health checks** em `/health`. **Dockerfile multi-stage** da API. Logger nativo, sem Serilog.
- O compose para "subir em qualquer lugar" é **gerado pelo Aspire**.

## 11. Front (Next.js)

- **App Router.** UI **mínima** na v1: só o necessário para validar os fluxos. UI/UX completa (Tailwind, shadcn, estado no cliente) é uma fase posterior; a decisão de estilo e estado está em aberto.
- **Tipos gerados do OpenAPI:** `openapi-typescript` + `openapi-fetch`. `npm run gen:api` regenera; o CI falha se o arquivo gerado estiver desatualizado.
- Server Components chamam a API direto do servidor (catálogo, SEO). Ações autenticadas passam pelo BFF (seção 7).
- Convenção TS: kebab-case em pastas e arquivos.

## 12. Seed

- Nicho **esportes e outdoor**, com variantes cor × tamanho (vestuário, calçados) e atributos técnicos por categoria (barraca: capacidade, peso; bike: aro, marchas; garrafa: litros).
- **Seed atual (versão enxuta):** `seed/catalog.json` escrito à mão, com 4 categorias, 16 produtos e 30 SKUs, sem imagens. Quem clona roda sem token nenhum.
- **Seeder idempotente** (`CatalogSeedService`, só em Development): o AppHost passa o caminho em `Seed:Path`; tudo passa pelo domínio, numa transação só, e o que já existe (por slug) é pulado.
- **Importador** `tools/Grit.MlImporter` (emenda 2026-10-17, ADR 0019): busca por termo com a categoria da loja na linha de comando (`/products/search`, `/products/{id}`, `/products/{id}/items`), token por `dotnet user-secrets`, imagens WebP de até 800 px. O formato do seed ganha `images` (até 8 `.webp`), e o seeder de Development envia ao S3 as imagens que existirem. Os testes de integração usam uma cópia congelada do seed escrito à mão. Rodar o importador substitui o `seed/` versionado.
- **Bogus** (pt_BR): volume de produtos para o k6 com `--Seed:VolumeProducts` (5b).

## 13. Testes

| Camada | O que testa | Ferramenta |
|---|---|---|
| Domínio | Invariantes dos agregados, puro, sem mock | xUnit + Shouldly |
| Integração por módulo | Da request HTTP ao banco real: handler, EF, outbox, consumer | `WebApplicationFactory` + Testcontainers (Postgres, Redis) |
| Arquitetura | Fronteiras entre camadas e entre módulos | NetArchTest |

- **Teste vitrine:** o saga do checkout inteiro (caminho feliz e pagamento recusado com liberação da reserva).
- **Fora:** testes unitários de handler com mock de repositório, meta de cobertura em %, E2E com Playwright (fica para a fase de UI).
- FluentAssertions não é usado (licença comercial a partir da v8).

## 14. Repositório e qualidade

- **Monorepo:** `src/`, `tests/`, `web/`, `tools/`, `seed/`, `bench/`, `docs/`.
- **Central Package Management** (`Directory.Packages.props`), **`Directory.Build.props`** com `Nullable`, `TreatWarningsAsErrors` e analisadores no nível recomendado, **`.editorconfig`** verificado no CI com `dotnet format --verify-no-changes`.
- **Solution** no formato `.slnx`: `Grit.slnx`.
- **GitHub Actions** a cada push e PR: build, as 3 camadas de teste, formatação, tipos do OpenAPI atualizados, lint e build do Next.js. Badge no README.
- **Conventional Commits.**
- **Documentação:** README vitrine (resumo em inglês no topo, diagrama, GIF do trace no Aspire, números do k6, como rodar, decisões); ADRs em `docs/adr/`; C4 e o diagrama de sequência do saga em Mermaid; glossário da linguagem ubíqua em `docs/glossary.md`.
- **Fora:** Husky e hooks de pre-commit, SonarCloud, CHANGELOG automatizado.

## 15. Estilo de código

- Código em **inglês**; README, ADRs e docs em **PT-BR** (resumo em inglês no README).
- **Comentário só para o porquê não óbvio** (uma decisão, uma limitação conhecida). Nunca o "o quê". Sem XML doc (`/// <summary>`) no código interno; a documentação da API sai dos `.WithSummary()`.
- **Nomes simples e comuns** (`PlaceOrder`, `ReserveStock`, `CancelOrder`). Métodos curtos, retorno antecipado em vez de `if` aninhado.
- **Convenções .NET:** PascalCase em pastas e arquivos, arquivo com o nome exato do tipo, uma classe pública por arquivo, pasta = segmento do namespace (IDE0130), projetos com pontos (`Grit.Modules.Ordering.Contracts`). Pastas da raiz do repositório em minúsculas. O papel vai no nome do tipo: `PlaceOrderCommand`, `PlaceOrderHandler`, `PlaceOrderValidator`, `OrderingEndpoints`.
- **Namespaces:** `Grit.*` (`Grit.SharedKernel`, `Grit.Modules.Ordering`).

## 16. Marcos

Ver `.claude/plans/2026-10-14-roadmap.md` (local). Resumo: 0 Esqueleto → 1 Catalog → 2 Identity → 3 Basket → 4 Inventory + Ordering + Payments (saga) → 5 Admin + performance → 6 Vitrine.

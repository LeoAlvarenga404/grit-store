# Grit Store — Design

Fonte da verdade de todas as decisões do projeto. Fechado em 2026-10-14 numa sessão de perguntas e respostas (22 decisões). Os ADRs em `docs/adr/` detalham cada decisão no marco em que ela aparece no código.

## 1. Objetivo

E-commerce de roupas e tênis esportivos para portfolio: backend .NET maduro, rápido e fácil de manter, com uma vitrine Next.js completa. Não vai para produção, mas a infraestrutura fica pronta para subir (Aspire gera o compose).

- **Emenda 2026-10-17 (ruling UI-STORE, M13).** O nicho deixa de ser "esportes e outdoor" e passa a ser uma loja séria de roupas e calçados esportivos: camping, bike e outdoor saíram. O front deixa de ser mínimo: o M13 entrega a vitrine inteira (seção 11).

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
- **Cores com fotos próprias (emenda 2026-10-17, 13a, ADR 0020).** As cores de um produto são os valores do atributo `cor` dos SKUs. Cada cor tem até 8 fotos, e a primeira imagem da galeria é a capa.
- **Item da listagem:** o menor preço entre os SKUs que casam com os filtros; `hasPriceRange`, verdadeiro quando esses SKUs têm preços diferentes (a vitrine mostra "a partir de"); e todas as cores, cada uma com a primeira foto dela.
- **Ordenação da listagem:** `relevance` (padrão), `price_asc` ou `price_desc` (ADR 0013).
- **Página do produto:** a galeria inteira e uma galeria por cor.
- O gênero (Masculino, Feminino, Unissex) é uma faceta de atributo do produto (`genero`).
- **Busca full-text em português:** `tsvector` com dicionário `portuguese`, `unaccent` ("cafe" acha "café"), `pg_trgm` (tolerância a erro de digitação).
- **Autocomplete** (prefixo + trigram, cache no Redis).
- **Facetas com contagem:** categoria, faixa de preço, atributos (`Azul (12)`).
- **Slugs** para SEO e **paginação por keyset**.

**Carrinho**
- **Exige sessão** (emenda 2026-10-17, 13b): a API do Basket só atende autenticado, e o front manda quem não tem sessão para `/login`. O carrinho anônimo com **merge no login**, previsto aqui, não foi implementado (pendente).
- **Snapshot de preço**: o checkout avisa se o preço mudou desde que o item entrou no carrinho.
- A linha mostra a cor, o tamanho e a foto da cor do SKU (emenda 2026-10-17, 13a). Os três são lidos do catálogo a cada leitura, com a URL assinada na hora; o carrinho não guarda nenhum deles.

**Checkout e pedido**
- **`Idempotency-Key`** no checkout: dois cliques geram um pedido.
- **`Money`** como value object (decimal + moeda, nunca `double`).
- **CEP via ViaCEP** e **frete por tabela** (região × peso) atrás de uma interface.
- **Saga do checkout:** pedido criado → estoque reservado (expira em 15 min, mensagem agendada) → pagamento confirmado → estoque baixado → pedido confirmado. Pagamento recusado ou reserva expirada → reserva liberada, pedido cancelado.
- **Cancelamento antes do envio:** estorno + liberação de estoque (o saga ao contrário).
- **Linha do tempo do pedido** (cada mudança de estado registrada) e **número legível** (`#2026-000123`) separado do ID interno.
- **A linha do pedido guarda a variante comprada (emenda 2026-10-17, 13b).**
  - Slug do produto, cor e tamanho (`product_slug`, `color`, `size`, `varchar(100)`, anuláveis; migration `OrderLineVariant`), copiados no checkout do `SkuSnapshot` do catálogo, a mesma fonte do nome e do preço unitário. Pedidos feitos antes da migration respondem `null`.
  - A foto não é guardada: `GET /orders/{id}` a lê a cada request por `ICatalogReader.GetSkusAsync`, o contrato sem cache que o carrinho e o checkout já usam. É a primeira foto atual da cor do SKU, assinada por uma hora, e `null` quando a cor não tem foto ou o SKU não existe mais. O motivo: as URLs do catálogo valem uma hora e só o Catalog as assina (ADR 0016).
  - Limite: a lista `GET /orders` e a lista do admin não trazem foto nem variante.

**Pagamento**
- `IPaymentGateway` (ACL) com duas implementações: **Stripe** (modo de teste, Stripe CLI para webhooks) e `FakePaymentGateway` (testes e uso offline).
- **Webhook com verificação de assinatura e processamento idempotente** (o Stripe reenvia eventos).

**Admin mínimo**
- Cadastro de produto/SKU, ajuste de estoque, lista de pedidos com mudança de status, upload de imagens. O upload escolhe uma das cores dos SKUs do produto (emenda 2026-10-17, 13a).

**Fica para v2:** cupons e promoções, avaliações, frete real por API, parcelamento, notificações por e-mail, wishlist, relatórios, recomendações, devolução e troca, rastreio, Mercado Pago (segunda implementação do `IPaymentGateway`).

## 9. Performance

- **HybridCache** (L1 memória + L2 Redis) com proteção contra stampede e **invalidação por tag** (`catalog`), disparada por um filtro no grupo admin do Catalog depois do commit (emenda 2026-10-17, ADR 0017). **Estoque nunca vai para o cache.**
- **Sem output caching** (emenda 2026-10-17, ADR 0017): redundante com o HybridCache e congelaria as URLs pré-assinadas das imagens.
- **Chaves de cache versionadas (emenda 2026-10-17, 13a, ADR 0017).** Quando o registro guardado muda de forma, o tipo da chave ganha uma versão (`product-v2`, `search-page-v2`), para que uma entrada deixada no Redis pela versão anterior nunca seja lida.
- **Custo de assinatura:** cada item da listagem assina 1 + o número de cores URLs (a capa e a primeira foto de cada cor), contra 1 antes do 13a. Medir no k6.
- Compiled queries no EF Core onde o perfil mostrar ganho.
- **k6** em `bench/`: carga no catálogo e no checkout, números (p95, req/s, com e sem cache) publicados no README.
- **Fora:** CDN, réplicas de leitura (uma linha no README diz onde entrariam), AOT.

## 10. Infraestrutura e observabilidade

- **.NET Aspire:** o `AppHost` declara Postgres, Redis, armazenamento S3, a API e o Next.js. `dotnet run --project src/Grit.AppHost` sobe tudo, com o dashboard do Aspire.
- **OpenTelemetry** (traces, métricas, logs) cobrindo HTTP, EF Core, Npgsql, Redis e MassTransit: um checkout aparece como **um trace** atravessando os módulos, inclusive pelo outbox.
- **Health checks** em `/health`. **Dockerfile multi-stage** da API. Logger nativo, sem Serilog.
- O compose para "subir em qualquer lugar" é **gerado pelo Aspire**.

## 11. Front (Next.js)

- **App Router.** A vitrine completa chega no M13 (emenda 2026-10-17, ruling UI-STORE), no lugar da UI mínima da v1. Identidade visual e stack no ADR 0022: Tailwind v4 + shadcn, Archivo variável, preto e branco, sem biblioteca de estado no cliente.
- **Rotas (emenda 2026-10-17, 13b):**
  - o grupo `app/(store)/` reúne todas as páginas da loja (início, catálogo, produto, sacola, checkout, pedidos, login e cadastro). O `layout.tsx` dele desenha a barra de anúncio, o cabeçalho fixo e o rodapé, e lê o visitante (com sessão ou não, itens na sacola);
  - o layout raiz guarda só `<html>`, a fonte e o `Toaster` (`sonner`, no topo, região "Notificações");
  - `/admin` tem um shell neutro próprio: wordmark com o selo "Admin", navegação Produtos e Pedidos, e-mail, "Ver loja" e "Sair", conteúdo em cards brancos sobre `bg-muted/50` e tabelas do shadcn. É pensado para desktop;
  - `not-found.tsx` e `error.tsx` ficam na raiz, fora da loja, só com a barra do wordmark (`StatusPage`); a página de erro oferece "Tentar de novo" (`retry`).
- **Contagem da sacola no cabeçalho:** um contexto no cliente (`components/basket-count.tsx`) guarda o número com `useOptimistic`, alimentado pela contagem que o layout lê do servidor.
  - "Adicionar à sacola" chama uma Server Function dentro de uma transição e soma a quantidade na hora. A função devolve `{ errorCode }` (mostrado num aviso) e chama `refresh()`, que traz a contagem real.
  - As ações da sacola chamam `revalidatePath("/", "layout")` antes do redirect. Login e logout gravam cookies, o que já renderiza o layout de novo.
- **Catálogo:**
  - a URL é o único estado dos filtros: `q`, `category`, `attribute=chave:valor` repetido, `minPrice`/`maxPrice` e `sort` (a relevância, padrão da API, fica fora da URL);
  - facetas: categoria, gênero, cor (bolinhas), tamanho (P → GG, depois números), marca e preço; uma faixa de preço é cruzada com o filtro de preço já aplicado. Os filtros ativos aparecem como chips, com "Limpar filtros";
  - 24 produtos por página. "Carregar mais" é uma grade no cliente que pede a próxima página por uma Server Function, porque o navegador não alcança a API; ela recomeça quando a URL muda (`key`);
  - categoria da loja ganha banner no topo; no celular, os filtros abrem numa gaveta (`Sheet`).
- **Página do produto:**
  - a cor é `?cor=` (links com `replace` e `scroll={false}`): o servidor desenha a galeria e os SKUs dessa cor. Cor desconhecida cai na primeira; cor sem foto cai na galeria do produto;
  - galeria com miniaturas na vertical, foto principal com zoom num diálogo e arraste com scroll-snap no celular;
  - tamanhos em radios nativos, só com os SKUs que a cor tem, obrigatórios (com uma opção só, ela já vem escolhida);
  - preço do SKU escolhido; sem escolha, o menor da cor, com "a partir de" quando os preços diferem;
  - quantidade de 1 a 10, abas Descrição e Detalhes (sem os atributos de controle do vendedor) e 4 produtos relacionados da categoria.
- **Sacola e checkout:**
  - a linha mostra foto, cor e tamanho lidos do catálogo (seção 8), avisa quando o preço mudou e, indisponível, bloqueia "Finalizar compra";
  - o resumo usa um frete fixo (`lib/store-navigation.ts`, R$ 19,90), que precisa ser igual a `Order.FlatShipping`;
  - o checkout tem um formulário de cartão (número, nome, validade, código) e o botão "Usar cartão de teste", que o preenche com o 4242 4242 4242 4242. Só esse cartão é aceito: o front não envia os campos digitados, apenas o token `pm_card_visa`, que os dois gateways aprovam do mesmo jeito, então o front não sabe qual gateway está ativo. A recusa (`tok_decline`) deixou de ter caminho na tela e segue testada pela API e pela saga, até o Stripe Elements (P4) tokenizar cartões reais de teste;
  - a `Idempotency-Key` é gerada a cada renderização da página.
- **Página do pedido:** painel de situação, selo (verde ou vermelho só para confirmado ou cancelado), linhas com foto e variante que levam de volta ao produto com `?cor=`, totais e linha do tempo. "Cancelar pedido" fica atrás de um diálogo de confirmação, só enquanto o pedido está `Confirmed`. Enquanto `Placed`, a página se recarrega a cada 2 s (`<meta http-equiv="refresh">`).
- **Formulários:** `<form>` com Server Action, que postam sem JavaScript, com `select` nativo (`NativeSelect`) e radios nativos estilizados com `peer`/`has-[:checked]`. `SubmitButton` se desabilita enquanto a ação roda. Os erros voltam como `?error=<código>`, viram texto em pt-BR em cada página e aparecem em `ErrorMessage` (`role="alert"`).
- **Peças compartilhadas:** `ProductCard` (bolinhas de cor; passar o mouse ou focar troca a foto), `ColorSwatch`, `EmptyState`, `PriceSummary`, `OrderStatusBadge`, `StatusPage` e `AuthShell` (formulário + banner). Regras puras em `lib/`: `sizes.ts`, `product-variants.ts` (cor, tamanhos e variantes), `order-labels.ts` e `banners.ts`.
- **Imagens:** as fotos de produto são URLs S3 pré-assinadas, então o `next/image` roda com `unoptimized`. Ficam com `object-contain` e `mix-blend-multiply` sobre `bg-muted`, em proporção 4:5, porque as fotos dos vendedores têm fundos brancos de tamanhos diferentes. Os banners são arquivos estáticos (ADR 0022).
- **Tipos gerados do OpenAPI:** `openapi-typescript` + `openapi-fetch`. `npm run gen:api` regenera; o CI falha se o arquivo gerado estiver desatualizado.
- Server Components chamam a API direto do servidor (catálogo, SEO). Ações autenticadas passam pelo BFF (seção 7). O navegador nunca chama a API: as sugestões da busca passam por um route handler (`app/api/suggestions`).
- **Testes:** sem framework de teste de UI. Os helpers puros (ordem de tamanhos, estado da URL do catálogo, variantes do produto) rodam no runner do próprio Node (`npm test` → `node --test`, arquivos `.test.ts` pela remoção de tipos do Node 24, com `allowImportingTsExtensions`); o CI os roda depois do lint. As telas são aceitas no navegador.
- Convenção TS: kebab-case em pastas e arquivos.

## 12. Seed

- Nicho **roupas e tênis esportivos** (emenda 2026-10-17, ruling UI-STORE), com variantes cor × tamanho e preço por SKU. Seis categorias: Tênis; Camisetas e regatas; Shorts e bermudas; Calças e leggings; Moletons e jaquetas; Acessórios.
- **Fonte mista (emenda 2026-10-17, 13a, ADR 0021).** O catálogo do Mercado Livre não tem roupas, então:
  - **Tênis e Acessórios** vêm do importador do Mercado Livre (domínios `MLB-SNEAKERS`, `MLB-HATS_AND_CAPS` e `MLB-TRAVEL_AND_TRAINING_BAGS`), em `seed/catalog.json` e `seed/images/`;
  - as quatro categorias de roupa são a **linha própria GRIT**, escrita à mão em `seed/grit/catalog.json`: 12 modelos (3 por categoria), 3 cores × P, M, G e GG = 144 SKUs, um preço por cor, atributos `marca` (GRIT), `genero` e `material`. As fotos, 2 por cor e geradas com IA, ficam em `seed/grit/images/` com o nome `{slug}-{cor}-{1|2}.webp`: `{cor}` é a cor em formato de slug (`azul-marinho`), a foto 1 é a frente e a 2 é o detalhe ou as costas. `seed/grit/raw/` (as folhas brutas da IA) não é versionado.
- **Seed atual:** 32 modelos e 289 SKUs: Tênis (10) e Acessórios (10 bonés), 145 SKUs e 202 fotos importados do Mercado Livre em 2026-10-17; a linha GRIT com 12 modelos e 144 SKUs. Quem clona roda sem token nenhum.
- **Formato do seed:** `images: [{ color?, files }]`, com no máximo 8 arquivos `.webp` por cor. `color` é o valor de `cor` dos SKUs; fica de fora só para as fotos de SKUs sem cor.
- **Seeder idempotente** (`CatalogSeedService`, só em Development): o AppHost passa o caminho em `Seed:Path`, e o seeder também lê `grit/catalog.json` ao lado dele, quando existe. Os dois arquivos são validados como um catálogo só (um slug de categoria ou de produto pertence a um arquivo só) e gravados numa transação só. Tudo passa pelo domínio, o que já existe (por slug) é pulado e uma foto listada que não existe também. Por isso uma foto adicionada depois só aparece com o banco novo (com o AppHost, basta reiniciar).
- **Importador** `tools/Grit.MlImporter` (emendas 2026-10-17, ADR 0019): busca por termo e domínio, percorre a família de cada resultado e grava um produto da loja por modelo, com até 3 fotos WebP de até 800 px por cor. Substitui só `seed/catalog.json` e `seed/images/`, nunca toca em `seed/grit/` e recusa uma categoria da linha GRIT. Token por `dotnet user-secrets`. Os testes de integração usam uma cópia congelada do seed escrito à mão.
- **Bogus** (pt_BR): volume de produtos para o k6 com `--Seed:VolumeProducts` (5b).

## 13. Testes

| Camada | O que testa | Ferramenta |
|---|---|---|
| Domínio | Invariantes dos agregados, puro, sem mock | xUnit + Shouldly |
| Integração por módulo | Da request HTTP ao banco real: handler, EF, outbox, consumer | `WebApplicationFactory` + Testcontainers (Postgres, Redis) |
| Arquitetura | Fronteiras entre camadas e entre módulos | NetArchTest |
| Front (helpers puros) | Ordem de tamanhos, estado da URL do catálogo, variantes do produto | `node --test` |

- **Teste vitrine:** o saga do checkout inteiro (caminho feliz e pagamento recusado com liberação da reserva).
- **Fora:** testes unitários de handler com mock de repositório, meta de cobertura em %, E2E com Playwright e framework de teste de UI (emenda 2026-10-17, 13b: as telas são aceitas no navegador, ADR 0022).
- FluentAssertions não é usado (licença comercial a partir da v8).

## 14. Repositório e qualidade

- **Monorepo:** `src/`, `tests/`, `web/`, `tools/`, `seed/`, `bench/`, `docs/`.
- **Central Package Management** (`Directory.Packages.props`), **`Directory.Build.props`** com `Nullable`, `TreatWarningsAsErrors` e analisadores no nível recomendado, **`.editorconfig`** verificado no CI com `dotnet format --verify-no-changes`.
- **Solution** no formato `.slnx`: `Grit.slnx`.
- **GitHub Actions** a cada push e PR: build, as 3 camadas de teste, formatação, tipos do OpenAPI atualizados, lint, testes dos helpers e build do Next.js. Badge no README.
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

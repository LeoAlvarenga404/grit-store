# 0021 — Seed misto e linha própria GRIT

- **Status:** aceita
- **Data:** 2026-10-17
- **Sub-plano:** 13a

## Contexto

O M13 muda o nicho para roupas e tênis esportivos (ruling UI-STORE), com seis categorias: Tênis; Camisetas e regatas; Shorts e bermudas; Calças e leggings; Moletons e jaquetas; Acessórios. O seed vinha só do importador do Mercado Livre (ADR 0019), mas o catálogo do Mercado Livre não tem roupas: os domínios de camiseta, short, legging, calça, moletom e jaqueta devolvem zero resultados. Tênis, bonés e mochilas existem, com 2 a 6 cores por modelo.

## Decisão

- **Fonte mista.** Tênis e Acessórios vêm do importador (`seed/catalog.json` e `seed/images/`). As quatro categorias de roupa são a **linha própria GRIT**, um seed escrito à mão em `seed/grit/catalog.json`, com as fotos em `seed/grit/images/`.
- **A linha GRIT:** 12 modelos (3 por categoria), cada um com 3 cores × P, M, G e GG = 144 SKUs; um preço por cor; atributos `marca` (GRIT), `genero` e `material`. Cada cor tem 2 fotos geradas com IA, `{slug}-{cor}-1.webp` (frente) e `{slug}-{cor}-2.webp` (detalhe ou costas), com a cor em formato de slug (`camiseta-grit-dry-run-azul-marinho-1.webp`). As folhas brutas da IA ficam em `seed/grit/raw/`, que não é versionado.
- **Seeding:**
  - o seeder continua recebendo um só `Seed:Path` e lê também `grit/catalog.json` ao lado dele, quando existe; nada muda no AppHost;
  - os dois arquivos são validados como um catálogo só, antes de ler o banco: um slug de categoria ou de produto pertence a um arquivo só;
  - os dois são gravados numa transação só, e cada um envia as fotos da própria pasta `images/`;
  - uma foto listada que ainda não existe é pulada;
  - o seed é idempotente por slug e só envia fotos de produtos novos, então uma foto adicionada depois só aparece com o banco novo (com o AppHost, basta reiniciar: Postgres e Redis não têm volume).
- **Convivência com o importador.** O importador substitui só `seed/catalog.json` e `seed/images/` e nunca toca em `seed/grit/`. Ele lê a linha GRIT antes de chamar a API: uma `--category` da linha GRIT sai com código 1, um modelo com o slug de um produto GRIT é pulado, e o resultado é validado junto com a linha GRIT antes de ser gravado (ADR 0019).
- **Publicação.** O `Grit.Api.csproj` publica na imagem do compose os dois catálogos e as duas pastas de fotos (`seed/images/**` e `seed/grit/images/**`), sem copiá-los para o `bin`.

## Consequências

- Quem clona roda a loja inteira sem token: os dois arquivos estão versionados.
- Uma categoria importada nunca pode ter o nome de uma categoria GRIT; o importador recusa, e o seeder recusaria subir.
- Enquanto as fotos da linha GRIT não chegam, os produtos dela aparecem sem imagem.
- As duas fontes nomeiam as fotos de formas diferentes: o importador usa o número da cor (`{slug}-{n}-{n}.webp`), a linha GRIT usa o slug da cor.

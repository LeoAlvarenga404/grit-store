# 0019 — Importador do Mercado Livre

- **Status:** aceita
- **Data:** 2026-10-17
- **Sub-plano:** 1c (o importador que tinha sido adiado no modo rápido)

## Contexto

O seed versionado (`seed/catalog.json`) foi escrito à mão no modo rápido. O design (§12) previa gerá-lo a partir da API de catálogo do Mercado Livre, com imagens. O token OAuth é do dono do projeto, então a ferramenta precisa ser completa e testada sem ele: a execução real fica para quem tem o token.

## Decisão

- **`tools/Grit.MlImporter`**, um console que busca produtos por termo (`/products/search`), lê o produto (`/products/{id}`) e os anúncios (`/products/{id}/items`). A categoria da loja vem da linha de comando (`--category "Camping=barraca camping"`), então os endpoints de destaques e de categorias não são usados.
- **O importador reaproveita os records e o `Validate` do seeder** (referência ao módulo Catalog com `InternalsVisibleTo`). O arquivo gerado é exatamente o que o seeder aceita, e um arquivo inválido nunca é gravado.
- **Mapeamento:**
  - uma SKU por produto de catálogo, com `cor`/`tamanho` quando existirem;
  - preço = o menor anúncio em BRL, arredondado a centavos (ADR 0001, 0012); produto sem anúncio é pulado;
  - atributos seguem a regra da ADR 0009;
  - slug repetido é pulado.
- **Imagens:** até 2 por produto, convertidas para WebP com o maior lado em até 800 px, usando o SkiaSharp. O SkiaSharp é MIT; o ImageSharp tem licença dividida. Com no máximo 20 produtos por categoria, o seed fica com alguns MB.
- **O formato do seed ganha `images`** (até 8 nomes `.webp`). O seeder de Development envia as imagens que existirem direto para `products/{productId}/` no S3 e as anexa, só para os produtos que ele adiciona. Sem storage ou sem arquivo, segue sem imagem.
- **Token:** `MercadoLivre:AccessToken` nos user secrets da ferramenta, enviado só para `api.mercadolibre.com`. A ferramenta sai com códigos claros: 1 para argumentos inválidos, 2 sem token e 3 para falha HTTP (o token dura 6 horas).
- **Testes sem rede:** respostas da API gravadas à mão em fixtures, servidas por um `HttpMessageHandler` falso.

## Consequências

- Rodar o importador substitui o `seed/` inteiro. Os testes de integração leem uma cópia congelada do seed escrito à mão, então a troca não quebra as contagens.
- O seed de Development grava direto em `products/` (sem staging): só roda na subida e só para produtos novos, então não há URL de upload para abusar (ADR 0016).
- Variantes de cor e tamanho do Mercado Livre (`children_ids`) não são importadas.

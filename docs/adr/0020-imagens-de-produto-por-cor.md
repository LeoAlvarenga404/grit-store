# 0020 — Imagens de produto por cor

- **Status:** aceita
- **Data:** 2026-10-17
- **Sub-plano:** 13a

## Contexto

A vitrine do M13 (ruling UI-STORE) mostra cada cor de um produto com fotos próprias: bolinhas de cor no card, galeria por cor na página do produto e a foto da cor certa na linha do carrinho. Até o 5a, o produto guardava só uma lista de chaves (`image_keys text[]`, até 8 imagens), sem saber que cor cada foto mostra (ADR 0016).

## Decisão

- **Modelo.** O produto guarda uma lista ordenada de `ProductImage(Key, Color)` em `catalog.products.images` (`jsonb`, com as chaves `key` e `color`), no lugar de `image_keys`. Não é uma tabela: uma tabela própria não mexeria no `xmin` do produto, e a escrita de imagens escaparia da concorrência otimista (ADR 0007). A primeira imagem é a capa.
- **Cor de uma imagem** = o valor do atributo `cor` de um SKU (`Sku.ColorAttribute`), comparado de forma ordinal (o importador e a linha GRIT gravam em minúsculas). `null` só é aceito quando algum SKU não tem `cor`. Um produto sem SKUs ainda não recebe imagens. Nenhuma cor é inventada ("Não informada").
- **Invariantes, no agregado:**
  - chave fora de `products/{productId}/` → `catalog.invalid_image_key` (400);
  - chave repetida → `catalog.duplicate_image` (409, novo);
  - cor que nenhum SKU tem → `catalog.image_color_not_found` (400, novo);
  - no máximo 8 imagens **por cor** → `catalog.image_limit` (409).
  - Confirmar de novo a mesma chave com a mesma cor não muda nada e devolve a versão atual; a mesma chave com outra cor é 409 `catalog.duplicate_image`.
- **Ordem das cores** (`ProductColors.Group`): primeiro as cores com imagem, na ordem da primeira imagem de cada uma, depois as outras por nome. Assim a primeira cor mostra a capa; a capa só difere da primeira foto da primeira cor quando a primeira imagem do produto não tem cor.
- **Migração dos dados** (`ProductImageColors`, SQL escrito à mão, ADR 0008): cada chave existente recebe a `cor` do primeiro SKU do produto (por id); um produto sem SKUs fica com `null`. O `Down` devolve as chaves na ordem e perde as cores.
- **Exposição na API:**
  - listagem: `colors: [{ name, imageUrl }]` (a primeira foto de cada cor) e `hasPriceRange` (ADR 0013);
  - página do produto: `colors: [{ name, imageUrls }]`, além de `imageUrls` (a galeria inteira) e `skus`;
  - linha do carrinho: a primeira imagem da cor do SKU; para um SKU sem cor, a primeira imagem sem cor. Nunca a foto de outra cor: sem foto da cor, `imageUrl` é `null`;
  - admin: o GET do produto devolve `color` de cada imagem, e a confirmação do upload recebe `color` (opcional no contrato, ADR 0011).
- O seed agrupa as fotos por cor (`images: [{ color?, files }]`) e passa pelas mesmas regras do domínio antes de ler o banco (ADR 0021).

## Consequências

- Todas as URLs continuam assinadas a cada resposta e o cache guarda só chaves (ADR 0016, 0017); a listagem passa a assinar 1 + o número de cores URLs por item.
- O admin escolhe a cor no upload entre as cores dos SKUs, então o SKU precisa existir antes da foto.
- Limites conhecidos:
  - um atributo `cor` no nível do produto é ignorado pelas cores e pelas fotos, embora os filtros ainda o mesclem por baixo do atributo do SKU (ADR 0013);
  - imagens antigas sem cor, num produto que depois ganha SKUs com cor, ficam só na galeria inteira: não entram em nenhuma cor nem na linha do carrinho de um SKU com cor.

# 0010 — Slug único e imutável

- **Status:** aceita
- **Data:** 2026-10-15
- **Sub-plano:** 1a

## Contexto

Categorias e produtos precisam de um identificador legível para URLs públicas (1b). Renomear não pode quebrar links, e dois cadastros com nomes equivalentes não podem gerar o mesmo endereço.

## Decisão

- **Formato:** o slug vem do nome, em ASCII minúsculo, sem acentos (`ê` → `e`); qualquer outra sequência de caracteres vira um único `-`; no máximo 100 caracteres, sem `-` no fim. Um nome sem letra nem dígito é `catalog.empty_slug` (400). Aplicar a regra a um slug devolve o mesmo slug, e é assim que o EF o restaura.
- **Imutável:** o slug é calculado na criação e nunca muda num rename (sem redirecionamentos na v1).
- **Único por tabela**, com duas defesas:
  - o handler consulta antes de inserir e responde `catalog.category_slug_taken` / `catalog.product_slug_taken` (409);
  - um envio duplo concorrente passa pelas duas consultas antes de qualquer commit, e o segundo bate no índice único. O handler traduz essa violação (SqlState `23505` no índice `ix_*_slug`) para o mesmo 409. A revisão mediu 19 de 20 envios duplos chegando ao índice, então isso não é um caso teórico.

## Consequências

- O plano do 1a aceitava o 500 na corrida como limite conhecido; a medição derrubou essa premissa.
- Outros módulos com chave natural única (por exemplo, e-mail no Identity) podem repetir o padrão: consulta prévia mais tradução da violação do índice.

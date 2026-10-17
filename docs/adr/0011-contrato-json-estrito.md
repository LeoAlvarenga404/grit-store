# 0011 — Contrato JSON estrito

- **Status:** aceita
- **Data:** 2026-10-15
- **Sub-plano:** 1a
- **Complementa:** ADR 0006

## Contexto

O front usa tipos TypeScript gerados do OpenAPI. Os primeiros endpoints com números e campos opcionais mostraram que os padrões do ASP.NET produzem um contrato frouxo, e que algumas entradas válidas em JSON viravam 500 no banco.

## Decisão

- **Números estritos:** `JsonNumberHandling.Strict` na API. Os padrões web também aceitam números como string, e isso tipava todo número como `integer | string` (com `pattern`) no OpenAPI e no TypeScript. Agora `version` é `integer`/`uint32` e vira `number`.
- **Coleções opcionais com padrão `null`:** `attributes` tem `= null` no record do request, sai de `required` no OpenAPI e o endpoint trata ausência como mapa vazio.
- **NUL rejeitado na validação:** o PostgreSQL não aceita U+0000 em texto nem em `jsonb`. Nome, descrição e valores de atributos com NUL respondem 400 `validation.failed` com o erro no campo, em vez de 500 na gravação.
- **Id inexistente:** no corpo é 400 (`catalog.category_not_found`, o dado enviado é inválido); na URL é 404 (`catalog.product_not_found`, `catalog.sku_not_found`, o recurso não existe).

## Consequências

- Um cliente que mande número como string recebe 400.
- O schema `ProblemDetails` gerado ainda não declara `code` nem `errors`; o front lê esses campos com um cast até a primeira tela que precise deles (pendência registrada).

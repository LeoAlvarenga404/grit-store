# 0009 — Nomes em snake_case e atributos em `jsonb`

- **Status:** aceita
- **Data:** 2026-10-15
- **Sub-plano:** 1a

## Contexto

O sub-plano 1b escreve SQL à mão (Dapper, `tsvector`), e identificadores PascalCase entre aspas deixariam esse SQL ruidoso. Os produtos de esporte têm atributos que variam por categoria (`capacidade`, `peso`) e variantes que variam por SKU (`cor`, `tamanho`).

## Decisão

- **snake_case em tudo** pelo `EFCore.NamingConventions` (`UseSnakeCaseNamingConvention()`): tabelas, colunas, chaves, índices e as colunas da tabela de histórico de migrations, que fica no schema de cada módulo.
- **Atributos como `IReadOnlyDictionary<string, string>` em `jsonb`**, no produto e no SKU. São gravados por um conversor para texto e um `ValueComparer` estrutural, sem ligar o JSON dinâmico no data source compartilhado da API.
- **Regra de formato (FluentValidation, `AttributesRule`):** até 20 entradas, chaves de 1 a 50 caracteres em `a-z`, `0-9` e `-`, valores de 1 a 100 caracteres sem NUL nem U+FFFE (a regra de texto do 1b rejeita os dois).
- **Invariante (agregado):** dois SKUs do mesmo produto não podem ter o mesmo conjunto de atributos, em qualquer ordem (`catalog.duplicate_variant`, 409).

## Consequências

- Uma consulta Dapper lê as colunas com aliases ou `MatchNamesWithUnderscores`.
- Se o snake_case se mostrar errado, o custo é uma migration de renomeação.
- O agregado guarda uma cópia dos dicionários recebidos; quem chamou não altera o produto depois.

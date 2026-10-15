# 0001 — `Money` em `decimal`, sem arredondamento e com teto

- **Status:** aceita
- **Data:** 2026-10-15
- **Sub-plano:** 0a

## Contexto

O design (§8) pede dinheiro em `decimal` com moeda, nunca `double`. A v1 só soma valores e multiplica por quantidades inteiras (itens do carrinho, linhas do pedido). Cupons percentuais, que exigiriam uma regra de arredondamento, ficam para a v2.

Na revisão da leva 2, o Codex mostrou que `System.Decimal` guarda só 28 a 29 dígitos significativos: em valores na casa de 10^28, `Add` e `Multiply` descartam os centavos sem avisar.

## Decisão

- `Money` é um `record` com `decimal Amount` e `string Currency`, criado só por `Money.Create`, que devolve `Result<Money>`.
- **Sem arredondamento.** `Create` rejeita valores com mais de duas casas decimais (`money.too_many_decimal_places`) em vez de arredondar.
- **Teto de 1.000.000.000.000,00.** `Create` rejeita valores acima disso (`money.amount_too_large`). `Add` e `Multiply` lançam `OverflowException` se o resultado passar do teto. Com o teto e quantidades `int`, todo resultado cabe em no máximo 25 dígitos significativos, então a aritmética é sempre exata.
- A moeda é validada só pelo formato: três letras ASCII maiúsculas. Não há tabela ISO 4217.
- Somar moedas diferentes ou multiplicar por quantidade negativa é bug, não regra de negócio: lança exceção em vez de devolver `Result`.

## Consequências

- No banco, `numeric(15,2)` comporta qualquer valor válido.
- Duas casas decimais servem para BRL e USD. Uma moeda com outra quantidade de casas (JPY, BHD) exigiria os dados de casas decimais da ISO 4217.
- Quando os cupons percentuais chegarem, a regra de arredondamento é escolhida ali, com teste próprio.
- `Money` tem construtor privado: fica fora dos `.Contracts` e dos DTOs HTTP, que usam tipos primitivos.

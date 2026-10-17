# 0012 — Uma moeda na loja: BRL

- **Status:** aceita
- **Data:** 2026-10-15
- **Sub-plano:** 1a

## Contexto

`Money` (ADR 0001) carrega a moeda e lança exceção ao somar moedas diferentes. Carrinho e pedido somam preços de vários SKUs.

## Decisão

- O catálogo só aceita preços em BRL: `Product.AddSku` e `ChangeSkuPrice` rejeitam outra moeda com `catalog.unsupported_currency` (400).
- O formato na API continua `priceAmount` + `priceCurrency` (ADR 0001: `Money` não vai para o fio), para que uma segunda moeda no futuro não mude o contrato.

## Consequências

- Os totais de carrinho e pedido sempre somam, sem conversão.
- Vender em outra moeda exige decidir conversão e arredondamento antes; até lá, a regra fica no agregado.

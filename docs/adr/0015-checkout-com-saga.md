# 0015 — Checkout com saga

- **Status:** aceita
- **Data:** 2026-10-16
- **Sub-planos:** 3, 4a, 4b, 4c (só o gateway fake) e 4d, entregues juntos no modo rápido

## Contexto

Finalizar uma compra envolve três módulos que não compartilham transação: Ordering (pedido), Inventory (estoque) e Payments (cobrança). Um erro no meio do caminho não pode deixar estoque preso nem cobrar um pedido rejeitado.

## Decisão

- **Carrinho só para cliente autenticado**, guardado no Redis (`basket:{customerId}`, 30 dias). Cada linha guarda o preço que o cliente viu; `GET /basket` mostra também o preço atual.
- **Checkout em duas etapas.** Uma query lê o carrinho e os preços atuais (`ICatalogReader`) sem abrir transação. Depois, o `PlaceOrderCommand` grava o pedido numa transação só (ADR 0005). O cliente envia `expectedSubtotalAmount`; se o subtotal atual for outro, a resposta é 409 `ordering.price_changed`.
- **Idempotência:** o header `Idempotency-Key` é obrigatório, com índice único em `(customer_id, idempotency_key)`. Repetir a chave devolve o mesmo pedido. Um envio duplicado concorrente bate no índice e recebe 409 `ordering.checkout_in_progress`.
- **Saga do MassTransit** no Ordering, com repositório EF pessimista: `Reserving → Authorizing → Confirmed`.
  - Sem estoque: o pedido é cancelado com motivo `out_of_stock`.
  - Pagamento recusado: a saga publica `ReleaseStock` e cancela o pedido com motivo `payment_declined`.
  - Cancelamento pelo cliente (só a partir de `Confirmed`): a saga libera o estoque e estorna o pagamento.
  - O estado da saga, a mudança do pedido e o outbox são gravados na mesma transação.
- **Reserva de estoque** com `SELECT … ORDER BY sku_id FOR UPDATE`: reserva todas as linhas ou nenhuma. Dois compradores disputando a última unidade nunca levam os dois. A reserva vale 15 minutos e expira por consulta de horário, sem agendador (o agendamento em memória não é durável).
- **Pagamento** pela porta `IPaymentGateway`. Hoje existe só o `FakePaymentGateway`, que recusa o token `tok_decline`. O Stripe entra depois como segunda implementação.
- **Frete fixo** de R$ 19,90, sem ViaCEP.

## Consequências

- A confirmação ignora a expiração: um pagamento que chegue depois de 15 minutos pode vender além do estoque. A correção vem com um gateway lento (Stripe), num ramo `StockCommitFailed`.
- Carrinho sem `WATCH`: duas abas editando ao mesmo tempo podem perder uma alteração.
- "Meus pedidos" não tem paginação.
- Uma coleção owned cuja chave composta inclui um valor definido no código precisa de `ValueGeneratedNever()` nessa parte; sem isso, o EF grava a reserva nova como UPDATE e falha. O teste de corrida pegou o erro.
- Os três limites estão marcados com `ponytail:` no código.

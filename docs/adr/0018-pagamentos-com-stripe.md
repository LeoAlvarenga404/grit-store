# 0018 — Pagamentos com Stripe

- **Status:** aceita
- **Data:** 2026-10-17
- **Sub-plano:** 4c

## Contexto

A saga do checkout (ADR 0015) autoriza o pagamento de forma síncrona pela porta `IPaymentGateway`, que até aqui só tinha o `FakePaymentGateway`. O Stripe é a segunda implementação. As chaves são do dono do projeto, então o código e os testes não podem depender delas: tudo funciona sem conta, e ligar o Stripe é só configuração.

## Decisão

- **O `paymentToken` do checkout é um PaymentMethod do Stripe** (`pm_card_visa` aprova, `pm_card_chargeDeclined` recusa no modo de teste). A cobrança é confirmada e capturada na hora: a saga não tem passo de captura, e o cancelamento já envia `RefundPayment`, que vira um estorno simples. Com `error_on_requires_action` e o tipo `card` explícito (`allowed_payment_method_types`), o Stripe recusa 3-D Secure em vez de deixar a intent esperando um cliente que o checkout não tem para onde mandar. O tipo precisa ser explícito: o Stripe rejeita `error_on_requires_action` em intents com métodos automáticos (gerenciados pelo dashboard), o que só apareceu no primeiro teste com uma conta real (2026-10-17).
- **Idempotência por pedido.** A porta passa a receber o id do pedido. A cobrança usa a chave `authorize-{orderId}` e o estorno `refund-{paymentIntentId}`, então uma mensagem reentregue nunca cobra nem estorna duas vezes.
- **Recusa × falha.** Cartão recusado ou PaymentMethod desconhecido viram `payments.declined`, com o motivo do Stripe na mensagem. Qualquer outro erro lança exceção: uma chave de API errada também é um `invalid_request_error`, e não pode recusar silenciosamente todos os pedidos.
- **Provedor por configuração, com falha na subida.** `Payments:Provider` aceita `Fake` (o padrão, inclusive em testes, no compose e num clone novo) ou `Stripe`. Com `Stripe`, faltando `Payments:Stripe:SecretKey` ou `Payments:Stripe:WebhookSecret`, a API não sobe.
- **O webhook é uma trilha de auditoria, não um segundo caminho de confirmação.** A saga já recebeu a resposta da chamada síncrona; publicar de novo a partir do `payment_intent.succeeded` mandaria um evento para uma saga que já saiu de `Authorizing`. Comportamento de `POST /api/v1/payments/stripe/webhook`:
  - responde 404 enquanto não há segredo configurado;
  - verifica a assinatura (`Stripe-Signature`, tolerância de 300 s) e responde 400 `payments.invalid_signature` se ela não bater;
  - grava o id do evento uma vez em `payments.stripe_events`, com `ON CONFLICT DO NOTHING`, e loga a primeira entrega (disputa como aviso);
  - responde 200 também para as reentregas;
  - fica fora do OpenAPI.
- **AppHost:** repassa as chaves dos próprios user secrets só em run mode e só quando o provedor está configurado. O compose gerado não muda, e os testes nunca leem as chaves de quem está desenvolvendo.
- **Testes sem rede:** o gateway é testado com um `HttpMessageHandler` falso que devolve respostas gravadas do Stripe, e o webhook com assinaturas calculadas no teste com um segredo de teste.

## Consequências

- 3-D Secure e métodos com redirect são recusados.
- Uma intent que não termine em `succeeded` é recusada sem ser cancelada. Isso está marcado com `ponytail:` no código.
- Disputas só viram log de aviso.
- O caso de gateway lento da ADR 0015 (confirmação depois dos 15 minutos da reserva) continua em aberto.
- O módulo Payments ganhou o schema `payments` com a migration `InitialPayments` (ADR 0008).

import Link from "next/link";
import { api } from "@/lib/api/client";
import { callApi } from "@/lib/api/call-api";
import { formatPrice } from "@/lib/format";
import { placeOrder } from "./actions";

const errorMessages: Record<string, string> = {
  "ordering.basket_empty": "Seu carrinho está vazio.",
  "ordering.sku_unavailable": "Um item do carrinho ficou indisponível. Revise o carrinho.",
  "ordering.price_changed": "Um preço mudou desde que você abriu esta página. Confira o novo subtotal.",
  "ordering.checkout_in_progress": "Este pedido ainda está sendo criado. Veja seus pedidos em instantes.",
  "validation.failed": "Informe o token do cartão.",
};

export default async function CheckoutPage({ searchParams }: { searchParams: Promise<{ error?: string }> }) {
  const { error } = await searchParams;
  const { data: basket, response } = await callApi((headers) => api.GET("/api/v1/basket", { headers }));
  if (!basket) throw new Error(`The basket answered ${response.status}.`);

  return (
    <main className="narrow">
      <h1>Finalizar compra</h1>
      {error && (
        <p role="alert" className="error">
          {errorMessages[error] ?? "Não foi possível fazer o pedido. Tente de novo."}
        </p>
      )}
      {basket.lines.length === 0 ? (
        <p>
          Seu carrinho está vazio. <Link href="/">Ver produtos</Link>
        </p>
      ) : (
        <>
          <ul>
            {basket.lines.map((line) => (
              <li key={line.skuId}>
                {line.quantity} × {line.productName} —{" "}
                {formatPrice(line.currentUnitPriceAmount ?? line.unitPriceAmount)}
              </li>
            ))}
          </ul>
          <p>
            Subtotal: <strong>{formatPrice(basket.subtotalAmount)}</strong> (o frete entra no total do pedido)
          </p>
          <form action={placeOrder} className="stack">
            {/* A new key per page render: a double submit of this form places one order, a new visit a new one. */}
            <input type="hidden" name="idempotencyKey" value={crypto.randomUUID()} />
            <input type="hidden" name="expectedSubtotalAmount" value={basket.subtotalAmount} />
            <label>
              Token do cartão
              <input name="paymentToken" defaultValue="pm_card_visa" required aria-describedby="token-hint" />
            </label>
            <p id="token-hint" className="hint">
              Gateway simulado: qualquer token aprova e <code>tok_decline</code> recusa. Com o Stripe ligado, <code>pm_card_visa</code> aprova e <code>pm_card_chargeDeclined</code> recusa.
            </p>
            <button type="submit">Fazer pedido</button>
          </form>
        </>
      )}
    </main>
  );
}

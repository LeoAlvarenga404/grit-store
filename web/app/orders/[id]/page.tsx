import Link from "next/link";
import { notFound } from "next/navigation";
import { api } from "@/lib/api/client";
import { callApi } from "@/lib/api/call-api";
import { formatDateTime, formatPrice } from "@/lib/format";
import { cancelOrder } from "../actions";
import { cancellationReasonLabel, statusLabel } from "../order-labels";

const errorMessages: Record<string, string> = {
  "ordering.order_not_cancellable": "Só pedidos confirmados podem ser cancelados.",
};

export default async function OrderPage({
  params,
  searchParams,
}: {
  params: Promise<{ id: string }>;
  searchParams: Promise<{ error?: string }>;
}) {
  const { id } = await params;
  const { error } = await searchParams;
  const { data: order, response } = await callApi((headers) =>
    api.GET("/api/v1/orders/{id}", { headers, params: { path: { id } } }),
  );
  if (response.status === 404) notFound();
  if (!order) throw new Error(`The order answered ${response.status}.`);
  const isProcessing = order.status === "Placed";

  return (
    <main>
      {/* The checkout saga confirms or cancels the order in the background; reload until it settles. */}
      {isProcessing && <meta httpEquiv="refresh" content="2" />}
      <p>
        <Link href="/orders">Meus pedidos</Link>
      </p>
      <h1>Pedido {order.number}</h1>
      {error && (
        <p role="alert" className="error">
          {errorMessages[error] ?? "Não foi possível cancelar o pedido. Tente de novo."}
        </p>
      )}
      <p aria-live="polite">
        Situação: <strong>{statusLabel(order.status)}</strong>
        {order.cancellationReason && ` (${cancellationReasonLabel(order.cancellationReason)})`}
      </p>
      <table>
        <thead>
          <tr>
            <th scope="col">Produto</th>
            <th scope="col">Quantidade</th>
            <th scope="col">Preço</th>
            <th scope="col">Total</th>
          </tr>
        </thead>
        <tbody>
          {order.lines.map((line) => (
            <tr key={line.skuId}>
              <td>{line.productName}</td>
              <td>{line.quantity}</td>
              <td>{formatPrice(line.unitPriceAmount)}</td>
              <td>{formatPrice(line.lineTotalAmount)}</td>
            </tr>
          ))}
        </tbody>
      </table>
      <p>Subtotal: {formatPrice(order.subtotalAmount)}</p>
      <p>Frete: {formatPrice(order.shippingAmount)}</p>
      <p>
        Total: <strong>{formatPrice(order.totalAmount)}</strong>
      </p>
      <h2>Histórico</h2>
      <ol className="timeline">
        {order.history.map((change) => (
          <li key={`${change.status}-${change.occurredAt}`}>
            {statusLabel(change.status)} em {formatDateTime(change.occurredAt)}
          </li>
        ))}
      </ol>
      {order.status === "Confirmed" && (
        <form action={cancelOrder}>
          <input type="hidden" name="orderId" value={order.id} />
          <button type="submit" className="secondary">
            Cancelar pedido
          </button>
        </form>
      )}
    </main>
  );
}

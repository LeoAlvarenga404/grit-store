import Link from "next/link";
import { api } from "@/lib/api/client";
import { callApi } from "@/lib/api/call-api";
import { formatDateTime, formatPrice } from "@/lib/format";
import { statusLabel } from "@/app/orders/order-labels";
import { shipOrder } from "./actions";

const statuses = ["Placed", "Confirmed", "Shipped", "Cancelled"];

const errorMessages: Record<string, string> = {
  "ordering.order_not_shippable": "Só pedidos confirmados podem ser enviados.",
  "ordering.order_not_found": "O pedido não existe.",
};

export default async function AdminOrdersPage({
  searchParams,
}: {
  searchParams: Promise<{ status?: string; after?: string; error?: string }>;
}) {
  const { status, after, error } = await searchParams;
  const { data, response } = await callApi((headers) =>
    api.GET("/api/v1/admin/orders", { headers, params: { query: { status: status || undefined, after: after || undefined } } }),
  );
  if (!data) throw new Error(`The admin orders answered ${response.status}.`);
  const olderPage = data.nextCursor ? new URLSearchParams({ ...(status ? { status } : {}), after: data.nextCursor }) : null;

  return (
    <main className="legacy">
      <h1>Pedidos</h1>
      {error && (
        <p role="alert" className="error">
          {errorMessages[error] ?? "Não foi possível alterar o pedido. Tente de novo."}
        </p>
      )}
      <form className="inline-form">
        <label>
          Situação
          <select name="status" defaultValue={status ?? ""}>
            <option value="">Todas</option>
            {statuses.map((value) => (
              <option key={value} value={value}>
                {statusLabel(value)}
              </option>
            ))}
          </select>
        </label>
        <button type="submit">Filtrar</button>
      </form>
      <table>
        <thead>
          <tr>
            <th scope="col">Pedido</th>
            <th scope="col">Data</th>
            <th scope="col">Situação</th>
            <th scope="col">Total</th>
            <th scope="col">Ação</th>
          </tr>
        </thead>
        <tbody>
          {data.items.map((order) => (
            <tr key={order.id}>
              <td>{order.number}</td>
              <td>{formatDateTime(order.placedAt)}</td>
              <td>{statusLabel(order.status)}</td>
              <td>{formatPrice(order.totalAmount)}</td>
              <td>
                {order.status === "Confirmed" && (
                  <form action={shipOrder}>
                    <input type="hidden" name="orderId" value={order.id} />
                    <button type="submit">Marcar como enviado</button>
                  </form>
                )}
              </td>
            </tr>
          ))}
        </tbody>
      </table>
      {olderPage && (
        <p>
          <Link href={`/admin/orders?${olderPage}`}>Pedidos mais antigos</Link>
        </p>
      )}
    </main>
  );
}

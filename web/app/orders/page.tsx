import Link from "next/link";
import { api } from "@/lib/api/client";
import { callApi } from "@/lib/api/call-api";
import { formatDateTime, formatPrice } from "@/lib/format";
import { statusLabel } from "./order-labels";

export default async function OrdersPage() {
  const { data: orders, response } = await callApi((headers) => api.GET("/api/v1/orders", { headers }));
  if (!orders) throw new Error(`The orders answered ${response.status}.`);

  return (
    <main className="legacy">
      <h1>Meus pedidos</h1>
      {orders.length === 0 ? (
        <p>
          Você ainda não fez pedidos. <Link href="/">Ver produtos</Link>
        </p>
      ) : (
        <table>
          <thead>
            <tr>
              <th scope="col">Pedido</th>
              <th scope="col">Data</th>
              <th scope="col">Situação</th>
              <th scope="col">Total</th>
            </tr>
          </thead>
          <tbody>
            {orders.map((order) => (
              <tr key={order.id}>
                <td>
                  <Link href={`/orders/${order.id}`}>{order.number}</Link>
                </td>
                <td>{formatDateTime(order.placedAt)}</td>
                <td>{statusLabel(order.status)}</td>
                <td>{formatPrice(order.totalAmount)}</td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </main>
  );
}

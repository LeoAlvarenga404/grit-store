import type { Metadata } from "next";
import Link from "next/link";
import { ChevronRight, Package } from "lucide-react";
import { EmptyState } from "@/components/empty-state";
import { OrderStatusBadge } from "@/components/order-status-badge";
import { api } from "@/lib/api/client";
import { callApi } from "@/lib/api/call-api";
import { formatDateTime, formatPrice } from "@/lib/format";
import { orderStatusLabel } from "@/lib/order-labels";

export const metadata: Metadata = { title: "Meus pedidos" };

export default async function OrdersPage() {
  const { data: orders, response } = await callApi((headers) => api.GET("/api/v1/orders", { headers }));
  if (!orders) throw new Error(`The orders answered ${response.status}.`);

  if (orders.length === 0) {
    return (
      <main className="mx-auto max-w-3xl px-4 lg:px-8">
        <h1 className="sr-only">Meus pedidos</h1>
        <EmptyState
          icon={Package}
          title="Você ainda não fez pedidos"
          text="Quando você comprar, seus pedidos aparecem aqui."
          actionLabel="Ver lançamentos"
          actionHref="/catalog"
        />
      </main>
    );
  }

  return (
    <main className="mx-auto max-w-3xl px-4 py-10 lg:px-8">
      <h1 className="text-display text-3xl md:text-4xl">Meus pedidos</h1>
      <ul className="mt-8 divide-y border-y">
        {orders.map((order) => (
          <li key={order.id}>
            <Link
              href={`/orders/${order.id}`}
              className="flex items-center gap-4 py-5 hover:bg-muted/50"
              aria-label={`Pedido ${order.number}, ${orderStatusLabel(order.status)}, ${formatPrice(order.totalAmount)}`}
            >
              <div className="min-w-0 flex-1">
                <p className="font-semibold tabular-nums">{order.number}</p>
                <p className="text-sm text-muted-foreground">{formatDateTime(order.placedAt)}</p>
              </div>
              <OrderStatusBadge status={order.status} />
              <p className="w-28 text-right font-semibold tabular-nums">{formatPrice(order.totalAmount)}</p>
              <ChevronRight aria-hidden="true" className="size-4 text-muted-foreground" />
            </Link>
          </li>
        ))}
      </ul>
    </main>
  );
}

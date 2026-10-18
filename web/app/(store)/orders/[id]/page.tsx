import type { Metadata } from "next";
import Link from "next/link";
import { notFound } from "next/navigation";
import { ArrowLeft } from "lucide-react";
import { ErrorMessage } from "@/components/error-message";
import { OrderStatusBadge } from "@/components/order-status-badge";
import { PriceSummary } from "@/components/price-summary";
import { Button } from "@/components/ui/button";
import { api } from "@/lib/api/client";
import { callApi } from "@/lib/api/call-api";
import { formatDateTime } from "@/lib/format";
import { CancelOrderDialog } from "./cancel-order-dialog";
import { OrderLineItem } from "./order-line-item";
import { OrderStatusPanel } from "./order-status-panel";
import { OrderTimeline } from "./order-timeline";

export const metadata: Metadata = { title: "Pedido" };

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
    <main className="mx-auto max-w-3xl px-4 py-10 lg:px-8">
      {/* The checkout saga confirms or cancels the order in the background; reload until it settles. */}
      {isProcessing && <meta httpEquiv="refresh" content="2" />}
      <Link href="/orders" className="inline-flex items-center gap-1 text-sm hover:underline">
        <ArrowLeft aria-hidden="true" className="size-4" /> Meus pedidos
      </Link>
      <div className="mt-4 flex flex-wrap items-end justify-between gap-3">
        <div>
          <h1 className="text-display text-3xl">Pedido {order.number}</h1>
          <p className="mt-1 text-sm text-muted-foreground">
            Feito em <time dateTime={order.placedAt}>{formatDateTime(order.placedAt)}</time>
          </p>
        </div>
        <OrderStatusBadge status={order.status} />
      </div>
      {error && <ErrorMessage className="mt-6">{errorMessages[error] ?? "Não foi possível cancelar o pedido. Tente de novo."}</ErrorMessage>}
      <div className="mt-6">
        <OrderStatusPanel status={order.status} cancellationReason={order.cancellationReason} />
      </div>
      <section aria-labelledby="items-title" className="mt-10">
        <h2 id="items-title" className="text-display text-lg">
          Itens
        </h2>
        <ul className="mt-2 divide-y border-y">
          {order.lines.map((line) => (
            <OrderLineItem key={line.skuId} line={line} />
          ))}
        </ul>
        <PriceSummary className="mt-6 ml-auto max-w-sm" subtotalAmount={order.subtotalAmount} shippingAmount={order.shippingAmount} />
      </section>
      <div className="mt-10 grid gap-10 sm:grid-cols-[1fr_auto] sm:items-start">
        <OrderTimeline history={order.history} />
        {order.status === "Confirmed" && <CancelOrderDialog orderId={order.id} orderNumber={order.number} />}
      </div>
      {order.status !== "Placed" && (
        <Button asChild variant="link" className="mt-10 px-0">
          <Link href="/catalog">Continuar comprando</Link>
        </Button>
      )}
    </main>
  );
}

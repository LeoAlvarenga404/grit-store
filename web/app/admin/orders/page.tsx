import type { Metadata } from "next";
import Link from "next/link";
import { ErrorMessage } from "@/components/error-message";
import { OrderStatusBadge } from "@/components/order-status-badge";
import { SubmitButton } from "@/components/submit-button";
import { Button } from "@/components/ui/button";
import { Label } from "@/components/ui/label";
import { NativeSelect, NativeSelectOption } from "@/components/ui/native-select";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { api } from "@/lib/api/client";
import { callApi } from "@/lib/api/call-api";
import { formatDateTime, formatPrice } from "@/lib/format";
import { orderStatusLabel } from "@/lib/order-labels";
import { shipOrder } from "./actions";

export const metadata: Metadata = { title: "Pedidos" };

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
    <>
      <div className="flex flex-wrap items-end justify-between gap-4">
        <h1 className="text-2xl font-semibold tracking-tight">Pedidos</h1>
        <form className="flex items-end gap-2">
          <div className="space-y-1.5">
            <Label htmlFor="status">Situação</Label>
            <NativeSelect id="status" name="status" defaultValue={status ?? ""} className="w-44">
              <NativeSelectOption value="">Todas</NativeSelectOption>
              {statuses.map((value) => (
                <NativeSelectOption key={value} value={value}>{orderStatusLabel(value)}</NativeSelectOption>
              ))}
            </NativeSelect>
          </div>
          <Button type="submit" variant="outline" size="sm" className="h-9">Filtrar</Button>
        </form>
      </div>
      {error && <ErrorMessage className="mt-6">{errorMessages[error] ?? "Não foi possível alterar o pedido. Tente de novo."}</ErrorMessage>}
      <div className="mt-6 rounded-md border bg-background">
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead>Pedido</TableHead>
              <TableHead>Data</TableHead>
              <TableHead>Situação</TableHead>
              <TableHead className="text-right">Total</TableHead>
              <TableHead className="text-right">Ação</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {data.items.map((order) => (
              <TableRow key={order.id}>
                <TableCell className="font-medium tabular-nums">{order.number}</TableCell>
                <TableCell>{formatDateTime(order.placedAt)}</TableCell>
                <TableCell><OrderStatusBadge status={order.status} /></TableCell>
                <TableCell className="text-right tabular-nums">{formatPrice(order.totalAmount)}</TableCell>
                <TableCell className="text-right">
                  {order.status === "Confirmed" && (
                    <form action={shipOrder}>
                      <input type="hidden" name="orderId" value={order.id} />
                      <SubmitButton size="sm" variant="outline" pendingLabel="Enviando…">Marcar como enviado</SubmitButton>
                    </form>
                  )}
                </TableCell>
              </TableRow>
            ))}
            {data.items.length === 0 && (
              <TableRow>
                <TableCell colSpan={5} className="py-10 text-center text-muted-foreground">Nenhum pedido nesta situação.</TableCell>
              </TableRow>
            )}
          </TableBody>
        </Table>
      </div>
      {olderPage && (
        <div className="mt-4 flex justify-end">
          <Button asChild variant="outline" size="sm"><Link href={`/admin/orders?${olderPage}`}>Pedidos mais antigos</Link></Button>
        </div>
      )}
    </>
  );
}

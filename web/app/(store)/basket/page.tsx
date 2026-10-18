import type { Metadata } from "next";
import Link from "next/link";
import { ShoppingBag } from "lucide-react";
import { Button } from "@/components/ui/button";
import { EmptyState } from "@/components/empty-state";
import { ErrorMessage } from "@/components/error-message";
import { PriceSummary } from "@/components/price-summary";
import { api } from "@/lib/api/client";
import { callApi } from "@/lib/api/call-api";
import { flatShippingAmount } from "@/lib/store-navigation";
import { BasketLine } from "./basket-line";

export const metadata: Metadata = { title: "Sacola" };

const errorMessages: Record<string, string> = {
  "basket.quantity_limit": "No máximo 10 unidades por item.",
  "basket.line_not_found": "Este item não está mais na sacola.",
  "validation.failed": "Use uma quantidade de 1 a 10.",
};

export default async function BasketPage({ searchParams }: { searchParams: Promise<{ error?: string }> }) {
  const { error } = await searchParams;
  const { data: basket, response } = await callApi((headers) => api.GET("/api/v1/basket", { headers }));
  if (!basket) throw new Error(`The basket answered ${response.status}.`);

  if (basket.lines.length === 0) {
    return (
      <main className="mx-auto max-w-screen-2xl px-4 lg:px-8">
        <h1 className="sr-only">Sacola</h1>
        <EmptyState
          icon={ShoppingBag}
          title="Sua sacola está vazia"
          text="Explore os lançamentos e encontre seu próximo treino."
          actionLabel="Ver lançamentos"
          actionHref="/catalog"
        />
      </main>
    );
  }

  const itemCount = basket.lines.reduce((total, line) => total + line.quantity, 0);
  const hasUnavailableLine = basket.lines.some((line) => !line.isAvailable);

  return (
    <main className="mx-auto max-w-screen-2xl px-4 py-10 lg:px-8">
      <h1 className="text-display text-3xl md:text-4xl">Sacola</h1>
      <p className="mt-2 text-sm text-muted-foreground">
        {itemCount} {itemCount === 1 ? "item" : "itens"}
      </p>
      {error && <ErrorMessage className="mt-6">{errorMessages[error] ?? "Não foi possível alterar a sacola. Tente de novo."}</ErrorMessage>}
      <div className="mt-8 grid gap-10 lg:grid-cols-[minmax(0,1fr)_24rem]">
        <ul className="divide-y border-y">
          {basket.lines.map((line) => (
            <BasketLine key={line.skuId} line={line} />
          ))}
        </ul>
        <aside aria-labelledby="summary-title" className="h-fit bg-muted/60 p-6 lg:sticky lg:top-24">
          <h2 id="summary-title" className="text-display text-xl">
            Resumo
          </h2>
          <PriceSummary className="mt-6" subtotalAmount={basket.subtotalAmount} shippingAmount={flatShippingAmount} />
          {hasUnavailableLine ? (
            <>
              <Button size="lg" className="mt-6 w-full" disabled>
                Finalizar compra
              </Button>
              <p className="mt-3 text-sm text-destructive">Remova os itens indisponíveis para continuar.</p>
            </>
          ) : (
            <Button asChild size="lg" className="mt-6 w-full">
              <Link href="/checkout">Finalizar compra</Link>
            </Button>
          )}
          <Link href="/catalog" className="mt-4 block text-center text-sm underline-offset-4 hover:underline">
            Continuar comprando
          </Link>
        </aside>
      </div>
    </main>
  );
}

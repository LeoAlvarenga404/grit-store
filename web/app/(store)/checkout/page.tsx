import type { Metadata } from "next";
import Image from "next/image";
import Link from "next/link";
import { ArrowLeft, ImageOff, Lock, ShoppingBag } from "lucide-react";
import { EmptyState } from "@/components/empty-state";
import { ErrorMessage } from "@/components/error-message";
import { PriceSummary } from "@/components/price-summary";
import { SubmitButton } from "@/components/submit-button";
import { api } from "@/lib/api/client";
import { callApi } from "@/lib/api/call-api";
import type { components } from "@/lib/api/schema";
import { formatPrice } from "@/lib/format";
import { variantLabel } from "@/lib/product-variants";
import { flatShippingAmount } from "@/lib/store-navigation";
import { placeOrder } from "./actions";
import { TestCardOptions } from "./test-card-options";

export const metadata: Metadata = { title: "Finalizar compra" };

const errorMessages: Record<string, string> = {
  "ordering.basket_empty": "Sua sacola está vazia.",
  "ordering.sku_unavailable": "Um item da sacola ficou indisponível. Revise a sacola.",
  "ordering.price_changed": "Um preço mudou desde que você abriu esta página. Confira o novo subtotal.",
  "ordering.checkout_in_progress": "Este pedido ainda está sendo criado. Veja seus pedidos em instantes.",
  "validation.failed": "Escolha um cartão de teste.",
};

export default async function CheckoutPage({ searchParams }: { searchParams: Promise<{ error?: string }> }) {
  const { error } = await searchParams;
  const { data: basket, response } = await callApi((headers) => api.GET("/api/v1/basket", { headers }));
  if (!basket) throw new Error(`The basket answered ${response.status}.`);
  const errorText = error && (errorMessages[error] ?? "Não foi possível fazer o pedido. Tente de novo.");

  // A stale checkout page can come back with ordering.basket_empty, so the empty state still shows the error.
  if (basket.lines.length === 0) {
    return (
      <main className="mx-auto max-w-screen-2xl px-4 lg:px-8">
        <h1 className="sr-only">Finalizar compra</h1>
        {errorText && <ErrorMessage className="mx-auto mt-10 max-w-md">{errorText}</ErrorMessage>}
        <EmptyState
          icon={ShoppingBag}
          title="Sua sacola está vazia"
          text="Adicione produtos para finalizar a compra."
          actionLabel="Ver lançamentos"
          actionHref="/catalog"
        />
      </main>
    );
  }

  const totalAmount = basket.subtotalAmount + flatShippingAmount;
  const hasUnavailableLine = basket.lines.some((line) => !line.isAvailable);

  return (
    <main className="mx-auto max-w-screen-2xl px-4 py-10 lg:px-8">
      <Link href="/basket" className="inline-flex items-center gap-1 text-sm hover:underline">
        <ArrowLeft aria-hidden="true" className="size-4" /> Voltar para a sacola
      </Link>
      <h1 className="text-display mt-4 text-3xl md:text-4xl">Finalizar compra</h1>
      {errorText && <ErrorMessage className="mt-6">{errorText}</ErrorMessage>}
      <div className="mt-8 grid gap-10 lg:grid-cols-[minmax(0,1fr)_26rem]">
        <form action={placeOrder} className="space-y-8">
          {/* A new key per page render: a double submit of this form places one order, a new visit a new one. */}
          <input type="hidden" name="idempotencyKey" value={crypto.randomUUID()} />
          <input type="hidden" name="expectedSubtotalAmount" value={basket.subtotalAmount} />
          <TestCardOptions />
          {hasUnavailableLine ? (
            <ErrorMessage>
              Um item da sacola ficou indisponível. <Link href="/basket" className="underline">Revise a sacola</Link> para continuar.
            </ErrorMessage>
          ) : (
            <SubmitButton size="lg" className="w-full" pendingLabel="Processando pagamento…">
              <Lock aria-hidden="true" /> Pagar {formatPrice(totalAmount)}
            </SubmitButton>
          )}
          <p className="text-xs text-muted-foreground">Ao pagar, o pedido é criado e o pagamento é processado. Você pode cancelar até o pedido ser enviado.</p>
        </form>
        <aside aria-labelledby="summary-title" className="h-fit bg-muted/60 p-6 lg:sticky lg:top-24">
          <h2 id="summary-title" className="text-display text-xl">Resumo do pedido</h2>
          <ul className="mt-6 divide-y">
            {basket.lines.map((line) => (
              <SummaryLine key={line.skuId} line={line} />
            ))}
          </ul>
          <PriceSummary className="mt-6 border-t pt-6" subtotalAmount={basket.subtotalAmount} shippingAmount={flatShippingAmount} />
        </aside>
      </div>
    </main>
  );
}

function SummaryLine({ line }: { line: components["schemas"]["BasketLineResponse"] }) {
  const unitPrice = line.currentUnitPriceAmount ?? line.unitPriceAmount;
  const variant = variantLabel(line.color, line.size);

  return (
    <li className="flex gap-3 py-3">
      <div className="relative aspect-[4/5] w-14 shrink-0 bg-background">
        {line.imageUrl ? (
          <Image src={line.imageUrl} alt="" fill sizes="56px" className="object-contain p-1 mix-blend-multiply" unoptimized />
        ) : (
          <ImageOff aria-hidden="true" className="absolute inset-0 m-auto size-4 text-muted-foreground" />
        )}
      </div>
      <div className="min-w-0 flex-1 text-sm">
        <p className="line-clamp-2 font-medium">{line.productName}</p>
        {variant && <p className="text-muted-foreground">{variant}</p>}
        <p className="tabular-nums text-muted-foreground">{`${line.quantity} × ${formatPrice(unitPrice)}`}</p>
      </div>
      <p className="text-sm font-semibold tabular-nums">{formatPrice(unitPrice * line.quantity)}</p>
    </li>
  );
}

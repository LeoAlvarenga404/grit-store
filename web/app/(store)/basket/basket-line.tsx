import { ImageOff, Minus, Plus, Trash2 } from "lucide-react";
import Image from "next/image";
import Link from "next/link";
import type { components } from "@/lib/api/schema";
import { cn } from "@/lib/cn";
import { formatPrice } from "@/lib/format";
import { productHref, variantLabel } from "@/lib/product-variants";
import { changeQuantity, removeLine } from "./actions";

type Line = components["schemas"]["BasketLineResponse"];

export function BasketLine({ line }: { line: Line }) {
  const unitPrice = line.currentUnitPriceAmount ?? line.unitPriceAmount;
  const href = productHref(line.productSlug, line.color);
  const variant = variantLabel(line.color, line.size);

  return (
    <li className={cn("flex gap-4 py-6", !line.isAvailable && "opacity-70")}>
      <Link href={href} className="relative aspect-[4/5] w-24 shrink-0 overflow-hidden bg-muted sm:w-28" tabIndex={-1} aria-hidden="true">
        {line.imageUrl ? (
          <Image src={line.imageUrl} alt="" fill sizes="112px" className="object-contain p-2 mix-blend-multiply" unoptimized />
        ) : (
          <span className="absolute inset-0 flex items-center justify-center">
            <ImageOff aria-hidden="true" className="size-5 text-muted-foreground" />
          </span>
        )}
      </Link>
      <div className="flex min-w-0 flex-1 flex-col">
        <div className="flex items-start justify-between gap-4">
          <div className="min-w-0">
            <Link href={href} className="font-medium hover:underline">
              {line.productName}
            </Link>
            {variant && <p className="mt-1 text-sm text-muted-foreground">{variant}</p>}
            <p className="mt-1 text-sm text-muted-foreground tabular-nums">{formatPrice(unitPrice)} cada</p>
          </div>
          <p className="font-semibold tabular-nums">{formatPrice(unitPrice * line.quantity)}</p>
        </div>
        {line.hasPriceChanged && line.isAvailable && (
          <p className="mt-2 text-sm">O preço mudou desde que você adicionou: era {formatPrice(line.unitPriceAmount)}.</p>
        )}
        {!line.isAvailable && <p className="mt-2 text-sm font-medium text-destructive">Indisponível. Remova este item para finalizar a compra.</p>}
        <div className="mt-auto flex items-center justify-between gap-4 pt-4">
          {line.isAvailable ? <QuantityStepper line={line} /> : <span />}
          <form action={removeLine}>
            <input type="hidden" name="skuId" value={line.skuId} />
            <button
              type="submit"
              aria-label={`Remover ${line.productName}`}
              className="inline-flex min-h-10 items-center gap-1.5 text-sm underline-offset-4 hover:underline"
            >
              <Trash2 aria-hidden="true" className="size-4" /> Remover
            </button>
          </form>
        </div>
      </div>
    </li>
  );
}

function QuantityStepper({ line }: { line: Line }) {
  return (
    <div className="flex h-10 items-center rounded-sm border">
      <form action={changeQuantity}>
        <input type="hidden" name="skuId" value={line.skuId} />
        <input type="hidden" name="quantity" value={line.quantity - 1} />
        <button
          type="submit"
          aria-label={`Diminuir quantidade de ${line.productName}`}
          disabled={line.quantity <= 1}
          className="flex size-10 items-center justify-center disabled:opacity-30"
        >
          <Minus aria-hidden="true" className="size-4" />
        </button>
      </form>
      <span className="w-8 text-center text-sm tabular-nums" aria-label={`Quantidade: ${line.quantity}`}>
        {line.quantity}
      </span>
      <form action={changeQuantity}>
        <input type="hidden" name="skuId" value={line.skuId} />
        <input type="hidden" name="quantity" value={line.quantity + 1} />
        <button
          type="submit"
          aria-label={`Aumentar quantidade de ${line.productName}`}
          disabled={line.quantity >= 10}
          className="flex size-10 items-center justify-center disabled:opacity-30"
        >
          <Plus aria-hidden="true" className="size-4" />
        </button>
      </form>
    </div>
  );
}

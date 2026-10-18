import { ImageOff } from "lucide-react";
import Image from "next/image";
import Link from "next/link";
import type { components } from "@/lib/api/schema";
import { formatPrice } from "@/lib/format";

export function OrderLineItem({ line }: { line: components["schemas"]["OrderLineResponse"] }) {
  const variant = [line.color && `Cor: ${line.color}`, line.size && `Tamanho: ${line.size}`].filter(Boolean).join(" · ");
  const productHref = line.productSlug ? (line.color ? `/products/${line.productSlug}?cor=${encodeURIComponent(line.color)}` : `/products/${line.productSlug}`) : null;

  return (
    <li className="flex gap-4 py-4">
      <div className="relative aspect-[4/5] w-20 shrink-0 bg-muted">
        {line.imageUrl ? (
          <Image src={line.imageUrl} alt="" fill sizes="80px" className="object-contain p-2 mix-blend-multiply" unoptimized />
        ) : (
          <ImageOff aria-hidden="true" className="absolute inset-0 m-auto size-5 text-muted-foreground" />
        )}
      </div>
      <div className="flex min-w-0 flex-1 justify-between gap-4">
        <div className="min-w-0">
          {productHref ? (
            <Link href={productHref} className="font-medium hover:underline">
              {line.productName}
            </Link>
          ) : (
            <span className="font-medium">{line.productName}</span>
          )}
          {variant && <p className="text-sm text-muted-foreground first-letter:uppercase">{variant}</p>}
          <p className="text-sm text-muted-foreground tabular-nums">{`${line.quantity} × ${formatPrice(line.unitPriceAmount)}`}</p>
        </div>
        <p className="font-semibold tabular-nums">{formatPrice(line.lineTotalAmount)}</p>
      </div>
    </li>
  );
}

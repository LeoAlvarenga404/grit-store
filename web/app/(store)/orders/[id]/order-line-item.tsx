import { ImageOff } from "lucide-react";
import Image from "next/image";
import Link from "next/link";
import type { components } from "@/lib/api/schema";
import { formatPrice } from "@/lib/format";
import { productHref, variantLabel } from "@/lib/product-variants";

export function OrderLineItem({ line }: { line: components["schemas"]["OrderLineResponse"] }) {
  const variant = variantLabel(line.color, line.size);
  // Lines placed before the order kept the slug have none, so they show the name without a link.
  const href = line.productSlug ? productHref(line.productSlug, line.color) : null;

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
          {href ? (
            <Link href={href} className="font-medium hover:underline">
              {line.productName}
            </Link>
          ) : (
            <span className="font-medium">{line.productName}</span>
          )}
          {variant && <p className="text-sm text-muted-foreground">{variant}</p>}
          <p className="text-sm text-muted-foreground tabular-nums">{`${line.quantity} × ${formatPrice(line.unitPriceAmount)}`}</p>
        </div>
        <p className="font-semibold tabular-nums">{formatPrice(line.lineTotalAmount)}</p>
      </div>
    </li>
  );
}

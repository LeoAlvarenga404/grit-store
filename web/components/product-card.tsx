import Image from "next/image";
import Link from "next/link";
import { cn } from "@/lib/cn";
import { formatPrice } from "@/lib/format";

type Product = { slug: string; name: string; priceAmount: number; imageUrl: null | string };

// Seller photos have white backgrounds of uneven size: contain them on a light grey 4:5 tile so the grid stays even.
export function ProductCard({ product, className }: { product: Product; className?: string }) {
  return (
    <Link href={`/products/${product.slug}`} className={cn("group block", className)}>
      <div className="relative aspect-[4/5] overflow-hidden bg-muted">
        {product.imageUrl ? (
          <Image
            src={product.imageUrl}
            alt=""
            fill
            sizes="(min-width: 1280px) 16vw, (min-width: 768px) 33vw, 50vw"
            className="object-contain p-6 mix-blend-multiply transition-transform duration-300 group-hover:scale-105"
            unoptimized
          />
        ) : (
          <span className="absolute inset-0 flex items-center justify-center text-xs text-muted-foreground">Sem foto</span>
        )}
      </div>
      <div className="mt-3 space-y-1">
        <h3 className="line-clamp-2 text-sm leading-snug group-hover:underline">{product.name}</h3>
        <p className="text-sm font-semibold tabular-nums">{formatPrice(product.priceAmount)}</p>
      </div>
    </Link>
  );
}

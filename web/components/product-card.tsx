"use client";

import { ArrowRight } from "lucide-react";
import Image from "next/image";
import Link from "next/link";
import { useState } from "react";
import { ColorSwatch } from "@/components/color-swatch";
import { FavoriteButton } from "@/components/favorite-button";
import type { components } from "@/lib/api/schema";
import { cn } from "@/lib/cn";
import { formatPrice } from "@/lib/format";

type Product = Pick<components["schemas"]["ProductSummaryResponse"], "slug" | "name" | "priceAmount" | "hasPriceRange" | "imageUrl" | "colors">;

const maxSwatches = 5;

// Seller photos have white backgrounds of uneven size: contain them on a light grey 4:5 tile so the grid stays even.
export function ProductCard({ product, className }: { product: Product; className?: string }) {
  const colors = product.colors;
  const [activeImage, setActiveImage] = useState(product.imageUrl);
  const href = `/products/${product.slug}`;

  return (
    <div className={cn("group relative", className)}>
      <FavoriteButton slug={product.slug} name={product.name} className="absolute top-2 right-2 z-10" />
      <Link href={href} className="relative block aspect-[4/5] overflow-hidden bg-muted" tabIndex={-1} aria-hidden="true">
        {activeImage ? (
          <Image
            src={activeImage}
            alt={product.name}
            fill
            sizes="(min-width: 1280px) 16vw, (min-width: 768px) 33vw, 50vw"
            className="object-contain p-6 mix-blend-multiply transition-transform duration-500 ease-out group-hover:scale-105"
            unoptimized
          />
        ) : (
          <span className="absolute inset-0 flex items-center justify-center text-xs text-muted-foreground">Sem foto</span>
        )}
        <span className="absolute inset-x-3 bottom-3 hidden translate-y-3 items-center justify-center gap-1.5 bg-background py-2.5 text-xs font-semibold tracking-[0.08em] uppercase opacity-0 shadow-sm transition-all duration-300 group-focus-within:translate-y-0 group-focus-within:opacity-100 group-hover:translate-y-0 group-hover:opacity-100 md:flex">
          Ver produto <ArrowRight aria-hidden="true" className="size-3.5" />
        </span>
      </Link>
      {colors.length > 1 && (
        <ul className="mt-3 flex items-center gap-1.5" aria-label={`${colors.length} cores`}>
          {colors.slice(0, maxSwatches).map((color) => (
            <li key={color.name}>
              <button
                type="button"
                title={color.name}
                aria-label={`Ver na cor ${color.name}`}
                onMouseEnter={() => color.imageUrl && setActiveImage(color.imageUrl)}
                onFocus={() => color.imageUrl && setActiveImage(color.imageUrl)}
                className={cn("flex size-6 items-center justify-center rounded-full ring-offset-2 transition-shadow hover:ring-1 hover:ring-foreground", color.imageUrl === activeImage && "ring-1 ring-foreground")}
              >
                <ColorSwatch name={color.name} imageUrl={color.imageUrl} />
              </button>
            </li>
          ))}
          {colors.length > maxSwatches && <li className="text-xs text-muted-foreground">+{colors.length - maxSwatches}</li>}
        </ul>
      )}
      <Link href={href} className="mt-2 block space-y-1">
        <h3 className="line-clamp-2 text-sm leading-snug underline-offset-4 group-hover:underline">{product.name}</h3>
        <p className="text-sm font-semibold tabular-nums">
          {product.hasPriceRange && <span className="font-normal text-muted-foreground">a partir de </span>}
          {formatPrice(product.priceAmount)}
        </p>
      </Link>
    </div>
  );
}

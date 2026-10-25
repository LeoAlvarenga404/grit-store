"use client";

import { useEffect } from "react";
import { SectionHeading } from "@/components/home/section-heading";
import { ProductCard } from "@/components/product-card";
import { cn } from "@/lib/cn";
import { recentlyViewed } from "@/lib/shopper-lists";
import { useSlugs } from "@/lib/slug-store";
import { useProductCards } from "@/lib/use-product-cards";

// On a product page the current product is recorded and left out; on the home it shows everything seen so far.
export function RecentlyViewed({ currentSlug, count, className }: { currentSlug?: string; count: number; className?: string }) {
  const slugs = useSlugs(recentlyViewed);
  const { products } = useProductCards(slugs.filter((slug) => slug !== currentSlug).slice(0, count));

  useEffect(() => {
    if (currentSlug) recentlyViewed.add(currentSlug);
  }, [currentSlug]);

  if (products.length === 0) return null;

  return (
    <section aria-labelledby="recent-title" className={cn("mx-auto max-w-screen-2xl px-4 lg:px-8", className)}>
      <SectionHeading id="recent-title" eyebrow="Continue de onde parou" title="Vistos recentemente" />
      <ul className={cn("grid grid-cols-2 gap-x-3 gap-y-8 lg:grid-cols-4", count > 4 && "lg:grid-cols-6")}>
        {products.map((product) => (
          <li key={product.id}>
            <ProductCard product={product} />
          </li>
        ))}
      </ul>
    </section>
  );
}

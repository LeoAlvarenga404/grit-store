import type { components } from "@/lib/api/schema";
import { SectionHeading } from "@/components/home/section-heading";
import { ProductCard } from "@/components/product-card";
import { ScrollRow } from "@/components/scroll-row";

type Product = components["schemas"]["ProductSummaryResponse"];

export function ProductGridSection({ id, title, eyebrow, href, products }: { id: string; title: string; eyebrow: string; href: string; products: Product[] }) {
  if (products.length === 0) return null;

  return (
    <section aria-labelledby={id} className="mx-auto max-w-screen-2xl px-4 py-16 lg:px-8 lg:py-24">
      <SectionHeading id={id} eyebrow={eyebrow} title={title} href={href} />
      <ul className="grid grid-cols-2 gap-x-3 gap-y-8 md:grid-cols-3 xl:grid-cols-6">
        {products.map((product) => (
          <li key={product.id} className="reveal">
            <ProductCard product={product} />
          </li>
        ))}
      </ul>
    </section>
  );
}

export function ProductRowSection({ id, title, eyebrow, href, products }: { id: string; title: string; eyebrow: string; href: string; products: Product[] }) {
  if (products.length === 0) return null;

  return (
    <section aria-labelledby={id} className="mx-auto max-w-screen-2xl py-16 lg:px-8 lg:py-24">
      <div className="px-4 lg:px-0">
        <SectionHeading id={id} eyebrow={eyebrow} title={title} href={href} />
      </div>
      <ScrollRow label={title}>
        {products.map((product) => (
          <li key={product.id} className="w-[44vw] shrink-0 snap-start sm:w-[30vw] lg:w-[calc((100%-4*0.75rem)/5)]">
            <ProductCard product={product} />
          </li>
        ))}
      </ScrollRow>
    </section>
  );
}

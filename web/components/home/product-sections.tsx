import { SectionHeading } from "@/components/home/section-heading";
import { ProductCard } from "@/components/product-card";

type Product = { id: string; slug: string; name: string; priceAmount: number; hasPriceRange: boolean; imageUrl: null | string; colors: { name: string; imageUrl: null | string }[] };

export function ProductGridSection({ id, title, href, products }: { id: string; title: string; href: string; products: Product[] }) {
  if (products.length === 0) return null;

  return (
    <section aria-labelledby={id} className="mx-auto max-w-screen-2xl px-4 py-14 lg:px-8">
      <SectionHeading id={id} title={title} href={href} />
      <ul className="grid grid-cols-2 gap-x-3 gap-y-8 md:grid-cols-3 xl:grid-cols-6">
        {products.map((product) => (
          <li key={product.id}>
            <ProductCard product={product} />
          </li>
        ))}
      </ul>
    </section>
  );
}

export function ProductRowSection({ id, title, href, products }: { id: string; title: string; href: string; products: Product[] }) {
  if (products.length === 0) return null;

  return (
    <section aria-labelledby={id} className="mx-auto max-w-screen-2xl py-14 lg:px-8">
      <div className="px-4 lg:px-0">
        <SectionHeading id={id} title={title} href={href} />
      </div>
      <ul className="flex snap-x snap-mandatory gap-3 overflow-x-auto px-4 pb-2 lg:px-0 [scrollbar-width:thin]">
        {products.map((product) => (
          <li key={product.id} className="w-[44vw] shrink-0 snap-start sm:w-[30vw] lg:w-[calc((100%-4*0.75rem)/5)]">
            <ProductCard product={product} />
          </li>
        ))}
      </ul>
    </section>
  );
}

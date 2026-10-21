import { SectionHeading } from "@/components/home/section-heading";
import { ProductCard } from "@/components/product-card";
import { getProducts } from "@/lib/api/catalog-reads";

const relatedCount = 4;

export async function RelatedProducts({ categorySlug, productId }: { categorySlug: string; productId: string }) {
  const { data } = await getProducts({ category: categorySlug, limit: relatedCount + 1 });
  const products = (data?.items ?? []).filter((item) => item.id !== productId).slice(0, relatedCount);
  if (products.length === 0) return null;

  return (
    <section aria-labelledby="related-title" className="mt-24 border-t pt-16">
      <SectionHeading id="related-title" eyebrow="Mais da categoria" title="Você também pode gostar" href={`/catalog?category=${encodeURIComponent(categorySlug)}`} />
      <ul className="grid grid-cols-2 gap-x-3 gap-y-8 lg:grid-cols-4">
        {products.map((product) => (
          <li key={product.id} className="reveal">
            <ProductCard product={product} />
          </li>
        ))}
      </ul>
    </section>
  );
}

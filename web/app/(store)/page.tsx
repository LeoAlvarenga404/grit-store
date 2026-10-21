import { CategoryGrid } from "@/components/home/category-grid";
import { BrandMarquee } from "@/components/home/brand-marquee";
import { Hero } from "@/components/home/hero";
import { ProductGridSection, ProductRowSection } from "@/components/home/product-sections";
import { PromoBanners } from "@/components/home/promo-banners";
import { ValueStrip } from "@/components/home/value-strip";
import { api } from "@/lib/api/client";

// Rendered per request: the build must not need a running API.
export const dynamic = "force-dynamic";

export default async function HomePage() {
  const [latest, sneakers] = await Promise.all([
    api.GET("/api/v1/catalog/products", { params: { query: { limit: 12 } } }),
    api.GET("/api/v1/catalog/products", { params: { query: { category: "tenis", limit: 10 } } }),
  ]);

  return (
    <main>
      <Hero />
      <ValueStrip />
      <CategoryGrid />
      <ProductGridSection id="latest-title" eyebrow="Novidades" title="Lançamentos" href="/catalog" products={latest.data?.items ?? []} />
      <BrandMarquee />
      <PromoBanners />
      <ProductRowSection id="sneakers-title" eyebrow="Corrida e treino" title="Tênis" href="/catalog?category=tenis" products={sneakers.data?.items ?? []} />
    </main>
  );
}

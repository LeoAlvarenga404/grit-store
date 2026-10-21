import type { Metadata } from "next";
import Link from "next/link";
import { notFound } from "next/navigation";
import { cache } from "react";
import { storeValues } from "@/components/home/value-strip";
import { api } from "@/lib/api/client";
import { galleryOf, selectColor, skusOfColor } from "@/lib/product-variants";
import { ColorPicker } from "./color-picker";
import { ProductGallery } from "./product-gallery";
import { ProductAccordion } from "./product-accordion";
import { PurchasePanel } from "./purchase-panel";
import { RelatedProducts } from "./related-products";

type PageProps = { params: Promise<{ slug: string }>; searchParams: Promise<{ cor?: string | string[] }> };

// generateMetadata and the page read the same product in one request.
const getProduct = cache((slug: string) => api.GET("/api/v1/catalog/products/{slug}", { params: { path: { slug } } }));

export async function generateMetadata({ params }: PageProps): Promise<Metadata> {
  const { data } = await getProduct((await params).slug);
  return { title: data?.name ?? "Produto" };
}

export default async function ProductPage({ params, searchParams }: PageProps) {
  const { slug } = await params;
  const { cor } = await searchParams;
  const { data: product, response } = await getProduct(slug);
  if (response.status === 404) notFound();
  if (!product) throw new Error(`The catalog answered ${response.status} for the product ${slug}.`);
  const color = selectColor(product.colors, Array.isArray(cor) ? cor[0] : cor);
  const colorKey = color?.name ?? "";

  return (
    <main className="mx-auto max-w-screen-2xl px-4 py-6 lg:px-8">
      <nav aria-label="Trilha" className="text-sm text-muted-foreground">
        <ol className="flex flex-wrap items-center gap-2">
          <li>
            <Link href="/" className="hover:text-foreground hover:underline">
              Início
            </Link>
          </li>
          <li aria-hidden="true">/</li>
          <li>
            <Link href={`/catalog?category=${encodeURIComponent(product.categorySlug)}`} className="hover:text-foreground hover:underline">
              {product.categoryName}
            </Link>
          </li>
          <li aria-hidden="true">/</li>
          <li aria-current="page" className="text-foreground">
            {product.name}
          </li>
        </ol>
      </nav>
      <div className="mt-6 grid grid-cols-[minmax(0,1fr)] items-start gap-8 lg:grid-cols-[minmax(0,7fr)_minmax(0,5fr)] lg:gap-14">
        <div className="lg:sticky lg:top-24">
          <ProductGallery key={colorKey} images={galleryOf(product.imageUrls, color)} productName={product.name} />
        </div>
        <div>
          <p className="text-eyebrow text-muted-foreground">{product.attributes["marca"] ?? product.categoryName}</p>
          <h1 className="text-display mt-2 text-2xl break-words sm:text-3xl md:text-4xl">{product.name}</h1>
          <PurchasePanel
            key={colorKey}
            skus={skusOfColor(product.skus, color)}
            colorPicker={<ColorPicker slug={product.slug} colors={product.colors} selected={color?.name ?? null} />}
          >
            <ul className="grid gap-3 border-t pt-6 text-xs sm:grid-cols-3">
              {storeValues.map(({ icon: Icon, title, text }) => (
                <li key={title} className="flex items-start gap-2">
                  <Icon aria-hidden="true" className="size-5 shrink-0" strokeWidth={1.5} />
                  <span>
                    <span className="block font-semibold">{title}</span>
                    <span className="text-muted-foreground">{text}</span>
                  </span>
                </li>
              ))}
            </ul>
          </PurchasePanel>
          <ProductAccordion description={product.description} attributes={product.attributes} />
        </div>
      </div>
      <RelatedProducts categorySlug={product.categorySlug} productId={product.id} />
    </main>
  );
}

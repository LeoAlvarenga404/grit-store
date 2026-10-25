import { SearchX } from "lucide-react";
import type { Metadata } from "next";
import { EmptyState } from "@/components/empty-state";
import { ErrorMessage } from "@/components/error-message";
import { getFacets, getProducts } from "@/lib/api/catalog-reads";
import type { components } from "@/lib/api/schema";
import { storeCategories } from "@/lib/store-navigation";
import { ActiveFilters } from "./active-filters";
import { catalogHref, readFilters, toFacetQuery, toSearchQuery, type Filters } from "./catalog-href";
import { CategoryBanner } from "./category-banner";
import { FilterPanel } from "./filter-panel";
import { FiltersSheet } from "./filters-sheet";
import { ProductGrid } from "./product-grid";
import { SortSelect } from "./sort-select";

// Rendered per request: the build must not need a running API.
export const dynamic = "force-dynamic";

export const metadata: Metadata = { title: "Produtos" };

type Facets = components["schemas"]["GetProductFacetsResponse"];

export default async function CatalogPage({ searchParams }: { searchParams: Promise<Record<string, string | string[] | undefined>> }) {
  const filters = readFilters(await searchParams);
  const [products, facetsResponse] = await Promise.all([
    getProducts(toSearchQuery(filters)),
    getFacets(toFacetQuery(filters)),
  ]);
  const facets = facetsResponse.data ?? undefined;
  const listKey = catalogHref(filters, {});
  const storeCategory = storeCategories.find((category) => category.slug === filters.category);
  // Every facet count equals the size of its refinement, so the categories add up to the number of matches.
  const totalProducts = facets?.categories.reduce((sum, category) => sum + category.count, 0) ?? null;
  const activeCount = filters.attributes.length + (filters.category ? 1 : 0) + (filters.minPrice || filters.maxPrice ? 1 : 0);

  const list = !products.data ? (
    <ErrorMessage>Não foi possível carregar os produtos. Confira os filtros e tente de novo.</ErrorMessage>
  ) : products.data.items.length === 0 ? (
    <EmptyState
      icon={SearchX}
      title="Nenhum produto encontrado"
      text="Tente outra busca ou remova alguns filtros."
      actionLabel="Ver todos os produtos"
      actionHref="/catalog"
    />
  ) : (
    <ProductGrid
      key={listKey}
      query={toSearchQuery(filters)}
      initialProducts={products.data.items}
      initialCursor={products.data.nextCursor}
      totalProducts={totalProducts}
    />
  );

  return (
    <main>
      {storeCategory && <CategoryBanner slug={storeCategory.slug} name={storeCategory.name} />}
      <div className="mx-auto max-w-screen-2xl px-4 py-8 lg:px-8">
        {!storeCategory && <h1 className="text-display text-3xl md:text-4xl">{catalogTitle(filters, facets)}</h1>}
        <div className="mt-8 flex gap-10">
          <aside aria-label="Filtros" className="hidden w-60 shrink-0 lg:block">
            <FilterPanel filters={filters} facets={facets} />
          </aside>
          <section aria-label="Produtos" className="min-w-0 flex-1">
            <div className="mb-6 flex flex-wrap items-center gap-3">
              <FiltersSheet key={listKey} activeCount={activeCount}>
                <FilterPanel filters={filters} facets={facets} />
              </FiltersSheet>
              {totalProducts !== null && (
                <p className="text-sm text-muted-foreground">
                  <span className="font-semibold text-foreground tabular-nums">{totalProducts}</span> {totalProducts === 1 ? "produto" : "produtos"}
                </p>
              )}
              <div className="ml-auto">
                <SortSelect filters={filters} />
              </div>
            </div>
            <ActiveFilters filters={filters} facets={facets} />
            {list}
          </section>
        </div>
      </div>
    </main>
  );
}

function catalogTitle(filters: Filters, facets: Facets | undefined): string {
  if (filters.q) return `Resultados para “${filters.q}”`;
  const categoryName = facets?.categories.find((category) => category.slug === filters.category)?.name;
  return categoryName ?? "Todos os produtos";
}

import Image from "next/image";
import Link from "next/link";
import { api } from "@/lib/api/client";
import { formatPrice } from "@/lib/format";
import { catalogHref, toSearchQuery, type Filters } from "./catalog-href";
import { FilterPanel } from "./filter-panel";
import { SearchBox } from "./search-box";

// Rendered per request: the build must not need a running API.
export const dynamic = "force-dynamic";

export default async function CatalogPage({ searchParams }: { searchParams: Promise<Filters> }) {
  const filters = await searchParams;
  const query = toSearchQuery(filters);
  const [products, facets] = await Promise.all([
    api.GET("/api/v1/catalog/products", { params: { query: { ...query, after: filters.after || undefined } } }),
    api.GET("/api/v1/catalog/facets", { params: { query } }),
  ]);

  return (
    <main className="catalog">
      <aside>
        <FilterPanel filters={filters} facets={facets.data} />
      </aside>
      <section>
        <h1>Catálogo</h1>
        <SearchBox defaultValue={filters.q} category={filters.category} />
        {!products.data ? (
          <p role="alert" className="error">
            Não foi possível carregar os produtos. Confira os filtros e tente de novo.
          </p>
        ) : products.data.items.length === 0 ? (
          <p>Nenhum produto encontrado.</p>
        ) : (
          <ul className="products">
            {products.data.items.map((product) => (
              <li key={product.id}>
                <Link href={`/products/${product.slug}`}>
                  {product.imageUrl && <Image src={product.imageUrl} alt="" width={192} height={192} unoptimized />}
                  {product.name}
                </Link>
                <p>A partir de {formatPrice(product.priceAmount)}</p>
              </li>
            ))}
          </ul>
        )}
        {products.data?.nextCursor && (
          <Link href={catalogHref(filters, { after: products.data.nextCursor })}>Próxima página</Link>
        )}
      </section>
    </main>
  );
}

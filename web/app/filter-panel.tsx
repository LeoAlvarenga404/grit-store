import Link from "next/link";
import type { components } from "@/lib/api/schema";
import { formatPrice } from "@/lib/format";
import { catalogHref, type Filters } from "./catalog-href";

type Facets = components["schemas"]["GetProductFacetsResponse"];
type PriceRange = components["schemas"]["PriceFacetResponse"];

function priceRangeLabel(range: PriceRange): string {
  return range.maxAmount === null
    ? `A partir de ${formatPrice(range.minAmount)}`
    : `De ${formatPrice(range.minAmount)} a ${formatPrice(range.maxAmount)}`;
}

// A range is minAmount <= price < maxAmount; prices have cents, so the inclusive upper bound is maxAmount - 0.01.
function priceRangeHref(filters: Filters, range: PriceRange): string {
  const maxPrice = range.maxAmount === null ? undefined : (range.maxAmount - 0.01).toFixed(2);
  return catalogHref(filters, { minPrice: String(range.minAmount), maxPrice });
}

export function FilterPanel({ filters, facets }: { filters: Filters; facets: Facets | undefined }) {
  if (!facets) return null;
  const hasPriceFilter = Boolean(filters.minPrice || filters.maxPrice);

  return (
    <nav aria-label="Filtros" className="filters">
      <h2>Categorias</h2>
      <ul>
        {facets.categories.map((category) => (
          <li key={category.slug}>
            <Link
              href={catalogHref(filters, { category: category.slug })}
              aria-current={filters.category === category.slug ? "true" : undefined}
            >
              {category.name} ({category.count})
            </Link>
          </li>
        ))}
      </ul>
      {filters.category && <Link href={catalogHref(filters, { category: undefined })}>Todas as categorias</Link>}
      <h2>Preço</h2>
      <ul>
        {facets.prices.map((range) => (
          <li key={range.minAmount}>
            <Link href={priceRangeHref(filters, range)}>
              {priceRangeLabel(range)} ({range.count})
            </Link>
          </li>
        ))}
      </ul>
      {hasPriceFilter && (
        <Link href={catalogHref(filters, { minPrice: undefined, maxPrice: undefined })}>Qualquer preço</Link>
      )}
    </nav>
  );
}

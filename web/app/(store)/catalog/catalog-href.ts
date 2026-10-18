import type { components, paths } from "@/lib/api/schema";

export type SearchQuery = NonNullable<paths["/api/v1/catalog/products"]["get"]["parameters"]["query"]>;
type FacetQuery = NonNullable<paths["/api/v1/catalog/facets"]["get"]["parameters"]["query"]>;
type PriceRange = components["schemas"]["PriceFacetResponse"];
type SearchParams = Record<string, string | string[] | undefined>;

export type Filters = {
  q?: string;
  category?: string;
  minPrice?: string;
  maxPrice?: string;
  attributes: string[];
  sort?: string;
};

const productsPerPage = 24;

export const sortOptions = [
  { value: "relevance", label: "Relevância" },
  { value: "price_asc", label: "Menor preço" },
  { value: "price_desc", label: "Maior preço" },
];

function first(value: string | string[] | undefined): string | undefined {
  return (Array.isArray(value) ? value[0] : value) || undefined;
}

export function readFilters(params: SearchParams): Filters {
  const attribute = params.attribute ?? [];
  return {
    q: first(params.q),
    category: first(params.category),
    minPrice: first(params.minPrice),
    maxPrice: first(params.maxPrice),
    attributes: (Array.isArray(attribute) ? attribute : [attribute]).filter((pair) => pair.indexOf(":") > 0),
    sort: first(params.sort),
  };
}

// The URL holds the whole filter state; relevance is the API default, so it stays out of the URL.
export function catalogHref(filters: Filters, changes: Partial<Filters>): string {
  const next = { ...filters, ...changes };
  const params = new URLSearchParams();
  for (const key of ["q", "category", "minPrice", "maxPrice"] as const) {
    const value = next[key];
    if (value) params.set(key, value);
  }
  for (const pair of next.attributes) params.append("attribute", pair);
  if (next.sort && next.sort !== "relevance") params.set("sort", next.sort);
  const query = params.toString();
  return query ? `/catalog?${query}` : "/catalog";
}

export function attributeValue(filters: Filters, key: string): string | undefined {
  return filters.attributes.find((pair) => pair.startsWith(`${key}:`))?.slice(key.length + 1);
}

// The API matches one value per attribute key: a value replaces the key's current one, and the same value again removes it.
export function toggleAttribute(filters: Filters, key: string, value: string): string[] {
  const others = filters.attributes.filter((pair) => !pair.startsWith(`${key}:`));
  return attributeValue(filters, key) === value ? others : [...others, `${key}:${value}`];
}

// The facets endpoint documents this intersection: the range's bounds narrowed by the price filter already applied.
export function priceRangeHref(filters: Filters, range: PriceRange): string {
  const currentMin = filters.minPrice ? Number(filters.minPrice) : undefined;
  const currentMax = filters.maxPrice ? Number(filters.maxPrice) : undefined;
  const rangeMax = range.maxAmount === null ? undefined : range.maxAmount - 0.01;
  const minPrice = Math.max(range.minAmount, currentMin ?? range.minAmount);
  const maxPrice = rangeMax === undefined ? currentMax : Math.min(rangeMax, currentMax ?? rangeMax);
  return catalogHref(filters, { minPrice: String(minPrice), maxPrice: maxPrice === undefined ? undefined : maxPrice.toFixed(2) });
}

export function toFacetQuery(filters: Filters): FacetQuery {
  return {
    q: filters.q,
    category: filters.category,
    minPrice: filters.minPrice ? Number(filters.minPrice) : undefined,
    maxPrice: filters.maxPrice ? Number(filters.maxPrice) : undefined,
    attribute: filters.attributes.length > 0 ? filters.attributes : undefined,
  };
}

export function toSearchQuery(filters: Filters): SearchQuery {
  return { ...toFacetQuery(filters), sort: filters.sort, limit: productsPerPage };
}

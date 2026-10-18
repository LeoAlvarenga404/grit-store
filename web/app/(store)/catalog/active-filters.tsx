import { X } from "lucide-react";
import Link from "next/link";
import type { components } from "@/lib/api/schema";
import { formatPrice } from "@/lib/format";
import { attributeGroups } from "./filter-panel";
import { catalogHref, toggleAttribute, type Filters } from "./catalog-href";

type Facets = components["schemas"]["GetProductFacetsResponse"];
type Chip = { id: string; label: string; href: string };

export function ActiveFilters({ filters, facets }: { filters: Filters; facets: Facets | undefined }) {
  const chips = buildChips(filters, facets);
  if (chips.length === 0) return null;
  const hasRefinement = Boolean(filters.category || filters.minPrice || filters.maxPrice || filters.attributes.length > 0);

  return (
    <div className="mb-6 flex flex-wrap items-center gap-2">
      {chips.map((chip) => (
        <Link
          key={chip.id}
          href={chip.href}
          scroll={false}
          aria-label={`Remover filtro ${chip.label}`}
          className="inline-flex h-9 items-center gap-2 rounded-full border px-3 text-sm hover:border-foreground"
        >
          {chip.label}
          <X aria-hidden="true" className="size-3.5" />
        </Link>
      ))}
      {hasRefinement && (
        <Link
          href={catalogHref(filters, { category: undefined, minPrice: undefined, maxPrice: undefined, attributes: [] })}
          scroll={false}
          className="text-sm underline underline-offset-4"
        >
          Limpar filtros
        </Link>
      )}
    </div>
  );
}

function buildChips(filters: Filters, facets: Facets | undefined): Chip[] {
  return [...searchChips(filters), ...categoryChips(filters, facets), ...priceChips(filters), ...attributeChips(filters)];
}

function searchChips(filters: Filters): Chip[] {
  if (!filters.q) return [];
  return [{ id: "q", label: `Busca: ${filters.q}`, href: catalogHref(filters, { q: undefined }) }];
}

function categoryChips(filters: Filters, facets: Facets | undefined): Chip[] {
  if (!filters.category) return [];
  const name = facets?.categories.find((category) => category.slug === filters.category)?.name ?? filters.category;
  return [{ id: "category", label: name, href: catalogHref(filters, { category: undefined }) }];
}

function priceChips(filters: Filters): Chip[] {
  if (!filters.minPrice && !filters.maxPrice) return [];
  const href = catalogHref(filters, { minPrice: undefined, maxPrice: undefined });
  return [{ id: "price", label: priceLabel(filters.minPrice, filters.maxPrice), href }];
}

function priceLabel(minPrice: string | undefined, maxPrice: string | undefined): string {
  if (minPrice && maxPrice) return `${formatPrice(Number(minPrice))} a ${formatPrice(Number(maxPrice))}`;
  if (minPrice) return `A partir de ${formatPrice(Number(minPrice))}`;
  return `Até ${formatPrice(Number(maxPrice))}`;
}

function attributeChips(filters: Filters): Chip[] {
  return filters.attributes.map((pair) => {
    const separatorIndex = pair.indexOf(":");
    const key = pair.slice(0, separatorIndex);
    const value = pair.slice(separatorIndex + 1);
    const title = attributeGroups.find((group) => group.key === key)?.title ?? key;
    return {
      id: pair,
      label: `${title}: ${value}`,
      href: catalogHref(filters, { attributes: toggleAttribute(filters, key, value) }),
    };
  });
}

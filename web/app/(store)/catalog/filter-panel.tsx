import Link from "next/link";
import type { ReactNode } from "react";
import { ColorSwatch } from "@/components/color-swatch";
import type { components } from "@/lib/api/schema";
import { cn } from "@/lib/cn";
import { formatPrice } from "@/lib/format";
import { compareSizes } from "@/lib/sizes";
import { attributeValue, catalogHref, priceRangeHref, toggleAttribute, type Filters } from "./catalog-href";

type Facets = components["schemas"]["GetProductFacetsResponse"];
type AttributeValue = components["schemas"]["AttributeValueFacetResponse"];

export const attributeGroups = [
  { key: "genero", title: "Gênero" },
  { key: "cor", title: "Cor" },
  { key: "tamanho", title: "Tamanho" },
  { key: "marca", title: "Marca" },
];

export function priceRangeLabel(range: { minAmount: number; maxAmount: number | null }): string {
  return range.maxAmount === null
    ? `A partir de ${formatPrice(range.minAmount)}`
    : `${formatPrice(range.minAmount)} a ${formatPrice(range.maxAmount)}`;
}

export function FilterPanel({ filters, facets }: { filters: Filters; facets: Facets | undefined }) {
  if (!facets) return null;

  return (
    <div>
      {facets.categories.length > 0 && (
        <FacetGroup title="Categoria">
          <ul className="space-y-1">
            {facets.categories.map((category) => {
              const isActive = filters.category === category.slug;
              const href = catalogHref(filters, { category: isActive ? undefined : category.slug });
              return (
                <li key={category.slug}>
                  <FacetLink href={href} isActive={isActive} label={category.name} count={category.count} />
                </li>
              );
            })}
          </ul>
        </FacetGroup>
      )}
      {attributeGroups.map((group) => {
        const values = facets.attributes.find((facet) => facet.key === group.key)?.values ?? [];
        if (values.length === 0) return null;
        return <AttributeGroup key={group.key} filters={filters} groupKey={group.key} title={group.title} values={values} />;
      })}
      {facets.prices.length > 0 && (
        <FacetGroup title="Preço">
          <ul className="space-y-1">
            {facets.prices.map((range) => (
              <li key={`${range.minAmount}-${range.maxAmount}`}>
                <FacetLink href={priceRangeHref(filters, range)} isActive={false} label={priceRangeLabel(range)} count={range.count} />
              </li>
            ))}
          </ul>
        </FacetGroup>
      )}
    </div>
  );
}

type AttributeProps = { filters: Filters; groupKey: string; title: string; values: AttributeValue[] };

function AttributeGroup(props: AttributeProps) {
  if (props.groupKey === "cor") return <ColorValues {...props} />;
  if (props.groupKey === "tamanho") return <SizeValues {...props} />;
  return <ListValues {...props} />;
}

function ListValues({ filters, groupKey, title, values }: AttributeProps) {
  return (
    <FacetGroup title={title}>
      <ul className="space-y-1">
        {values.map((item) => (
          <li key={item.value}>
            <FacetLink
              href={catalogHref(filters, { attributes: toggleAttribute(filters, groupKey, item.value) })}
              isActive={attributeValue(filters, groupKey) === item.value}
              label={item.value}
              count={item.count}
            />
          </li>
        ))}
      </ul>
    </FacetGroup>
  );
}

function ColorValues({ filters, groupKey, title, values }: AttributeProps) {
  return (
    <FacetGroup title={title}>
      <ul className="grid grid-cols-2 gap-2">
        {values.map((item) => {
          const isActive = attributeValue(filters, groupKey) === item.value;
          return (
            <li key={item.value}>
              <Link
                href={catalogHref(filters, { attributes: toggleAttribute(filters, groupKey, item.value) })}
                scroll={false}
                aria-current={isActive ? "true" : undefined}
                className="flex min-h-9 items-center gap-2 text-sm capitalize"
              >
                <ColorSwatch name={item.value} imageUrl={null} className={cn("size-5", isActive && "ring-2 ring-foreground ring-offset-2")} />
                {item.value}
                <span className="text-muted-foreground tabular-nums">({item.count})</span>
              </Link>
            </li>
          );
        })}
      </ul>
    </FacetGroup>
  );
}

function SizeValues({ filters, groupKey, title, values }: AttributeProps) {
  const sizes = [...values].sort((left, right) => compareSizes(left.value, right.value));
  return (
    <FacetGroup title={title}>
      <ul className="flex flex-wrap gap-2">
        {sizes.map((item) => {
          const isActive = attributeValue(filters, groupKey) === item.value;
          return (
            <li key={item.value}>
              <Link
                href={catalogHref(filters, { attributes: toggleAttribute(filters, groupKey, item.value) })}
                scroll={false}
                aria-current={isActive ? "true" : undefined}
                aria-label={`Tamanho ${item.value}, ${item.count} produtos`}
                className={cn(
                  "flex h-10 min-w-11 items-center justify-center rounded-sm border px-3 text-sm hover:border-foreground",
                  isActive && "border-foreground bg-foreground text-background",
                )}
              >
                {item.value}
              </Link>
            </li>
          );
        })}
      </ul>
    </FacetGroup>
  );
}

function FacetGroup({ title, children }: { title: string; children: ReactNode }) {
  return (
    <details open className="border-b pb-6">
      <summary className="text-eyebrow cursor-pointer list-none py-4">{title}</summary>
      {children}
    </details>
  );
}

function FacetLink({ href, isActive, label, count }: { href: string; isActive: boolean; label: string; count: number }) {
  return (
    <Link
      href={href}
      scroll={false}
      aria-current={isActive ? "true" : undefined}
      className={cn("flex min-h-9 items-center justify-between gap-2 text-sm hover:underline", isActive && "font-semibold")}
    >
      {label}
      <span className="text-muted-foreground tabular-nums">({count})</span>
    </Link>
  );
}

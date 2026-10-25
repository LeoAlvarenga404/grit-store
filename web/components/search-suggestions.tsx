import { ArrowRight, Search, Tag } from "lucide-react";
import Image from "next/image";
import Link from "next/link";
import { cn } from "@/lib/cn";
import { formatPrice } from "@/lib/format";
import { type SuggestionEntry, type Suggestions } from "@/lib/search-suggestions";

type Props = {
  id: string;
  text: string;
  suggestions: Suggestions;
  entries: SuggestionEntry[];
  activeIndex: number;
  isLoading: boolean;
  onNavigate: () => void;
};

const rowClass = "flex items-center gap-3 px-4 py-2.5 text-sm";

export function SearchSuggestions({ id, text, suggestions, entries, activeIndex, isLoading, onNavigate }: Props) {
  const indexOf = (key: string) => entries.findIndex((entry) => entry.key === key);
  const optionProps = (key: string) => ({
    id: `${id}-${key}`,
    role: "option" as const,
    "aria-selected": indexOf(key) === activeIndex,
    onClick: onNavigate,
  });
  const hasMatches = suggestions.products.length > 0 || suggestions.categories.length > 0;

  return (
    <div
      id={id}
      role="listbox"
      aria-label="Sugestões de busca"
      className={cn("absolute top-full right-0 z-50 mt-2 w-full overflow-hidden rounded-lg border bg-background shadow-lg md:w-[26rem]", isLoading && "opacity-60")}
    >
      {!hasMatches && !isLoading && <p className="px-4 py-6 text-center text-sm text-muted-foreground">Nenhum produto para “{text}”.</p>}
      {suggestions.categories.length > 0 && (
        <div>
          <p className="px-4 pt-3 pb-1 text-xs font-semibold tracking-[0.08em] text-muted-foreground uppercase">Categorias</p>
          {suggestions.categories.map((category) => {
            const entry = entries[indexOf(`category-${category.slug}`)];
            return (
              <Link key={entry.key} href={entry.href} {...optionProps(entry.key)} className={cn(rowClass, "hover:bg-muted aria-selected:bg-muted")}>
                <Tag aria-hidden="true" className="size-4 text-muted-foreground" />
                <span className="flex-1">{category.name}</span>
                <span className="text-xs text-muted-foreground tabular-nums">{category.count}</span>
              </Link>
            );
          })}
        </div>
      )}
      {suggestions.products.length > 0 && (
        <div>
          <p className="px-4 pt-3 pb-1 text-xs font-semibold tracking-[0.08em] text-muted-foreground uppercase">Produtos</p>
          {suggestions.products.map((product) => {
            const entry = entries[indexOf(`product-${product.slug}`)];
            return (
              <Link key={entry.key} href={entry.href} {...optionProps(entry.key)} className={cn(rowClass, "hover:bg-muted aria-selected:bg-muted")}>
                <span className="relative size-12 shrink-0 bg-muted">
                  {product.imageUrl && <Image src={product.imageUrl} alt="" fill sizes="48px" className="object-contain p-1 mix-blend-multiply" unoptimized />}
                </span>
                <span className="min-w-0 flex-1">
                  <span className="line-clamp-2 leading-snug">{product.name}</span>
                  <span className="text-xs font-semibold tabular-nums">
                    {product.hasPriceRange && <span className="font-normal text-muted-foreground">a partir de </span>}
                    {formatPrice(product.priceAmount)}
                  </span>
                </span>
              </Link>
            );
          })}
        </div>
      )}
      <Link
        {...optionProps("all")}
        href={entries[indexOf("all")].href}
        className={cn(rowClass, "mt-1 border-t font-medium hover:bg-muted aria-selected:bg-muted")}
      >
        <Search aria-hidden="true" className="size-4 text-muted-foreground" />
        <span className="flex-1">Ver todos os resultados para “{text}”</span>
        <ArrowRight aria-hidden="true" className="size-4" />
      </Link>
    </div>
  );
}

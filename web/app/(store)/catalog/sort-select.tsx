"use client";

import { useRouter } from "next/navigation";
import { NativeSelect, NativeSelectOption } from "@/components/ui/native-select";
import { catalogHref, sortOptions, type Filters } from "./catalog-href";

export function SortSelect({ filters }: { filters: Filters }) {
  const router = useRouter();

  return (
    <div className="ml-auto flex items-center gap-2">
      <label htmlFor="catalog-sort" className="text-sm text-muted-foreground">
        Ordenar por
      </label>
      <NativeSelect
        id="catalog-sort"
        key={filters.sort ?? "relevance"}
        defaultValue={filters.sort ?? "relevance"}
        onChange={(event) => router.push(catalogHref(filters, { sort: event.target.value }), { scroll: false })}
        className="h-10"
      >
        {sortOptions.map((option) => (
          <NativeSelectOption key={option.value} value={option.value}>
            {option.label}
          </NativeSelectOption>
        ))}
      </NativeSelect>
    </div>
  );
}

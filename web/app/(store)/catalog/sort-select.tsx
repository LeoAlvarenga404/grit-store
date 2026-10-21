"use client";

import { useRouter } from "next/navigation";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { catalogHref, sortOptions, type Filters } from "./catalog-href";

export function SortSelect({ filters }: { filters: Filters }) {
  const router = useRouter();

  return (
    <Select value={filters.sort ?? "relevance"} onValueChange={(sort) => router.push(catalogHref(filters, { sort }), { scroll: false })}>
      <SelectTrigger aria-label="Ordenar produtos">
        <span className="text-muted-foreground">Ordenar por</span>
        <SelectValue />
      </SelectTrigger>
      <SelectContent>
        {sortOptions.map((option) => (
          <SelectItem key={option.value} value={option.value}>
            {option.label}
          </SelectItem>
        ))}
      </SelectContent>
    </Select>
  );
}

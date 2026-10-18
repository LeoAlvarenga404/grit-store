"use client";

import { Search } from "lucide-react";
import { useId, useState } from "react";
import { cn } from "@/lib/cn";

// ponytail: one request per keystroke and a slow answer may overwrite a newer one; debounce if the API feels it.
export function SearchField({ defaultValue, className }: { defaultValue?: string; className?: string }) {
  const [suggestions, setSuggestions] = useState<string[]>([]);
  const listId = useId();

  async function suggest(text: string) {
    if (text.trim().length < 2) {
      setSuggestions([]);
      return;
    }
    const response = await fetch(`/api/suggestions?q=${encodeURIComponent(text)}`);
    const items: { name: string }[] = response.ok ? await response.json() : [];
    setSuggestions(items.map((item) => item.name));
  }

  return (
    <form action="/catalog" role="search" className={cn("relative", className)}>
      <label className="sr-only" htmlFor={`${listId}-input`}>
        Buscar produtos
      </label>
      <Search aria-hidden="true" className="pointer-events-none absolute top-1/2 left-3 size-4 -translate-y-1/2 text-muted-foreground" />
      <input
        id={`${listId}-input`}
        name="q"
        type="search"
        list={listId}
        defaultValue={defaultValue}
        placeholder="O que você procura?"
        onChange={(event) => void suggest(event.target.value)}
        className="h-10 w-full rounded-full border-0 bg-muted pr-4 pl-9 text-sm placeholder:text-muted-foreground focus-visible:bg-background focus-visible:ring-1 focus-visible:ring-foreground focus-visible:outline-none"
      />
      <datalist id={listId}>
        {suggestions.map((name) => (
          <option key={name} value={name} />
        ))}
      </datalist>
    </form>
  );
}

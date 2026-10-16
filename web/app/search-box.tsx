"use client";

import { useState } from "react";

// ponytail: one request per keystroke and a slow answer may overwrite a newer one; debounce if the API feels it.
export function SearchBox({ defaultValue, category }: { defaultValue?: string; category?: string }) {
  const [suggestions, setSuggestions] = useState<string[]>([]);

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
    <form action="/" role="search" className="search">
      <label>
        Buscar produtos
        <input
          name="q"
          type="search"
          list="product-suggestions"
          defaultValue={defaultValue}
          onChange={(event) => void suggest(event.target.value)}
        />
      </label>
      <datalist id="product-suggestions">
        {suggestions.map((name) => (
          <option key={name} value={name} />
        ))}
      </datalist>
      {category && <input type="hidden" name="category" value={category} />}
      <button type="submit">Buscar</button>
    </form>
  );
}

"use client";

import { Search } from "lucide-react";
import { useRouter } from "next/navigation";
import { useEffect, useId, useRef, useState } from "react";
import { SearchSuggestions } from "@/components/search-suggestions";
import { cn } from "@/lib/cn";
import { type Suggestions, suggestionEntries } from "@/lib/search-suggestions";

const minimumLength = 2;
const debounceInMilliseconds = 200;
const noSuggestions: Suggestions = { products: [], categories: [] };

export function SearchField({ defaultValue, className, onNavigate }: { defaultValue?: string; className?: string; onNavigate?: () => void }) {
  const router = useRouter();
  const id = useId();
  const formRef = useRef<HTMLFormElement>(null);
  const [text, setText] = useState(defaultValue ?? "");
  const [suggestions, setSuggestions] = useState(noSuggestions);
  const [isOpen, setIsOpen] = useState(false);
  const [isLoading, setIsLoading] = useState(false);
  const [activeIndex, setActiveIndex] = useState(-1);

  function close() {
    setIsOpen(false);
    onNavigate?.();
  }

  const query = text.trim();
  const isSearchable = query.length >= minimumLength;
  const entries = suggestionEntries(query, suggestions);
  const isPanelVisible = isOpen && isSearchable;

  useEffect(() => {
    if (!isSearchable) return;
    // Aborting on every change keeps a slow answer from overwriting a newer one.
    const controller = new AbortController();
    const timer = setTimeout(async () => {
      setIsLoading(true);
      try {
        const response = await fetch(`/api/suggestions?q=${encodeURIComponent(query)}`, { signal: controller.signal });
        setSuggestions(response.ok ? await response.json() : noSuggestions);
        setActiveIndex(-1);
        setIsLoading(false);
      } catch {
        if (!controller.signal.aborted) setIsLoading(false);
      }
    }, debounceInMilliseconds);
    return () => {
      clearTimeout(timer);
      controller.abort();
    };
  }, [query, isSearchable]);

  useEffect(() => {
    function closeOnOutsideClick(event: MouseEvent) {
      if (!formRef.current?.contains(event.target as Node)) setIsOpen(false);
    }
    document.addEventListener("mousedown", closeOnOutsideClick);
    return () => document.removeEventListener("mousedown", closeOnOutsideClick);
  }, []);

  function onKeyDown(event: React.KeyboardEvent<HTMLInputElement>) {
    if (event.key === "Escape") return setIsOpen(false);
    if (!isPanelVisible) return;
    if (event.key === "ArrowDown" || event.key === "ArrowUp") {
      event.preventDefault();
      const step = event.key === "ArrowDown" ? 1 : -1;
      // -1 is the input itself: the arrows cycle through it and the entries.
      setActiveIndex((current) => ((current + 1 + step + entries.length + 1) % (entries.length + 1)) - 1);
    }
    if (event.key === "Enter" && activeIndex >= 0) {
      event.preventDefault();
      close();
      router.push(entries[activeIndex].href);
    }
  }

  return (
    <form ref={formRef} action="/catalog" role="search" onSubmit={close} className={cn("relative", className)}>
      <label className="sr-only" htmlFor={`${id}-input`}>
        Buscar produtos
      </label>
      <Search aria-hidden="true" className="pointer-events-none absolute top-1/2 left-3 size-4 -translate-y-1/2 text-muted-foreground" />
      <input
        id={`${id}-input`}
        name="q"
        type="search"
        role="combobox"
        aria-expanded={isPanelVisible}
        aria-controls={`${id}-panel`}
        aria-autocomplete="list"
        aria-activedescendant={isPanelVisible && activeIndex >= 0 ? `${id}-panel-${entries[activeIndex].key}` : undefined}
        autoComplete="off"
        value={text}
        placeholder="O que você procura?"
        onChange={(event) => {
          setText(event.target.value);
          setIsOpen(true);
        }}
        onFocus={() => setIsOpen(true)}
        onKeyDown={onKeyDown}
        className="h-10 w-full rounded-full border-0 bg-muted pr-4 pl-9 text-sm placeholder:text-muted-foreground focus-visible:bg-background focus-visible:ring-1 focus-visible:ring-foreground focus-visible:outline-none"
      />
      {isPanelVisible && (
        <SearchSuggestions
          id={`${id}-panel`}
          text={query}
          suggestions={suggestions}
          entries={entries}
          activeIndex={activeIndex}
          isLoading={isLoading}
          onNavigate={close}
        />
      )}
    </form>
  );
}

import { useSyncExternalStore } from "react";
import { addSlug, parseSlugs, toggleSlug } from "./slug-list";

const noSlugs: string[] = [];

// A list kept in localStorage and shared by every component and tab that reads it. When the browser blocks storage
// the list lives in memory until the page closes.
export function createSlugStore(key: string, max: number) {
  const listeners = new Set<() => void>();
  let memory = noSlugs;
  let cachedRaw: string | null | undefined;
  let cached = noSlugs;

  // useSyncExternalStore needs the same array back until the stored value changes.
  function read(): string[] {
    let raw: string | null;
    try {
      raw = window.localStorage.getItem(key);
    } catch {
      return memory;
    }
    if (raw !== cachedRaw) {
      cachedRaw = raw;
      cached = parseSlugs(raw);
    }
    return cached;
  }

  function write(list: string[]) {
    try {
      window.localStorage.setItem(key, JSON.stringify(list));
    } catch {
      memory = list;
    }
    listeners.forEach((listener) => listener());
  }

  function subscribe(listener: () => void) {
    listeners.add(listener);
    const onStorage = (event: StorageEvent) => {
      if (event.key === key) listener();
    };
    window.addEventListener("storage", onStorage);
    return () => {
      listeners.delete(listener);
      window.removeEventListener("storage", onStorage);
    };
  }

  return {
    subscribe,
    read,
    add: (slug: string) => write(addSlug(read(), slug, max)),
    toggle: (slug: string) => write(toggleSlug(read(), slug, max)),
  };
}

type SlugStore = ReturnType<typeof createSlugStore>;

export function useSlugs(store: SlugStore): string[] {
  return useSyncExternalStore(store.subscribe, store.read, () => noSlugs);
}

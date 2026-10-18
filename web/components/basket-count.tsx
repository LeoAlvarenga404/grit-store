"use client";

import { ShoppingBag } from "lucide-react";
import Link from "next/link";
import { createContext, useContext, useOptimistic, type ReactNode } from "react";
import { cn } from "@/lib/cn";

type BasketCount = { count: number; addOptimistic: (quantity: number) => void };

const BasketCountContext = createContext<BasketCount | null>(null);

// The count comes from the server; an add-to-bag raises it inside its transition and the server's new count
// replaces it when the action ends (the action calls refresh()).
export function BasketCountProvider({ count, children }: { count: number; children: ReactNode }) {
  const [optimisticCount, addOptimistic] = useOptimistic(count, (current: number, quantity: number) => current + quantity);
  return <BasketCountContext value={{ count: optimisticCount, addOptimistic }}>{children}</BasketCountContext>;
}

export function useBasketCount(): BasketCount {
  const basketCount = useContext(BasketCountContext);
  if (!basketCount) throw new Error("useBasketCount needs a BasketCountProvider above it.");
  return basketCount;
}

export function BasketLink({ className }: { className?: string }) {
  const { count } = useBasketCount();

  return (
    <Link href="/basket" aria-label={`Sacola, ${count} ${count === 1 ? "item" : "itens"}`} className={cn("relative", className)}>
      <ShoppingBag className="size-5" />
      {count > 0 && (
        <span className="absolute top-0.5 right-0.5 flex min-w-4 items-center justify-center rounded-full bg-foreground px-1 text-[10px] leading-4 font-semibold text-background tabular-nums">
          {count}
        </span>
      )}
    </Link>
  );
}

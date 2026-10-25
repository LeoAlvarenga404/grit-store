"use client";

import { Heart } from "lucide-react";
import Link from "next/link";
import { cn } from "@/lib/cn";
import { favorites } from "@/lib/shopper-lists";
import { useSlugs } from "@/lib/slug-store";

export function FavoritesLink({ className }: { className?: string }) {
  const count = useSlugs(favorites).length;

  return (
    <Link href="/favoritos" aria-label={`Favoritos, ${count} ${count === 1 ? "item" : "itens"}`} className={cn("relative", className)}>
      <Heart className="size-5" />
      {count > 0 && (
        <span className="absolute top-0.5 right-0.5 flex min-w-4 items-center justify-center rounded-full bg-foreground px-1 text-[10px] leading-4 font-semibold text-background tabular-nums">
          {count}
        </span>
      )}
    </Link>
  );
}

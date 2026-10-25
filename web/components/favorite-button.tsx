"use client";

import { Heart } from "lucide-react";
import { toast } from "sonner";
import { cn } from "@/lib/cn";
import { favorites } from "@/lib/shopper-lists";
import { useSlugs } from "@/lib/slug-store";

export function FavoriteButton({ slug, name, className }: { slug: string; name: string; className?: string }) {
  const isFavorite = useSlugs(favorites).includes(slug);

  function toggle() {
    favorites.toggle(slug);
    toast(isFavorite ? "Removido dos favoritos" : "Adicionado aos favoritos");
  }

  return (
    <button
      type="button"
      onClick={toggle}
      aria-pressed={isFavorite}
      aria-label={isFavorite ? `Remover ${name} dos favoritos` : `Adicionar ${name} aos favoritos`}
      className={cn("inline-flex size-9 items-center justify-center rounded-full bg-background/90 shadow-sm transition-transform hover:scale-110 focus-visible:ring-2 focus-visible:ring-ring focus-visible:outline-none", className)}
    >
      <Heart aria-hidden="true" className={cn("size-[18px] transition-colors", isFavorite && "fill-foreground")} />
    </button>
  );
}

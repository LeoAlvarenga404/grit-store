import type { Metadata } from "next";
import { FavoritesList } from "./favorites-list";

export const metadata: Metadata = { title: "Favoritos" };

export default function FavoritesPage() {
  return (
    <main className="mx-auto max-w-screen-2xl px-4 py-10 lg:px-8">
      <h1 className="text-display text-3xl md:text-5xl">Favoritos</h1>
      <p className="mt-3 text-sm text-muted-foreground">Guardados neste navegador.</p>
      <div className="mt-8">
        <FavoritesList />
      </div>
    </main>
  );
}

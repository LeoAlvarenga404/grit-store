import { createSlugStore } from "./slug-store";

export const favorites = createSlugStore("grit:favorites", 48);

export const recentlyViewed = createSlugStore("grit:recently-viewed", 12);

import { api } from "@/lib/api/client";
import type { Tokens } from "./session";

// The API revokes the whole session family when a refresh token is used twice (no grace window), so requests
// carrying the same token share one refresh, and its result is kept for requests still in flight with the old
// cookie. The map lives on globalThis because the proxy and the pages are separate bundles in one process.
// ponytail: per-process map; several Next.js instances need a shared store (Redis) or sticky sessions.
const reuseWindowInMilliseconds = 30_000;

const shared = globalThis as typeof globalThis & { gritRefreshes?: Map<string, Promise<Tokens | null>> };
const refreshes = (shared.gritRefreshes ??= new Map<string, Promise<Tokens | null>>());

export function refreshTokens(refreshToken: string): Promise<Tokens | null> {
  const pending = refreshes.get(refreshToken);
  if (pending) return pending;

  const refresh = requestRefresh(refreshToken);
  refreshes.set(refreshToken, refresh);
  const forget = () => setTimeout(() => refreshes.delete(refreshToken), reuseWindowInMilliseconds);
  refresh.then(forget, forget);
  return refresh;
}

async function requestRefresh(refreshToken: string): Promise<Tokens | null> {
  const { data } = await api.POST("/api/v1/identity/refresh", { body: { refreshToken }, cache: "no-store" });
  return data ?? null;
}

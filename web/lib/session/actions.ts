"use server";

import { cookies, headers } from "next/headers";
import { redirect } from "next/navigation";
import { api } from "@/lib/api/client";
import { errorCode } from "@/lib/api/problem";
import { safeReturnPath } from "./return-path";
import { clearSession, refreshTokenCookie, writeSession } from "./session";

function readCredentials(formData: FormData) {
  return { email: String(formData.get("email") ?? ""), password: String(formData.get("password") ?? "") };
}

// The API trusts X-Forwarded-For only from this server, so each browser gets its own login rate-limit window.
// ponytail: the last hop is the address seen by the proxy in front of Next.js; without such a proxy a client can pick it.
async function clientAddress(): Promise<Record<string, string>> {
  const forwardedFor = (await headers()).get("x-forwarded-for")?.split(",").at(-1)?.trim();
  return forwardedFor ? { "X-Forwarded-For": forwardedFor } : {};
}

// A failed attempt keeps the page the visitor was heading to, so the next one still lands there.
function authErrorPath(page: "/login" | "/register", error: string, next: string): string {
  const query = new URLSearchParams({ error });
  if (next !== "/") query.set("next", next);
  return `${page}?${query}`;
}

export async function logIn(formData: FormData) {
  const next = safeReturnPath(formData.get("next"));
  const result = await api.POST("/api/v1/identity/login", {
    body: readCredentials(formData),
    headers: await clientAddress(),
  });
  if (!result.data) redirect(authErrorPath("/login", errorCode(result), next));
  writeSession(await cookies(), result.data);
  redirect(next);
}

export async function register(formData: FormData) {
  const result = await api.POST("/api/v1/identity/register", { body: readCredentials(formData) });
  if (!result.data) redirect(authErrorPath("/register", errorCode(result), safeReturnPath(formData.get("next"))));
  await logIn(formData);
}

const logOutDeadlineInMilliseconds = 3_000;

export async function logOut() {
  const cookieStore = await cookies();
  const refreshToken = cookieStore.get(refreshTokenCookie)?.value;
  // Best effort with a short deadline: the cookies go even when the API is slow or unreachable,
  // and the session then ends when its refresh token expires.
  if (refreshToken) {
    await api
      .POST("/api/v1/identity/logout", {
        body: { refreshToken },
        cache: "no-store",
        signal: AbortSignal.timeout(logOutDeadlineInMilliseconds),
      })
      .catch(() => undefined);
  }
  clearSession(cookieStore);
  redirect("/login");
}

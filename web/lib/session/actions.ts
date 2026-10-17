"use server";

import { cookies, headers } from "next/headers";
import { redirect } from "next/navigation";
import { api } from "@/lib/api/client";
import { errorCode } from "@/lib/api/problem";
import { clearSession, writeSession } from "./session";

function readCredentials(formData: FormData) {
  return { email: String(formData.get("email") ?? ""), password: String(formData.get("password") ?? "") };
}

// The API trusts X-Forwarded-For only from this server, so each browser gets its own login rate-limit window.
// ponytail: the last hop is the address seen by the proxy in front of Next.js; without such a proxy a client can pick it.
async function clientAddress(): Promise<Record<string, string>> {
  const forwardedFor = (await headers()).get("x-forwarded-for")?.split(",").at(-1)?.trim();
  return forwardedFor ? { "X-Forwarded-For": forwardedFor } : {};
}

export async function logIn(formData: FormData) {
  const result = await api.POST("/api/v1/identity/login", {
    body: readCredentials(formData),
    headers: await clientAddress(),
  });
  if (!result.data) redirect(`/login?error=${encodeURIComponent(errorCode(result))}`);
  writeSession(await cookies(), result.data);
  redirect("/");
}

export async function register(formData: FormData) {
  const result = await api.POST("/api/v1/identity/register", { body: readCredentials(formData) });
  if (!result.data) redirect(`/register?error=${encodeURIComponent(errorCode(result))}`);
  await logIn(formData);
}

export async function logOut() {
  clearSession(await cookies());
  redirect("/login");
}

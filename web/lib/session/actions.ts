"use server";

import { cookies } from "next/headers";
import { redirect } from "next/navigation";
import { api } from "@/lib/api/client";
import { errorCode } from "@/lib/api/problem";
import { clearSession, writeSession } from "./session";

function readCredentials(formData: FormData) {
  return { email: String(formData.get("email") ?? ""), password: String(formData.get("password") ?? "") };
}

export async function logIn(formData: FormData) {
  const result = await api.POST("/api/v1/identity/login", { body: readCredentials(formData) });
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

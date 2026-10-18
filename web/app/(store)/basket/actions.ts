"use server";

import { redirect } from "next/navigation";
import { api } from "@/lib/api/client";
import { callApi } from "@/lib/api/call-api";
import { errorCode } from "@/lib/api/problem";

function readSkuId(formData: FormData): string {
  return String(formData.get("skuId") ?? "");
}

function readQuantity(formData: FormData): number {
  return Number(formData.get("quantity"));
}

export async function addToBasket(formData: FormData) {
  const slug = String(formData.get("slug") ?? "");
  const result = await callApi((headers) =>
    api.POST("/api/v1/basket/lines", {
      headers,
      body: { skuId: readSkuId(formData), quantity: readQuantity(formData) },
    }),
  );
  if (!result.response.ok) redirect(`/products/${encodeURIComponent(slug)}?error=${encodeURIComponent(errorCode(result))}`);
  redirect("/basket");
}

export async function changeQuantity(formData: FormData) {
  const result = await callApi((headers) =>
    api.PUT("/api/v1/basket/lines/{skuId}", {
      headers,
      params: { path: { skuId: readSkuId(formData) } },
      body: { quantity: readQuantity(formData) },
    }),
  );
  if (!result.response.ok) redirect(`/basket?error=${encodeURIComponent(errorCode(result))}`);
  redirect("/basket");
}

export async function removeLine(formData: FormData) {
  await callApi((headers) =>
    api.DELETE("/api/v1/basket/lines/{skuId}", { headers, params: { path: { skuId: readSkuId(formData) } } }),
  );
  redirect("/basket");
}

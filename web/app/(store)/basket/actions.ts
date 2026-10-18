"use server";

import { refresh, revalidatePath } from "next/cache";
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

// Called by the product page inside a transition: it answers instead of redirecting, so the page can show a toast,
// and refresh() brings the header's new bag count.
export async function addToBasket(skuId: string, quantity: number): Promise<{ errorCode: string | null }> {
  const result = await callApi((headers) => api.POST("/api/v1/basket/lines", { headers, body: { skuId, quantity } }));
  if (!result.response.ok) return { errorCode: errorCode(result) };
  refresh();
  return { errorCode: null };
}

// The layout holds the header's bag count and a redirect alone may reuse it, so the layout is revalidated first.
export async function changeQuantity(formData: FormData) {
  const result = await callApi((headers) =>
    api.PUT("/api/v1/basket/lines/{skuId}", {
      headers,
      params: { path: { skuId: readSkuId(formData) } },
      body: { quantity: readQuantity(formData) },
    }),
  );
  revalidatePath("/", "layout");
  if (!result.response.ok) redirect(`/basket?error=${encodeURIComponent(errorCode(result))}`);
  redirect("/basket");
}

export async function removeLine(formData: FormData) {
  await callApi((headers) =>
    api.DELETE("/api/v1/basket/lines/{skuId}", { headers, params: { path: { skuId: readSkuId(formData) } } }),
  );
  revalidatePath("/", "layout");
  redirect("/basket");
}

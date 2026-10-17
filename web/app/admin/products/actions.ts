"use server";

import { redirect } from "next/navigation";
import { api } from "@/lib/api/client";
import { callApi } from "@/lib/api/call-api";
import { errorCode } from "@/lib/api/problem";

function productPath(productId: string): string {
  return `/admin/products/${encodeURIComponent(productId)}`;
}

function backToProduct(productId: string, result: { error?: unknown; response: Response }): never {
  const query = result.response.ok ? "" : `?error=${encodeURIComponent(errorCode(result))}`;
  redirect(`${productPath(productId)}${query}`);
}

export async function changePrice(formData: FormData) {
  const productId = String(formData.get("productId") ?? "");
  const skuId = String(formData.get("skuId") ?? "");
  const result = await callApi((headers) =>
    api.PUT("/api/v1/admin/products/{id}/skus/{skuId}/price", {
      headers,
      params: { path: { id: productId, skuId } },
      body: { priceAmount: Number(formData.get("priceAmount")), priceCurrency: "BRL", version: Number(formData.get("version")) },
    }),
  );
  backToProduct(productId, result);
}

export async function setStock(formData: FormData) {
  const productId = String(formData.get("productId") ?? "");
  const skuId = String(formData.get("skuId") ?? "");
  const result = await callApi((headers) =>
    api.PUT("/api/v1/admin/stock/{skuId}", {
      headers,
      params: { path: { skuId } },
      body: { onHand: Number(formData.get("onHand")) },
    }),
  );
  backToProduct(productId, result);
}

export async function removeImage(formData: FormData) {
  const productId = String(formData.get("productId") ?? "");
  const result = await callApi((headers) =>
    api.DELETE("/api/v1/admin/products/{id}/images/{fileName}", {
      headers,
      params: {
        path: { id: productId, fileName: String(formData.get("fileName") ?? "") },
        query: { version: Number(formData.get("version")) },
      },
    }),
  );
  backToProduct(productId, result);
}

// The BFF sends the bytes to the presigned URL itself: the browser never talks to the storage (no CORS, no client JavaScript).
export async function uploadImage(formData: FormData) {
  const productId = String(formData.get("productId") ?? "");
  const file = formData.get("file");
  if (!(file instanceof File) || file.size === 0) redirect(`${productPath(productId)}?error=image_missing`);

  const upload = await callApi((headers) =>
    api.POST("/api/v1/admin/products/{id}/images/uploads", {
      headers,
      params: { path: { id: productId } },
      body: { contentType: file.type, sizeInBytes: file.size },
    }),
  );
  if (!upload.data) backToProduct(productId, upload);
  const { key, uploadUrl } = upload.data;

  const stored = await fetch(uploadUrl, { method: "PUT", body: file, headers: { "Content-Type": file.type } });
  if (!stored.ok) redirect(`${productPath(productId)}?error=upload_failed`);

  const attached = await callApi((headers) =>
    api.POST("/api/v1/admin/products/{id}/images", {
      headers,
      params: { path: { id: productId } },
      body: { key, version: Number(formData.get("version")) },
    }),
  );
  backToProduct(productId, attached);
}

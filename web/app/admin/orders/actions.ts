"use server";

import { redirect } from "next/navigation";
import { api } from "@/lib/api/client";
import { callApi } from "@/lib/api/call-api";
import { errorCode } from "@/lib/api/problem";

export async function shipOrder(formData: FormData) {
  const orderId = String(formData.get("orderId") ?? "");
  const result = await callApi((headers) =>
    api.POST("/api/v1/admin/orders/{id}/ship", { headers, params: { path: { id: orderId } } }),
  );
  redirect(result.response.ok ? "/admin/orders" : `/admin/orders?error=${encodeURIComponent(errorCode(result))}`);
}

"use server";

import { redirect } from "next/navigation";
import { api } from "@/lib/api/client";
import { callApi } from "@/lib/api/call-api";
import { errorCode } from "@/lib/api/problem";

export async function cancelOrder(formData: FormData) {
  const orderId = String(formData.get("orderId") ?? "");
  const result = await callApi((headers) =>
    api.POST("/api/v1/orders/{id}/cancel", { headers, params: { path: { id: orderId } } }),
  );
  const query = result.response.ok ? "" : `?error=${encodeURIComponent(errorCode(result))}`;
  redirect(`/orders/${encodeURIComponent(orderId)}${query}`);
}

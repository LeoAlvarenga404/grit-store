"use server";

import { redirect } from "next/navigation";
import { api } from "@/lib/api/client";
import { callApi } from "@/lib/api/call-api";
import { errorCode } from "@/lib/api/problem";

export async function placeOrder(formData: FormData) {
  const result = await callApi((headers) =>
    api.POST("/api/v1/orders", {
      headers,
      params: { header: { "Idempotency-Key": String(formData.get("idempotencyKey") ?? "") } },
      body: {
        paymentToken: String(formData.get("paymentToken") ?? ""),
        expectedSubtotalAmount: Number(formData.get("expectedSubtotalAmount")),
      },
    }),
  );
  if (!result.data) redirect(`/checkout?error=${encodeURIComponent(errorCode(result))}`);
  redirect(`/orders/${result.data.orderId}`);
}

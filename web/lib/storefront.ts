import { cookies } from "next/headers";
import { api } from "@/lib/api/client";
import { accessTokenCookie, refreshTokenCookie } from "@/lib/session/session";

export async function getVisitor() {
  const cookieStore = await cookies();
  const accessToken = cookieStore.get(accessTokenCookie)?.value;
  const isSignedIn = Boolean(cookieStore.get(refreshTokenCookie)?.value);
  if (!accessToken) return { isSignedIn, basketItemCount: 0 };

  // Best effort: the header must never redirect or fail because the basket is slow or the token just expired.
  const { data } = await api.GET("/api/v1/basket", { headers: { Authorization: `Bearer ${accessToken}` } }).catch(() => ({ data: undefined }));
  const basketItemCount = data?.lines.reduce((total, line) => total + line.quantity, 0) ?? 0;
  return { isSignedIn, basketItemCount };
}

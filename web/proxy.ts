import { NextResponse, type NextRequest } from "next/server";
import { refreshTokens } from "@/lib/session/refresh";
import { accessTokenCookie, clearSession, refreshTokenCookie, writeSession } from "@/lib/session/session";

// Server Components cannot set cookies, so an expired access token is renewed here, before the page renders;
// Next.js hands the cookies set on this response to cookies() in the same request.
export async function proxy(request: NextRequest) {
  const response = NextResponse.next();
  const refreshToken = request.cookies.get(refreshTokenCookie)?.value;
  if (request.cookies.get(accessTokenCookie)?.value || !refreshToken) return response;

  const tokens = await refreshTokens(refreshToken);
  if (tokens) writeSession(response.cookies, tokens);
  else clearSession(response.cookies);
  return response;
}

export const config = {
  matcher: ["/((?!_next/static|_next/image|favicon.ico).*)"],
};

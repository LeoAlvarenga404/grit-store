import { NextResponse, type NextRequest } from "next/server";
import { refreshTokens } from "@/lib/session/refresh";
import { currentPathHeader } from "@/lib/session/return-path";
import { accessTokenCookie, clearSession, refreshTokenCookie, writeSession } from "@/lib/session/session";

// Server Components cannot set cookies, so an expired access token is renewed here, before the page renders;
// Next.js hands the cookies set on this response to cookies() in the same request.
export async function proxy(request: NextRequest) {
  // callApi reads this to send a signed-out visitor to /login?next=<this page>.
  const requestHeaders = new Headers(request.headers);
  requestHeaders.set(currentPathHeader, request.nextUrl.pathname + request.nextUrl.search);
  const response = NextResponse.next({ request: { headers: requestHeaders } });
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

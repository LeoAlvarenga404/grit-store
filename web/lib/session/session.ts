import type { components } from "@/lib/api/schema";

export type Tokens = components["schemas"]["TokenResponse"];

export const accessTokenCookie = "grit_access";
export const refreshTokenCookie = "grit_refresh";

// The access cookie expires a minute before the token, so the proxy renews it before the API could answer 401.
const accessTokenMarginInMilliseconds = 60_000;

type CookieOptions = { httpOnly: true; secure: boolean; sameSite: "lax"; path: "/"; expires: Date };

type CookieWriter = {
  set(name: string, value: string, options: CookieOptions): unknown;
  delete(name: string): unknown;
};

function cookieOptions(expires: Date): CookieOptions {
  return { httpOnly: true, secure: process.env.NODE_ENV === "production", sameSite: "lax", path: "/", expires };
}

export function writeSession(cookies: CookieWriter, tokens: Tokens) {
  const accessTokenExpiresAt = Date.parse(tokens.accessTokenExpiresAt) - accessTokenMarginInMilliseconds;
  cookies.set(accessTokenCookie, tokens.accessToken, cookieOptions(new Date(accessTokenExpiresAt)));
  cookies.set(refreshTokenCookie, tokens.refreshToken, cookieOptions(new Date(tokens.refreshTokenExpiresAt)));
}

export function clearSession(cookies: CookieWriter) {
  cookies.delete(accessTokenCookie);
  cookies.delete(refreshTokenCookie);
}

import { cookies, headers } from "next/headers";
import { redirect } from "next/navigation";
import { refreshTokens } from "@/lib/session/refresh";
import { currentPathHeader, loginHref } from "@/lib/session/return-path";
import { accessTokenCookie, clearSession, refreshTokenCookie, writeSession } from "@/lib/session/session";

type CookieStore = Awaited<ReturnType<typeof cookies>>;
type AuthorizationHeader = { Authorization: string };

// Calls the API as the signed-in customer: on 401 it refreshes once and retries; without a session it sends the user to /login.
export async function callApi<Result extends { response: Response }>(
  call: (headers: AuthorizationHeader) => Promise<Result>,
): Promise<Result> {
  const cookieStore = await cookies();
  const accessToken = cookieStore.get(accessTokenCookie)?.value || (await renewSession(cookieStore));
  if (!accessToken) redirect(await signInPath());

  const result = await call({ Authorization: `Bearer ${accessToken}` });
  if (result.response.status !== 401) return result;

  const renewedAccessToken = await renewSession(cookieStore);
  if (!renewedAccessToken) redirect(await signInPath());
  return call({ Authorization: `Bearer ${renewedAccessToken}` });
}

async function signInPath(): Promise<string> {
  return loginHref((await headers()).get(currentPathHeader));
}

async function renewSession(cookieStore: CookieStore): Promise<string | null> {
  const refreshToken = cookieStore.get(refreshTokenCookie)?.value;
  if (!refreshToken) return null;

  const tokens = await refreshTokens(refreshToken);
  try {
    if (tokens) writeSession(cookieStore, tokens);
    else clearSession(cookieStore);
  } catch {
    // A Server Component cannot set cookies. Within the refresh reuse window the proxy gets these same tokens
    // on the next request; after it the old cookie counts as reuse and the user signs in again.
  }
  return tokens?.accessToken ?? null;
}

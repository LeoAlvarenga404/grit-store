// The login form carries the page the visitor came from; anything but a path inside this site goes home,
// so the redirect after login can never lead to another host.
export function safeReturnPath(value: unknown): string {
  if (typeof value !== "string" || value.length > 300) return "/";
  if (!value.startsWith("/") || value.startsWith("//") || value.includes("\\")) return "/";
  return value;
}

export function loginHref(returnPath: string | null): string {
  const path = safeReturnPath(returnPath);
  const isAuthPage = path === "/login" || path.startsWith("/login?") || path === "/register" || path.startsWith("/register?");
  return path === "/" || isAuthPage ? "/login" : `/login?next=${encodeURIComponent(path)}`;
}

export const currentPathHeader = "x-grit-path";

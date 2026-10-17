// Read loosely: some callers pass a result whose error type is unknown, and framework errors (malformed JSON,
// unknown route, rate limit) carry no `code` (ADR 0006).
export function errorCode(result: { error?: unknown; response: Response }): string {
  const code = (result.error as { code?: unknown } | undefined)?.code;
  return typeof code === "string" ? code : `http_${result.response.status}`;
}

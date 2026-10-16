// The generated ProblemDetails type has no `code` yet (pending P10), but every API error carries one (ADR 0006).
export function errorCode(result: { error?: unknown; response: Response }): string {
  const code = (result.error as { code?: unknown } | undefined)?.code;
  return typeof code === "string" ? code : `http_${result.response.status}`;
}

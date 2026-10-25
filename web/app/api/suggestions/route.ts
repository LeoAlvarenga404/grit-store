import { getSuggestions } from "@/lib/api/catalog-reads";

// The browser cannot reach the API (its URL is server-only), so the search box asks for suggestions here.
export async function GET(request: Request) {
  const q = new URL(request.url).searchParams.get("q") ?? "";
  if (q.trim().length < 2) return Response.json([]);

  const { data } = await getSuggestions(q);
  return Response.json(data ?? []);
}

import { api } from "@/lib/api/client";

// Rendered per request: the build must not need a running API.
export const dynamic = "force-dynamic";

async function readApiStatus(): Promise<string> {
  try {
    const { data, error } = await api.GET("/health");
    return data?.status ?? error?.status ?? "Unknown";
  } catch {
    return "Unreachable";
  }
}

export default async function HomePage() {
  const status = await readApiStatus();

  return (
    <main>
      <h1>Grit Store</h1>
      <p>API: {status}</p>
    </main>
  );
}

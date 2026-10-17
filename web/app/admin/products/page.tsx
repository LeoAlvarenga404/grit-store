import Link from "next/link";
import { api } from "@/lib/api/client";
import { callApi } from "@/lib/api/call-api";

export default async function AdminProductsPage({ searchParams }: { searchParams: Promise<{ page?: string }> }) {
  const page = Math.max(1, Math.trunc(Number((await searchParams).page)) || 1);
  const { data, response } = await callApi((headers) =>
    api.GET("/api/v1/admin/products", { headers, params: { query: { page } } }),
  );
  if (!data) throw new Error(`The admin products answered ${response.status}.`);

  return (
    <main className="legacy">
      <h1>Produtos</h1>
      <table>
        <thead>
          <tr>
            <th scope="col">Produto</th>
            <th scope="col">Endereço</th>
            <th scope="col">Variantes</th>
          </tr>
        </thead>
        <tbody>
          {data.items.map((product) => (
            <tr key={product.id}>
              <td>
                <Link href={`/admin/products/${product.id}`}>{product.name}</Link>
              </td>
              <td>/products/{product.slug}</td>
              <td>{product.skuCount === 0 ? "Nenhuma (fora da loja)" : product.skuCount}</td>
            </tr>
          ))}
        </tbody>
      </table>
      <nav aria-label="Páginas" className="admin-nav">
        {page > 1 && <Link href={`/admin/products?page=${page - 1}`}>Anterior</Link>}
        {data.hasNextPage && <Link href={`/admin/products?page=${page + 1}`}>Próxima</Link>}
      </nav>
    </main>
  );
}

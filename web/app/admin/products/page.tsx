import type { Metadata } from "next";
import Link from "next/link";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { api } from "@/lib/api/client";
import { callApi } from "@/lib/api/call-api";

export const metadata: Metadata = { title: "Produtos" };

export default async function AdminProductsPage({ searchParams }: { searchParams: Promise<{ page?: string }> }) {
  const page = Math.max(1, Math.trunc(Number((await searchParams).page)) || 1);
  const { data, response } = await callApi((headers) =>
    api.GET("/api/v1/admin/products", { headers, params: { query: { page } } }),
  );
  if (!data) throw new Error(`The admin products answered ${response.status}.`);

  return (
    <>
      <h1 className="text-2xl font-semibold tracking-tight">Produtos</h1>
      <div className="mt-6 rounded-md border bg-background">
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead>Produto</TableHead>
              <TableHead>Endereço na loja</TableHead>
              <TableHead className="text-right">Variantes</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {data.items.map((product) => (
              <TableRow key={product.id}>
                <TableCell className="font-medium">
                  <Link href={`/admin/products/${product.id}`} className="hover:underline">{product.name}</Link>
                </TableCell>
                <TableCell className="font-mono text-xs text-muted-foreground">/products/{product.slug}</TableCell>
                <TableCell className="text-right">
                  {product.skuCount === 0 ? <Badge variant="outline">Fora da loja</Badge> : <span className="tabular-nums">{product.skuCount}</span>}
                </TableCell>
              </TableRow>
            ))}
            {data.items.length === 0 && (
              <TableRow>
                <TableCell colSpan={3} className="py-10 text-center text-muted-foreground">Nenhum produto.</TableCell>
              </TableRow>
            )}
          </TableBody>
        </Table>
      </div>
      <nav aria-label="Páginas" className="mt-4 flex items-center justify-between text-sm">
        <span className="text-muted-foreground">Página {page}</span>
        <div className="flex gap-2">
          {page > 1 && <Button asChild variant="outline" size="sm"><Link href={`/admin/products?page=${page - 1}`}>Anterior</Link></Button>}
          {data.hasNextPage && <Button asChild variant="outline" size="sm"><Link href={`/admin/products?page=${page + 1}`}>Próxima</Link></Button>}
        </div>
      </nav>
    </>
  );
}

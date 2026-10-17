import Link from "next/link";
import { notFound } from "next/navigation";
import type { ReactNode } from "react";
import { api } from "@/lib/api/client";
import { callApi } from "@/lib/api/call-api";

// UX only: the API answers 403 to anyone without the admin role, whatever this layout renders.
export default async function AdminLayout({ children }: Readonly<{ children: ReactNode }>) {
  const { data: me } = await callApi((headers) => api.GET("/api/v1/identity/me", { headers }));
  if (!me?.roles.includes("admin")) notFound();

  return (
    <>
      <nav aria-label="Administração" className="admin-nav">
        <Link href="/admin/products">Produtos</Link>
        <Link href="/admin/orders">Pedidos</Link>
      </nav>
      {children}
    </>
  );
}

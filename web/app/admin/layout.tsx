import type { Metadata } from "next";
import Link from "next/link";
import { notFound } from "next/navigation";
import type { ReactNode } from "react";
import { Logo } from "@/components/logo";
import { Badge } from "@/components/ui/badge";
import { api } from "@/lib/api/client";
import { callApi } from "@/lib/api/call-api";
import { logOut } from "@/lib/session/actions";
import { AdminNav } from "./admin-nav";

export const metadata: Metadata = { title: { default: "Administração", template: "%s · Administração" } };

// UX only: the API answers 403 to anyone without the admin role, whatever this layout renders.
export default async function AdminLayout({ children }: Readonly<{ children: ReactNode }>) {
  const { data: me } = await callApi((headers) => api.GET("/api/v1/identity/me", { headers }));
  if (!me?.roles.includes("admin")) notFound();

  return (
    <div className="flex flex-1 flex-col bg-muted/50">
      <header className="border-b bg-background">
        <div className="mx-auto flex h-14 max-w-screen-xl items-center gap-8 px-4 lg:px-8">
          <Link href="/admin" className="flex items-center gap-2">
            <Logo className="h-5" />
            <Badge variant="outline" className="rounded-sm">Admin</Badge>
          </Link>
          <AdminNav />
          <div className="ml-auto flex items-center gap-4 text-sm">
            <span className="hidden text-muted-foreground md:inline">{me.email}</span>
            <Link href="/" className="hover:underline">Ver loja</Link>
            <form action={logOut}><button type="submit" className="hover:underline">Sair</button></form>
          </div>
        </div>
      </header>
      <main className="mx-auto w-full max-w-screen-xl flex-1 px-4 py-8 lg:px-8">{children}</main>
    </div>
  );
}

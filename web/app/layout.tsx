import type { Metadata } from "next";
import { cookies } from "next/headers";
import Link from "next/link";
import type { ReactNode } from "react";
import { logOut } from "@/lib/session/actions";
import { refreshTokenCookie } from "@/lib/session/session";
import "./globals.css";

export const metadata: Metadata = {
  title: "Grit Store",
  description: "Esportes e outdoor",
};

export default async function RootLayout({ children }: Readonly<{ children: ReactNode }>) {
  const isSignedIn = Boolean((await cookies()).get(refreshTokenCookie)?.value);

  return (
    <html lang="pt-BR">
      <body>
        <header className="site-header">
          <nav aria-label="Principal">
            <Link href="/" className="brand">
              Grit Store
            </Link>
            <Link href="/basket">Carrinho</Link>
            <Link href="/orders">Pedidos</Link>
            {isSignedIn ? (
              <form action={logOut}>
                <button type="submit" className="link-button">
                  Sair
                </button>
              </form>
            ) : (
              <>
                <Link href="/login">Entrar</Link>
                <Link href="/register">Criar conta</Link>
              </>
            )}
          </nav>
        </header>
        {children}
      </body>
    </html>
  );
}

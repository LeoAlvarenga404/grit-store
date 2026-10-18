import Link from "next/link";
import type { ReactNode } from "react";
import { storeCategories } from "@/lib/store-navigation";

const footerLink = "inline-flex min-h-9 items-center text-sm text-background/70 hover:text-background";

function FooterColumn({ title, children }: { title: string; children: ReactNode }) {
  return (
    <div>
      <h2 className="text-eyebrow mb-3">{title}</h2>
      <ul>{children}</ul>
    </div>
  );
}

export function SiteFooter() {
  return (
    <footer className="bg-foreground text-background">
      <div className="mx-auto grid max-w-screen-2xl gap-10 px-4 py-14 sm:grid-cols-2 lg:grid-cols-[2fr_1fr_1fr_1fr] lg:px-8">
        <div>
          <p className="text-display text-4xl">Grit</p>
          <p className="mt-4 max-w-xs text-sm text-background/70">Roupas e tênis esportivos para quem treina de verdade.</p>
        </div>
        <FooterColumn title="Loja">
          {storeCategories.map((category) => (
            <li key={category.slug}>
              <Link href={`/catalog?category=${category.slug}`} className={footerLink}>
                {category.name}
              </Link>
            </li>
          ))}
        </FooterColumn>
        <FooterColumn title="Conta">
          <li>
            <Link href="/orders" className={footerLink}>
              Meus pedidos
            </Link>
          </li>
          <li>
            <Link href="/basket" className={footerLink}>
              Sacola
            </Link>
          </li>
          <li>
            <Link href="/login" className={footerLink}>
              Entrar
            </Link>
          </li>
        </FooterColumn>
        <FooterColumn title="Projeto">
          <li>
            <a href="https://github.com/LeoAlvarenga404/grit-store" className={footerLink}>
              Código no GitHub
            </a>
          </li>
        </FooterColumn>
      </div>
      <p className="border-t border-background/15 px-4 py-5 text-center text-xs text-background/60">
        Grit Store é um projeto de portfólio: nenhum pedido é real e os pagamentos rodam em modo de teste.
      </p>
    </footer>
  );
}

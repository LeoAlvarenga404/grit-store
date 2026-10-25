import { ArrowUp } from "lucide-react";
import Link from "next/link";
import type { ReactNode } from "react";
import { storeCategories } from "@/lib/store-navigation";

const footerLink = "inline-flex min-h-9 items-center text-sm text-background/70 underline-offset-4 transition-colors hover:text-background hover:underline";

function FooterColumn({ title, children }: { title: string; children: ReactNode }) {
  return (
    <div>
      <h2 className="text-eyebrow mb-4 text-background/50">{title}</h2>
      <ul>{children}</ul>
    </div>
  );
}

export function SiteFooter() {
  return (
    <footer className="relative overflow-hidden bg-foreground text-background">
      <div className="mx-auto max-w-screen-2xl px-4 pt-16 lg:px-8">
        <div className="grid gap-12 sm:grid-cols-2 lg:grid-cols-[2fr_1fr_1fr_1fr]">
          <div>
            <p className="text-display text-4xl">Grit</p>
            <p className="mt-4 max-w-xs text-sm leading-relaxed text-background/70">Roupas e tênis esportivos para quem treina de verdade.</p>
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
        <div className="mt-16 flex flex-col gap-4 border-t border-background/15 py-6 text-xs text-background/60 sm:flex-row sm:items-center sm:justify-between">
          <p>Grit Store é um projeto de portfólio: nenhum pedido é real e os pagamentos rodam em modo de teste.</p>
          <a href="#top" className="inline-flex items-center gap-2 font-semibold tracking-[0.08em] text-background uppercase hover:underline">
            Voltar ao topo <ArrowUp aria-hidden="true" className="size-4" />
          </a>
        </div>
      </div>
      <p aria-hidden="true" className="text-display pointer-events-none -mt-2 text-center text-[23vw] leading-[0.8] text-background/[0.06] select-none">
        Grit
      </p>
    </footer>
  );
}

import { User } from "lucide-react";
import Link from "next/link";
import { BasketLink } from "@/components/basket-count";
import { CategoryMenu, navLinkClass } from "@/components/category-menu";
import { FavoritesLink } from "@/components/favorites-link";
import { HeaderShell } from "@/components/header-shell";
import { MobileMenu } from "@/components/mobile-menu";
import { SearchField } from "@/components/search-field";
import { Wordmark } from "@/components/wordmark";
import { logOut } from "@/lib/session/actions";
import { menuShortcuts } from "@/lib/store-navigation";

const iconLink = "relative inline-flex size-10 items-center justify-center rounded-full transition-colors hover:bg-muted";

export function SiteHeader({ isSignedIn }: { isSignedIn: boolean }) {
  return (
    <HeaderShell>
      <div className="mx-auto flex h-16 max-w-screen-2xl items-center gap-4 px-4 transition-[height] duration-300 group-data-[scrolled=true]/header:h-14 lg:gap-10 lg:px-8">
        <MobileMenu />
        <Wordmark />
        <nav aria-label="Principal" className="hidden h-full items-center gap-8 lg:flex">
          {menuShortcuts.map((item) => (
            <Link key={item.href} href={item.href} className={navLinkClass}>
              {item.label}
            </Link>
          ))}
          <CategoryMenu />
        </nav>
        <SearchField className="ml-auto hidden w-full max-w-xs md:block" />
        <div className="ml-auto flex items-center gap-1 md:ml-0">
          {isSignedIn ? (
            <>
              <Link href="/orders" aria-label="Meus pedidos" className={iconLink}>
                <User className="size-5" />
              </Link>
              <form action={logOut}>
                <button type="submit" className="px-2 text-sm font-medium underline-offset-4 hover:underline">
                  Sair
                </button>
              </form>
            </>
          ) : (
            <Link href="/login" aria-label="Entrar" className={iconLink}>
              <User className="size-5" />
            </Link>
          )}
          <FavoritesLink className={iconLink} />
          <BasketLink className={iconLink} />
        </div>
      </div>
    </HeaderShell>
  );
}

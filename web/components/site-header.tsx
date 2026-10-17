import { ChevronDown, ShoppingBag, User } from "lucide-react";
import Link from "next/link";
import { MobileMenu } from "@/components/mobile-menu";
import { SearchField } from "@/components/search-field";
import { Wordmark } from "@/components/wordmark";
import { logOut } from "@/lib/session/actions";
import { menuShortcuts, storeCategories } from "@/lib/store-navigation";
import { getVisitor } from "@/lib/storefront";

const navLink = "inline-flex h-16 items-center border-b-2 border-transparent text-[13px] font-semibold tracking-[0.08em] uppercase hover:border-foreground";
const iconLink = "relative inline-flex size-10 items-center justify-center rounded-full hover:bg-muted";

export async function SiteHeader() {
  const visitor = await getVisitor();

  return (
    <header className="sticky top-0 z-40 border-b bg-background/95 backdrop-blur">
      <div className="mx-auto flex h-16 max-w-screen-2xl items-center gap-4 px-4 lg:gap-8 lg:px-8">
        <MobileMenu />
        <Wordmark />
        <nav aria-label="Principal" className="hidden h-full items-center gap-7 lg:flex">
          {menuShortcuts.map((item) => (
            <Link key={item.href} href={item.href} className={navLink}>
              {item.label}
            </Link>
          ))}
          {/* Hover or keyboard focus opens the panel; every link inside stays reachable with Tab. */}
          <div className="group h-full">
            <button type="button" className={`${navLink} gap-1`} aria-haspopup="true">
              Categorias <ChevronDown aria-hidden="true" className="size-4" />
            </button>
            <div className="invisible absolute inset-x-0 top-full border-b bg-background opacity-0 transition-opacity group-focus-within:visible group-focus-within:opacity-100 group-hover:visible group-hover:opacity-100">
              <ul className="mx-auto grid max-w-screen-2xl grid-cols-3 gap-x-8 gap-y-1 px-8 py-8">
                {storeCategories.map((category) => (
                  <li key={category.slug}>
                    <Link href={`/catalog?category=${category.slug}`} className="block py-2 text-sm hover:underline">
                      {category.name}
                    </Link>
                  </li>
                ))}
              </ul>
            </div>
          </div>
        </nav>
        <SearchField className="ml-auto hidden w-full max-w-xs md:block" />
        <div className="ml-auto flex items-center gap-1 md:ml-0">
          {visitor.isSignedIn ? (
            <form action={logOut}>
              <button type="submit" className="px-2 text-sm font-medium hover:underline">
                Sair
              </button>
            </form>
          ) : (
            <Link href="/login" aria-label="Entrar" className={iconLink}>
              <User className="size-5" />
            </Link>
          )}
          <Link
            href="/basket"
            aria-label={`Sacola, ${visitor.basketItemCount} ${visitor.basketItemCount === 1 ? "item" : "itens"}`}
            className={iconLink}
          >
            <ShoppingBag className="size-5" />
            {visitor.basketItemCount > 0 && (
              <span className="absolute top-0.5 right-0.5 flex min-w-4 items-center justify-center rounded-full bg-foreground px-1 text-[10px] leading-4 font-semibold text-background tabular-nums">
                {visitor.basketItemCount}
              </span>
            )}
          </Link>
        </div>
      </div>
    </header>
  );
}

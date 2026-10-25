import { ArrowRight, ChevronDown } from "lucide-react";
import Link from "next/link";
import { BannerImage } from "@/components/banner-image";
import { categoryCardImage } from "@/lib/banners";
import { storeCategories } from "@/lib/store-navigation";

export const navLinkClass =
  "relative inline-flex h-full items-center gap-1 text-[13px] font-semibold tracking-[0.08em] uppercase after:absolute after:inset-x-0 after:bottom-0 after:h-0.5 after:origin-left after:scale-x-0 after:bg-foreground after:transition-transform after:duration-300 hover:after:scale-x-100 focus-visible:after:scale-x-100";

// Hover or keyboard focus opens the panel; every link inside stays reachable with Tab.
export function CategoryMenu() {
  return (
    <div className="group/menu h-full">
      <button type="button" className={`${navLinkClass} group-hover/menu:after:scale-x-100 group-focus-within/menu:after:scale-x-100`} aria-haspopup="true">
        Categorias <ChevronDown aria-hidden="true" className="size-4 transition-transform duration-300 group-hover/menu:rotate-180 group-focus-within/menu:rotate-180" />
      </button>
      <div className="invisible absolute inset-x-0 top-full -translate-y-2 border-b bg-background opacity-0 shadow-xl transition-[opacity,transform,visibility] duration-200 group-focus-within/menu:visible group-focus-within/menu:translate-y-0 group-focus-within/menu:opacity-100 group-hover/menu:visible group-hover/menu:translate-y-0 group-hover/menu:opacity-100">
        <ul className="mx-auto grid max-w-screen-2xl grid-cols-6 gap-4 px-8 py-8">
          {storeCategories.map((category) => (
            <li key={category.slug}>
              <Link href={`/catalog?category=${category.slug}`} className="group/item block">
                <div className="relative overflow-hidden bg-muted">
                  <BannerImage
                    file={categoryCardImage(category.slug)}
                    alt=""
                    widthInPixels={800}
                    heightInPixels={1000}
                    sizes="200px"
                    className="aspect-[4/3] w-full transition-transform duration-500 ease-out group-hover/item:scale-110"
                  />
                </div>
                <span className="mt-3 flex items-center justify-between gap-2 text-sm font-semibold">
                  {category.name}
                  <ArrowRight aria-hidden="true" className="size-4 -translate-x-2 opacity-0 transition-all duration-300 group-hover/item:translate-x-0 group-hover/item:opacity-100" />
                </span>
              </Link>
            </li>
          ))}
        </ul>
        <div aria-hidden="true" className="pointer-events-none absolute inset-x-0 top-full h-screen bg-black/25" />
      </div>
    </div>
  );
}

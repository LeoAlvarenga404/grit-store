import { ArrowRight } from "lucide-react";
import Link from "next/link";
import { BannerImage } from "@/components/banner-image";
import { SectionHeading } from "@/components/home/section-heading";
import { categoryCardImage } from "@/lib/banners";
import { storeCategories } from "@/lib/store-navigation";

export function CategoryGrid() {
  return (
    <section aria-labelledby="categories-title" className="mx-auto max-w-screen-2xl px-4 py-14 lg:px-8">
      <SectionHeading id="categories-title" title="Compre por categoria" />
      <ul className="grid grid-cols-2 gap-3 md:grid-cols-3 xl:grid-cols-6">
        {storeCategories.map((category) => (
          <li key={category.slug} className="reveal">
            <Link href={`/catalog?category=${category.slug}`} className="group relative block overflow-hidden">
              <BannerImage
                file={categoryCardImage(category.slug)}
                alt=""
                widthInPixels={800}
                heightInPixels={1000}
                sizes="(min-width: 1280px) 16vw, (min-width: 768px) 33vw, 50vw"
                className="aspect-[4/5] w-full transition-transform duration-700 ease-out group-hover:scale-110"
              />
              <div className="absolute inset-0 bg-gradient-to-t from-black/60 via-transparent to-transparent" />
              <div className="absolute inset-x-0 bottom-0 p-4 text-white">
                <h3 className="text-display text-lg md:text-xl">{category.name}</h3>
                <span className="mt-2 inline-flex items-center gap-1 text-xs font-semibold tracking-[0.08em] uppercase">Comprar <ArrowRight aria-hidden="true" className="size-3.5 -translate-x-1 opacity-0 transition-all duration-300 group-hover:translate-x-0 group-hover:opacity-100" /></span>
              </div>
            </Link>
          </li>
        ))}
      </ul>
    </section>
  );
}

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
          <li key={category.slug}>
            <Link href={`/catalog?category=${category.slug}`} className="group relative block overflow-hidden">
              <BannerImage
                file={categoryCardImage(category.slug)}
                alt=""
                widthInPixels={800}
                heightInPixels={1000}
                sizes="(min-width: 1280px) 16vw, (min-width: 768px) 33vw, 50vw"
                className="aspect-[4/5] w-full transition-transform duration-500 group-hover:scale-105"
              />
              <div className="absolute inset-0 bg-gradient-to-t from-black/60 via-transparent to-transparent" />
              <div className="absolute inset-x-0 bottom-0 p-4 text-white">
                <h3 className="text-display text-lg md:text-xl">{category.name}</h3>
                <span className="mt-2 inline-block text-xs font-semibold tracking-[0.08em] uppercase underline underline-offset-4">Comprar</span>
              </div>
            </Link>
          </li>
        ))}
      </ul>
    </section>
  );
}

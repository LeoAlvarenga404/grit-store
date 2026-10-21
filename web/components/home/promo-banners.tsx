import Link from "next/link";
import { BannerImage } from "@/components/banner-image";
import { Button } from "@/components/ui/button";
import { promoBanners } from "@/lib/banners";

export function PromoBanners() {
  return (
    <section aria-label="Coleções" className="mx-auto grid max-w-screen-2xl gap-3 px-4 py-16 md:grid-cols-2 lg:px-8 lg:py-24">
      {promoBanners.map((banner) => (
        <div key={banner.desktopImage} className="group reveal relative overflow-hidden">
          <BannerImage
            file={banner.desktopImage}
            alt={banner.alt}
            widthInPixels={banner.widthInPixels}
            heightInPixels={banner.heightInPixels}
            sizes="(min-width: 768px) 50vw, 100vw"
            className="aspect-[2/1] w-full transition-transform duration-700 ease-out group-hover:scale-105"
          />
          <div className="absolute inset-0 bg-gradient-to-r from-black/55 to-transparent" />
          <div className="absolute inset-y-0 left-0 flex flex-col justify-center p-6 text-white md:p-10">
            {banner.eyebrow && <p className="text-eyebrow mb-2">{banner.eyebrow}</p>}
            <h2 className="text-display text-2xl md:text-4xl">{banner.title}</h2>
            <Button asChild variant="secondary" className="mt-5 self-start">
              <Link href={banner.href}>{banner.cta}</Link>
            </Button>
          </div>
        </div>
      ))}
    </section>
  );
}

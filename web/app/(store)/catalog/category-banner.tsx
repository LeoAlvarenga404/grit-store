import { BannerImage } from "@/components/banner-image";
import { catalogBanner } from "@/lib/banners";

export function CategoryBanner({ slug, name }: { slug: string; name: string }) {
  const banner = catalogBanner(slug);

  return (
    <section className="relative">
      <BannerImage
        file={banner.desktopImage}
        alt=""
        widthInPixels={1920}
        heightInPixels={400}
        sizes="100vw"
        isPriority
        className="hidden aspect-[24/5] w-full md:flex"
      />
      <BannerImage
        file={banner.mobileImage}
        alt=""
        widthInPixels={1080}
        heightInPixels={540}
        sizes="100vw"
        isPriority
        className="flex aspect-[2/1] w-full md:hidden"
      />
      <div className="absolute inset-0 bg-gradient-to-r from-black/55 to-transparent" />
      <div className="absolute inset-0 mx-auto flex max-w-screen-2xl items-center px-4 lg:px-8">
        <h1 className="text-display text-3xl text-white md:text-5xl">{name}</h1>
      </div>
    </section>
  );
}

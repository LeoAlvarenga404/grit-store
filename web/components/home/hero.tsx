import Link from "next/link";
import { BannerImage } from "@/components/banner-image";
import { HeroCarousel } from "@/components/home/hero-carousel";
import { Button } from "@/components/ui/button";
import { heroBanners, type Banner } from "@/lib/banners";

function HeroSlide({ banner, isFirst }: { banner: Banner; isFirst: boolean }) {
  return (
    <div className="relative">
      <div className="hero-media overflow-hidden">
        <BannerImage
          file={banner.desktopImage}
          alt={banner.alt}
          widthInPixels={banner.widthInPixels}
          heightInPixels={banner.heightInPixels}
          sizes="100vw"
          isPriority={isFirst}
          className="hidden aspect-[12/5] w-full md:flex"
        />
        <BannerImage
          file={banner.mobileImage ?? banner.desktopImage}
          alt={banner.alt}
          widthInPixels={1080}
          heightInPixels={1350}
          sizes="100vw"
          isPriority={isFirst}
          className="flex aspect-[4/5] w-full md:hidden"
        />
      </div>
      <div className="absolute inset-0 bg-gradient-to-t from-black/60 via-black/10 to-transparent md:bg-gradient-to-r md:from-black/55 md:via-black/15" />
      <div className="hero-text absolute inset-x-0 bottom-0 mx-auto max-w-screen-2xl px-6 pb-14 text-white md:inset-y-0 md:flex md:flex-col md:justify-center md:px-12 md:pb-0">
        {banner.eyebrow && <p className="text-eyebrow mb-3">{banner.eyebrow}</p>}
        <h2 className="text-display max-w-xl text-4xl md:text-7xl">{banner.title}</h2>
        {banner.subtitle && <p className="mt-4 max-w-md text-sm text-white/85 md:text-base">{banner.subtitle}</p>}
        <Button asChild variant="secondary" size="lg" className="mt-6 self-start">
          <Link href={banner.href}>{banner.cta}</Link>
        </Button>
      </div>
    </div>
  );
}

export function Hero() {
  return (
    <HeroCarousel
      labels={heroBanners.map((banner) => banner.title)}
      slides={heroBanners.map((banner, index) => (
        <HeroSlide key={banner.desktopImage} banner={banner} isFirst={index === 0} />
      ))}
    />
  );
}

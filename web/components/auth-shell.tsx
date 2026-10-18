import type { ReactNode } from "react";
import { BannerImage } from "@/components/banner-image";
import { authBanner } from "@/lib/banners";

export function AuthShell({ title, subtitle, children }: { title: string; subtitle: string; children: ReactNode }) {
  return (
    <main className="mx-auto grid max-w-screen-2xl lg:grid-cols-2">
      <div className="flex items-center justify-center px-4 py-16 lg:px-8">
        <div className="w-full max-w-sm">
          <h1 className="text-display text-3xl">{title}</h1>
          <p className="mt-2 text-sm text-muted-foreground">{subtitle}</p>
          <div className="mt-8">{children}</div>
        </div>
      </div>
      <BannerImage
        file={authBanner.file}
        alt={authBanner.alt}
        widthInPixels={authBanner.widthInPixels}
        heightInPixels={authBanner.heightInPixels}
        sizes="50vw"
        className="hidden h-full min-h-[40rem] w-full lg:flex"
      />
    </main>
  );
}

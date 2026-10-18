import { existsSync } from "node:fs";
import path from "node:path";
import Image from "next/image";
import { cn } from "@/lib/cn";

const bannersFolder = path.join(process.cwd(), "public", "banners");

// Until the AI-generated banners arrive, a grey block with the expected file name and size keeps every layout real.
export function BannerImage({
  file,
  alt,
  widthInPixels,
  heightInPixels,
  sizes,
  className,
  isPriority = false,
}: {
  file: string;
  alt: string;
  widthInPixels: number;
  heightInPixels: number;
  sizes: string;
  className?: string;
  isPriority?: boolean;
}) {
  if (!existsSync(path.join(bannersFolder, file))) {
    return (
      <div aria-hidden="true" className={cn("flex items-start justify-end bg-input p-3", className)}>
        <span className="font-mono text-[11px] text-muted-foreground">
          {file} · {widthInPixels}×{heightInPixels}
        </span>
      </div>
    );
  }

  return (
    <Image
      src={`/banners/${file}`}
      alt={alt}
      width={widthInPixels}
      height={heightInPixels}
      sizes={sizes}
      preload={isPriority}
      className={cn("object-cover", className)}
    />
  );
}

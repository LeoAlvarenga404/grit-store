"use client";

import { ZoomIn } from "lucide-react";
import Image from "next/image";
import { useState } from "react";
import { Dialog, DialogContent, DialogTitle } from "@/components/ui/dialog";
import { cn } from "@/lib/cn";

const photo = "object-contain mix-blend-multiply";

// The photo grows under the pointer: the transform origin follows the cursor.
function followCursor(event: React.MouseEvent<HTMLElement>) {
  const box = event.currentTarget.getBoundingClientRect();
  event.currentTarget.style.setProperty("--x", `${((event.clientX - box.left) / box.width) * 100}%`);
  event.currentTarget.style.setProperty("--y", `${((event.clientY - box.top) / box.height) * 100}%`);
}

// The page renders this with key = the selected colour, so another colour opens on its first photo.
export function ProductGallery({ images, productName }: { images: string[]; productName: string }) {
  const [current, setCurrent] = useState(0);
  const [isZoomOpen, setIsZoomOpen] = useState(false);

  if (images.length === 0) {
    return <div className="flex aspect-[4/5] items-center justify-center bg-muted text-sm text-muted-foreground">Sem foto</div>;
  }

  const altOf = (index: number) => `${productName}, foto ${index + 1} de ${images.length}`;

  function openZoom(index: number) {
    setCurrent(index);
    setIsZoomOpen(true);
  }

  return (
    <div className="lg:flex lg:gap-4">
      <ul aria-label="Fotos" className="hidden w-20 shrink-0 flex-col gap-3 lg:flex">
        {images.map((url, index) => (
          <li key={url}>
            <button
              type="button"
              aria-label={`Ver foto ${index + 1}`}
              aria-current={index === current}
              onClick={() => setCurrent(index)}
              className={cn(
                "relative block aspect-[4/5] w-full overflow-hidden rounded-sm border bg-muted",
                index === current ? "border-foreground" : "border-transparent hover:border-border",
              )}
            >
              <Image src={url} alt="" fill sizes="80px" className={cn(photo, "p-1")} unoptimized />
            </button>
          </li>
        ))}
      </ul>
      <button
        type="button"
        aria-label="Ampliar foto"
        onClick={() => openZoom(current)}
        onMouseMove={followCursor}
        className="group relative hidden aspect-[4/5] flex-1 cursor-zoom-in overflow-hidden bg-muted lg:block"
      >
        <Image
          src={images[current]}
          alt={altOf(current)}
          fill
          sizes="(min-width: 1024px) 45vw, 100vw"
          preload
          className={cn(photo, "p-8 transition-transform duration-300 ease-out [transform-origin:var(--x,50%)_var(--y,50%)] group-hover:scale-[1.6]")}
          unoptimized
        />
        <span className="absolute right-4 bottom-4 flex size-10 items-center justify-center rounded-full bg-background">
          <ZoomIn aria-hidden="true" className="size-5" />
        </span>
      </button>
      <MobileTrack images={images} altOf={altOf} onOpen={openZoom} />
      <Dialog open={isZoomOpen} onOpenChange={setIsZoomOpen}>
        <DialogContent className="max-w-[calc(100%-2rem)] p-0 sm:max-w-3xl" aria-describedby={undefined}>
          <DialogTitle className="sr-only">{productName}</DialogTitle>
          <div className="relative aspect-[4/5] max-h-[85vh] w-full bg-muted">
            <Image src={images[current]} alt={altOf(current)} fill sizes="90vw" className={photo} unoptimized />
          </div>
        </DialogContent>
      </Dialog>
    </div>
  );
}

// Native scroll-snap gives the swipe; the counter follows the scroll position.
function MobileTrack({ images, altOf, onOpen }: { images: string[]; altOf: (index: number) => string; onOpen: (index: number) => void }) {
  const [visible, setVisible] = useState(0);

  return (
    <div className="relative lg:hidden">
      <ul
        aria-label="Fotos"
        onScroll={(event) => setVisible(Math.round(event.currentTarget.scrollLeft / event.currentTarget.clientWidth))}
        className="flex snap-x snap-mandatory overflow-x-auto [scrollbar-width:none] [&::-webkit-scrollbar]:hidden"
      >
        {images.map((url, index) => (
          <li key={url} className="w-full shrink-0 snap-start">
            <button type="button" aria-label={`Ampliar foto ${index + 1}`} onClick={() => onOpen(index)} className="relative block aspect-[4/5] w-full bg-muted">
              <Image src={url} alt={altOf(index)} fill sizes="100vw" preload={index === 0} className={cn(photo, "p-6")} unoptimized />
            </button>
          </li>
        ))}
      </ul>
      {images.length > 1 && (
        <p className="absolute right-3 bottom-3 rounded-full bg-background/90 px-2.5 py-1 text-xs tabular-nums">
          {visible + 1} / {images.length}
        </p>
      )}
    </div>
  );
}

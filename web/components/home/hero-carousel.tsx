"use client";

import { ChevronLeft, ChevronRight } from "lucide-react";
import { useRef, useState, type ReactNode } from "react";
import { cn } from "@/lib/cn";

// Native scroll-snap does the sliding (swipe on touch for free); this only adds arrows, dots and the current index.
export function HeroCarousel({ slides, labels }: { slides: ReactNode[]; labels: string[] }) {
  const trackRef = useRef<HTMLDivElement>(null);
  const [current, setCurrent] = useState(0);

  function goTo(index: number) {
    const track = trackRef.current;
    if (!track) return;
    const target = (index + slides.length) % slides.length;
    track.scrollTo({ left: target * track.clientWidth, behavior: "smooth" });
  }

  function onScroll() {
    const track = trackRef.current;
    if (!track) return;
    setCurrent(Math.round(track.scrollLeft / track.clientWidth));
  }

  const arrow = "absolute top-1/2 z-10 hidden size-11 -translate-y-1/2 items-center justify-center rounded-full bg-background/90 hover:bg-background md:flex";

  return (
    <section aria-roledescription="carrossel" aria-label="Destaques" className="relative">
      <div
        ref={trackRef}
        onScroll={onScroll}
        className="flex snap-x snap-mandatory overflow-x-auto scroll-smooth [scrollbar-width:none] [&::-webkit-scrollbar]:hidden"
      >
        {slides.map((slide, index) => (
          <div key={labels[index]} data-active={index === current} aria-roledescription="slide" aria-label={`${index + 1} de ${slides.length}: ${labels[index]}`} className="w-full shrink-0 snap-start">
            {slide}
          </div>
        ))}
      </div>
      <button type="button" aria-label="Destaque anterior" onClick={() => goTo(current - 1)} className={cn(arrow, "left-4")}>
        <ChevronLeft className="size-5" />
      </button>
      <button type="button" aria-label="Próximo destaque" onClick={() => goTo(current + 1)} className={cn(arrow, "right-4")}>
        <ChevronRight className="size-5" />
      </button>
      <div className="absolute inset-x-0 bottom-4 flex justify-center gap-2">
        {labels.map((label, index) => (
          <button
            key={label}
            type="button"
            aria-label={`Ir para o destaque ${index + 1}`}
            aria-current={index === current}
            onClick={() => goTo(index)}
            className={cn("h-1 w-8 rounded-full bg-background/50 transition-colors", index === current && "bg-background")}
          />
        ))}
      </div>
    </section>
  );
}

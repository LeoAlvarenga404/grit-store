"use client";

import { ChevronLeft, ChevronRight } from "lucide-react";
import { useEffect, useRef, useState, type ReactNode } from "react";
import { cn } from "@/lib/cn";

type Position = { start: number; size: number };

// Native scroll-snap does the sliding; the scrollbar is replaced by arrows and a thin progress track.
export function ScrollRow({ children, label }: { children: ReactNode; label: string }) {
  const trackRef = useRef<HTMLUListElement>(null);
  const [position, setPosition] = useState<Position>({ start: 0, size: 100 });

  function measure() {
    const track = trackRef.current;
    if (!track || track.scrollWidth === 0) return;
    setPosition({ start: (track.scrollLeft / track.scrollWidth) * 100, size: (track.clientWidth / track.scrollWidth) * 100 });
  }

  useEffect(() => {
    const track = trackRef.current;
    if (!track) return;
    const observer = new ResizeObserver(measure);
    observer.observe(track);
    return () => observer.disconnect();
  }, []);

  function scrollBy(direction: 1 | -1) {
    const track = trackRef.current;
    track?.scrollBy({ left: direction * track.clientWidth * 0.8, behavior: "smooth" });
  }

  const isAtStart = position.start <= 0.5;
  const isAtEnd = position.start + position.size >= 99.5;
  const arrow =
    "absolute top-[34%] z-10 hidden size-12 -translate-y-1/2 items-center justify-center rounded-full bg-background shadow-lg ring-1 ring-black/5 transition-[opacity,transform,background-color] duration-200 hover:bg-foreground hover:text-background lg:flex";

  return (
    <div className="relative">
      <ul
        ref={trackRef}
        onScroll={measure}
        aria-label={label}
        className="flex snap-x snap-mandatory gap-3 overflow-x-auto scroll-smooth px-4 lg:px-0 [scrollbar-width:none] [&::-webkit-scrollbar]:hidden"
      >
        {children}
      </ul>
      <button
        type="button"
        aria-label="Ver anteriores"
        onClick={() => scrollBy(-1)}
        tabIndex={isAtStart ? -1 : 0}
        className={cn(arrow, "-left-6", isAtStart && "pointer-events-none scale-90 opacity-0")}
      >
        <ChevronLeft aria-hidden="true" className="size-5" />
      </button>
      <button
        type="button"
        aria-label="Ver próximos"
        onClick={() => scrollBy(1)}
        tabIndex={isAtEnd ? -1 : 0}
        className={cn(arrow, "-right-6", isAtEnd && "pointer-events-none scale-90 opacity-0")}
      >
        <ChevronRight aria-hidden="true" className="size-5" />
      </button>
      <div aria-hidden="true" className="mx-4 mt-6 h-0.5 overflow-hidden rounded-full bg-border lg:mx-0">
        <div className="h-full rounded-full bg-foreground transition-[margin,width] duration-150" style={{ marginLeft: `${position.start}%`, width: `${position.size}%` }} />
      </div>
    </div>
  );
}

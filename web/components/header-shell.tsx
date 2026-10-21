"use client";

import { useEffect, useState, type ReactNode } from "react";

// Only paints a shadow and tightens the bar after the page moves, so the first screen stays flat and calm.
export function HeaderShell({ children }: { children: ReactNode }) {
  const [isScrolled, setIsScrolled] = useState(false);

  useEffect(() => {
    const onScroll = () => setIsScrolled(window.scrollY > 8);
    window.addEventListener("scroll", onScroll, { passive: true });
    return () => window.removeEventListener("scroll", onScroll);
  }, []);

  return (
    <header
      data-scrolled={isScrolled}
      className="group/header sticky top-0 z-40 border-b bg-background/90 backdrop-blur-md transition-shadow duration-300 data-[scrolled=true]:shadow-[0_6px_24px_-12px_rgba(0,0,0,0.25)]"
    >
      {children}
    </header>
  );
}

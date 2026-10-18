"use client";

import { SlidersHorizontal } from "lucide-react";
import type { ReactNode } from "react";
import { Button } from "@/components/ui/button";
import { Sheet, SheetContent, SheetTitle, SheetTrigger } from "@/components/ui/sheet";

// Mobile only. The page renders it with key = the current URL, so following a filter link remounts it closed.
export function FiltersSheet({ activeCount, children }: { activeCount: number; children: ReactNode }) {
  return (
    <Sheet>
      <SheetTrigger asChild>
        <Button variant="outline" className="lg:hidden">
          <SlidersHorizontal aria-hidden="true" />
          Filtrar{activeCount > 0 && ` (${activeCount})`}
        </Button>
      </SheetTrigger>
      <SheetContent side="left" className="w-[88vw] max-w-sm gap-0 overflow-y-auto p-6">
        <SheetTitle className="text-display mb-6 text-xl">Filtros</SheetTitle>
        {children}
      </SheetContent>
    </Sheet>
  );
}

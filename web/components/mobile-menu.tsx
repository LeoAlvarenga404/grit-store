"use client";

import { Menu } from "lucide-react";
import Link from "next/link";
import { useState } from "react";
import { SearchField } from "@/components/search-field";
import { Button } from "@/components/ui/button";
import { Sheet, SheetClose, SheetContent, SheetTitle, SheetTrigger } from "@/components/ui/sheet";
import { menuShortcuts, storeCategories } from "@/lib/store-navigation";

const menuLink = "flex min-h-12 items-center border-b text-base font-medium";

export function MobileMenu() {
  const [isOpen, setIsOpen] = useState(false);

  return (
    <Sheet open={isOpen} onOpenChange={setIsOpen}>
      <SheetTrigger asChild>
        <Button variant="ghost" size="icon" aria-label="Abrir menu" className="lg:hidden">
          <Menu className="size-5" />
        </Button>
      </SheetTrigger>
      <SheetContent side="left" className="w-[86vw] max-w-sm gap-0 p-6">
        <SheetTitle className="text-display mb-6 text-2xl">Grit</SheetTitle>
        <SearchField className="mb-6" onNavigate={() => setIsOpen(false)} />
        <nav aria-label="Menu">
          <ul>
            {menuShortcuts.map((item) => (
              <li key={item.href}>
                <SheetClose asChild>
                  <Link href={item.href} className={menuLink}>
                    {item.label}
                  </Link>
                </SheetClose>
              </li>
            ))}
          </ul>
          <p className="text-eyebrow mt-8 mb-2 text-muted-foreground">Categorias</p>
          <ul>
            {storeCategories.map((category) => (
              <li key={category.slug}>
                <SheetClose asChild>
                  <Link href={`/catalog?category=${category.slug}`} className={menuLink}>
                    {category.name}
                  </Link>
                </SheetClose>
              </li>
            ))}
          </ul>
        </nav>
      </SheetContent>
    </Sheet>
  );
}

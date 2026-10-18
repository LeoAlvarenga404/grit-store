"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import { cn } from "@/lib/cn";

const sections = [
  { href: "/admin/products", label: "Produtos" },
  { href: "/admin/orders", label: "Pedidos" },
];

export function AdminNav() {
  const pathname = usePathname();

  return (
    <nav aria-label="Administração" className="flex h-full items-center gap-6">
      {sections.map((section) => {
        const isCurrent = pathname.startsWith(section.href);
        return (
          <Link
            key={section.href}
            href={section.href}
            aria-current={isCurrent ? "page" : undefined}
            className={cn(
              "inline-flex h-full items-center border-b-2 text-sm",
              isCurrent ? "border-foreground font-semibold" : "border-transparent text-muted-foreground hover:text-foreground",
            )}
          >
            {section.label}
          </Link>
        );
      })}
    </nav>
  );
}

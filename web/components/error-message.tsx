import type { ReactNode } from "react";
import { cn } from "@/lib/cn";

export function ErrorMessage({ children, className }: { children: ReactNode; className?: string }) {
  return (
    <p role="alert" className={cn("border border-destructive/30 bg-destructive/5 px-4 py-3 text-sm text-destructive", className)}>
      {children}
    </p>
  );
}

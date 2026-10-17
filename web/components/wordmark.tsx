import Link from "next/link";
import { cn } from "@/lib/cn";

export function Wordmark({ className }: { className?: string }) {
  return (
    <Link href="/" aria-label="Grit Store, página inicial" className={cn("text-display text-2xl tracking-tight", className)}>
      Grit
    </Link>
  );
}

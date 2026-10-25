import Link from "next/link";
import { Logo } from "@/components/logo";
import { cn } from "@/lib/cn";

export function Wordmark({ className }: { className?: string }) {
  return (
    <Link href="/" aria-label="Grit Store, página inicial" className={cn("block", className)}>
      <Logo />
    </Link>
  );
}

import type { LucideIcon } from "lucide-react";
import Link from "next/link";
import { Button } from "@/components/ui/button";

export function EmptyState({
  icon: Icon,
  title,
  text,
  actionLabel,
  actionHref,
}: {
  icon: LucideIcon;
  title: string;
  text?: string;
  actionLabel?: string;
  actionHref?: string;
}) {
  return (
    <div className="flex flex-col items-center px-4 py-20 text-center">
      <Icon aria-hidden="true" className="size-10 text-muted-foreground" strokeWidth={1.25} />
      <h2 className="text-display mt-6 text-2xl">{title}</h2>
      {text && <p className="mt-3 max-w-sm text-sm text-muted-foreground">{text}</p>}
      {actionLabel && actionHref && (
        <Button asChild className="mt-8">
          <Link href={actionHref}>{actionLabel}</Link>
        </Button>
      )}
    </div>
  );
}

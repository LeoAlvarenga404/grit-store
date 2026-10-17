import { ArrowRight } from "lucide-react";
import Link from "next/link";

export function SectionHeading({ id, title, href, linkLabel = "Ver tudo" }: { id: string; title: string; href?: string; linkLabel?: string }) {
  return (
    <div className="mb-6 flex items-end justify-between gap-4">
      <h2 id={id} className="text-display text-2xl md:text-3xl">
        {title}
      </h2>
      {href && (
        <Link href={href} className="inline-flex items-center gap-1 text-sm font-semibold tracking-[0.08em] uppercase hover:underline">
          {linkLabel} <ArrowRight aria-hidden="true" className="size-4" />
        </Link>
      )}
    </div>
  );
}

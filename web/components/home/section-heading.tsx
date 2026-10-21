import { ArrowRight } from "lucide-react";
import Link from "next/link";

export function SectionHeading({ id, title, eyebrow, href, linkLabel = "Ver tudo" }: { id: string; title: string; eyebrow?: string; href?: string; linkLabel?: string }) {
  return (
    <div className="reveal mb-8 flex items-end justify-between gap-4">
      <div>
        {eyebrow && <p className="text-eyebrow mb-2 text-muted-foreground">{eyebrow}</p>}
        <h2 id={id} className="text-display text-2xl md:text-4xl">
          {title}
        </h2>
      </div>
      {href && (
        <Link href={href} className="group inline-flex items-center gap-1.5 text-sm font-semibold tracking-[0.08em] uppercase underline-offset-4 hover:underline">
          {linkLabel} <ArrowRight aria-hidden="true" className="size-4 transition-transform duration-200 group-hover:translate-x-1" />
        </Link>
      )}
    </div>
  );
}

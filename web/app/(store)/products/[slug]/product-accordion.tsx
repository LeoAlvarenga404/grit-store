import { ChevronDown } from "lucide-react";
import type { ReactNode } from "react";
import { productDetails } from "@/lib/product-variants";

function Section({ title, isOpen = false, children }: { title: string; isOpen?: boolean; children: ReactNode }) {
  return (
    <details open={isOpen} className="group border-b">
      <summary className="flex cursor-pointer list-none items-center justify-between py-5 text-sm font-semibold tracking-[0.08em] uppercase [&::-webkit-details-marker]:hidden">
        {title}
        <ChevronDown aria-hidden="true" className="size-4 transition-transform duration-300 group-open:rotate-180" />
      </summary>
      <div className="pb-6">{children}</div>
    </details>
  );
}

export function ProductAccordion({ description, attributes }: { description: string; attributes: Record<string, string> }) {
  const details = productDetails(attributes);

  return (
    <div className="mt-10 border-t">
      <Section title="Descrição" isOpen>
        <p className="text-sm leading-relaxed whitespace-pre-line text-muted-foreground">{description}</p>
      </Section>
      {details.length > 0 && (
        <Section title="Detalhes">
          <dl className="grid gap-x-6 gap-y-3 text-sm sm:grid-cols-[9rem_1fr]">
            {details.map((detail) => (
              <div key={detail.label} className="contents">
                <dt className="text-muted-foreground">{detail.label}</dt>
                <dd>{detail.value}</dd>
              </div>
            ))}
          </dl>
        </Section>
      )}
    </div>
  );
}

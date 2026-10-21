import { Check } from "lucide-react";
import { cn } from "@/lib/cn";

const steps = ["Sacola", "Pagamento", "Confirmação"];

export function CheckoutSteps({ current }: { current: 0 | 1 | 2 }) {
  return (
    <ol aria-label="Etapas da compra" className="mb-10 flex items-center gap-3 text-xs font-semibold tracking-[0.08em] uppercase">
      {steps.map((label, index) => (
        <li key={label} aria-current={index === current ? "step" : undefined} className="flex items-center gap-3">
          <span
            className={cn(
              "flex size-7 items-center justify-center rounded-full border text-[11px]",
              index < current && "border-foreground bg-foreground text-background",
              index === current && "border-foreground",
              index > current && "text-muted-foreground",
            )}
          >
            {index < current ? <Check aria-hidden="true" className="size-3.5" /> : index + 1}
          </span>
          <span className={cn(index > current && "text-muted-foreground")}>{label}</span>
          {index < steps.length - 1 && <span aria-hidden="true" className={cn("h-px w-6 sm:w-16", index < current ? "bg-foreground" : "bg-border")} />}
        </li>
      ))}
    </ol>
  );
}

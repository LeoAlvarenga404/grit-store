import { cn } from "@/lib/cn";
import { formatPrice } from "@/lib/format";

export function PriceSummary({ subtotalAmount, shippingAmount, className }: { subtotalAmount: number; shippingAmount: number; className?: string }) {
  return (
    <dl className={cn("space-y-3 text-sm", className)}>
      <div className="flex justify-between">
        <dt className="text-muted-foreground">Subtotal</dt>
        <dd className="tabular-nums">{formatPrice(subtotalAmount)}</dd>
      </div>
      <div className="flex justify-between">
        <dt className="text-muted-foreground">Frete fixo</dt>
        <dd className="tabular-nums">{formatPrice(shippingAmount)}</dd>
      </div>
      <div className="flex items-baseline justify-between border-t pt-4 text-lg font-semibold">
        <dt>Total</dt>
        <dd className="tabular-nums">{formatPrice(subtotalAmount + shippingAmount)}</dd>
      </div>
    </dl>
  );
}

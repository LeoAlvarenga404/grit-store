import { Badge } from "@/components/ui/badge";
import { cn } from "@/lib/cn";
import { orderStatusLabel } from "@/lib/order-labels";

// Green and red appear only with a meaning: a confirmed or a cancelled order.
const statusClasses: Record<string, string> = {
  Placed: "border-border bg-background text-muted-foreground",
  Confirmed: "border-success/30 bg-success/10 text-success",
  Shipped: "border-transparent bg-foreground text-background",
  Cancelled: "border-destructive/30 bg-destructive/10 text-destructive",
};

export function OrderStatusBadge({ status, className }: { status: string; className?: string }) {
  return (
    <Badge variant="outline" className={cn("rounded-sm tracking-[0.08em] uppercase", statusClasses[status], className)}>
      {orderStatusLabel(status)}
    </Badge>
  );
}

import { CircleCheck, CircleX, Loader2, Package, Truck, type LucideIcon } from "lucide-react";
import { cn } from "@/lib/cn";
import { cancellationReasonLabel, orderStatusLabel } from "@/lib/order-labels";

type Panel = { icon: LucideIcon; iconClassName?: string; title: string; text?: string };

const borderClasses: Record<string, string> = {
  Confirmed: "border-success/40",
  Cancelled: "border-destructive/40",
};

function panelFor(status: string, cancellationReason: string | null): Panel {
  switch (status) {
    case "Placed":
      return { icon: Loader2, iconClassName: "animate-spin", title: "Processando pagamento", text: "Estamos confirmando seu pagamento. Esta página se atualiza sozinha." };
    case "Confirmed":
      return { icon: CircleCheck, iconClassName: "text-success", title: "Pedido confirmado", text: "Pagamento aprovado. Você pode cancelar até o pedido ser enviado." };
    case "Shipped":
      return { icon: Truck, title: "Pedido enviado", text: "Seu pedido saiu para entrega." };
    case "Cancelled":
      return {
        icon: CircleX,
        iconClassName: "text-destructive",
        title: "Pedido cancelado",
        text: cancellationReason ? `Motivo: ${cancellationReasonLabel(cancellationReason)}.` : "Este pedido foi cancelado.",
      };
    default:
      return { icon: Package, title: orderStatusLabel(status) };
  }
}

export function OrderStatusPanel({ status, cancellationReason }: { status: string; cancellationReason: string | null }) {
  const panel = panelFor(status, cancellationReason);
  const Icon = panel.icon;

  return (
    <section aria-live="polite" className={cn("flex items-start gap-4 border p-6", borderClasses[status])}>
      <Icon aria-hidden="true" className={cn("size-8 shrink-0", panel.iconClassName)} />
      <div>
        <h2 className="text-display text-xl">{panel.title}</h2>
        {panel.text && <p className="mt-1 text-sm text-muted-foreground">{panel.text}</p>}
      </div>
    </section>
  );
}

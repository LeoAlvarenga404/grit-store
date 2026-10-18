import { PackageX, ShieldCheck, Truck } from "lucide-react";
import { flatShippingLabel } from "@/lib/store-navigation";

// Only promises the backend keeps: flat shipping, Stripe payment, cancellation until the order ships.
export const storeValues = [
  { icon: Truck, title: "Frete fixo", text: `${flatShippingLabel} para todo o Brasil` },
  { icon: ShieldCheck, title: "Pagamento seguro", text: "Processado pelo Stripe" },
  { icon: PackageX, title: "Mudou de ideia?", text: "Cancele até o pedido ser enviado" },
];

export function ValueStrip() {
  return (
    <section aria-label="Vantagens" className="border-b bg-muted/60">
      <ul className="mx-auto grid max-w-screen-2xl gap-6 px-4 py-6 sm:grid-cols-3 lg:px-8">
        {storeValues.map(({ icon: Icon, title, text }) => (
          <li key={title} className="flex items-center gap-4 sm:justify-center">
            <Icon aria-hidden="true" className="size-6 shrink-0" strokeWidth={1.5} />
            <div>
              <p className="text-sm font-semibold">{title}</p>
              <p className="text-xs text-muted-foreground">{text}</p>
            </div>
          </li>
        ))}
      </ul>
    </section>
  );
}

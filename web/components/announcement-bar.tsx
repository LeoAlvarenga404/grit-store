import { Truck } from "lucide-react";
import { flatShippingLabel } from "@/lib/store-navigation";

export function AnnouncementBar() {
  return (
    <div className="bg-foreground text-background">
      <div className="mx-auto flex max-w-screen-2xl items-center justify-center gap-x-6 gap-y-1 px-4 py-2 text-center text-xs sm:justify-between">
        <p className="flex items-center gap-2">
          <Truck aria-hidden="true" className="size-4" />
          Frete fixo de {flatShippingLabel} para todo o Brasil
        </p>
        <p className="hidden text-background/70 sm:block">Loja de portfólio · pagamentos em modo de teste</p>
      </div>
    </div>
  );
}

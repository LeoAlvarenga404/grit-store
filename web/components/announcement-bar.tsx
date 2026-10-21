import { Truck } from "lucide-react";
import { flatShippingLabel } from "@/lib/store-navigation";

export function AnnouncementBar() {
  return (
    <div className="bg-foreground text-background">
      <p className="mx-auto flex max-w-screen-2xl items-center justify-center gap-2 px-4 py-2 text-center text-xs">
        <Truck aria-hidden="true" className="size-4" />
        Frete fixo de {flatShippingLabel} para todo o Brasil
      </p>
    </div>
  );
}

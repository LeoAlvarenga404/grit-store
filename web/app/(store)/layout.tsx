import type { ReactNode } from "react";
import { AnnouncementBar } from "@/components/announcement-bar";
import { BasketCountProvider } from "@/components/basket-count";
import { SiteFooter } from "@/components/site-footer";
import { SiteHeader } from "@/components/site-header";
import { getVisitor } from "@/lib/storefront";

export default async function StoreLayout({ children }: Readonly<{ children: ReactNode }>) {
  const visitor = await getVisitor();

  return (
    <BasketCountProvider count={visitor.basketItemCount}>
      <AnnouncementBar />
      <SiteHeader isSignedIn={visitor.isSignedIn} />
      <div className="flex-1">{children}</div>
      <SiteFooter />
    </BasketCountProvider>
  );
}

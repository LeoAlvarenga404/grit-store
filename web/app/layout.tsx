import type { Metadata } from "next";
import { Archivo } from "next/font/google";
import type { ReactNode } from "react";
import { Toaster } from "@/components/ui/sonner";
import { siteUrl } from "@/lib/site-url";
import "./globals.css";

const archivo = Archivo({ subsets: ["latin"], axes: ["wdth"], variable: "--font-archivo" });

export const metadata: Metadata = {
  metadataBase: new URL(siteUrl),
  title: { default: "Grit Store", template: "%s · Grit Store" },
  description: "Roupas e tênis esportivos para quem treina de verdade.",
  openGraph: { siteName: "Grit Store", locale: "pt_BR", type: "website" },
  twitter: { card: "summary_large_image" },
};

// The storefront chrome lives in app/(store)/layout.tsx, so the admin and the 404/error pages render without it.
export default function RootLayout({ children }: Readonly<{ children: ReactNode }>) {
  return (
    <html lang="pt-BR" className={archivo.variable}>
      <body className="flex min-h-dvh flex-col">
        {children}
        <Toaster position="top-center" containerAriaLabel="Notificações" />
      </body>
    </html>
  );
}

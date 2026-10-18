import type { Metadata } from "next";
import { Archivo } from "next/font/google";
import type { ReactNode } from "react";
import { Toaster } from "@/components/ui/sonner";
import "./globals.css";

const archivo = Archivo({ subsets: ["latin"], axes: ["wdth"], variable: "--font-archivo" });

export const metadata: Metadata = {
  title: { default: "Grit Store", template: "%s · Grit Store" },
  description: "Roupas e tênis esportivos.",
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

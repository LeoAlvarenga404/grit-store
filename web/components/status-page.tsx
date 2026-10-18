import type { ReactNode } from "react";
import { Wordmark } from "@/components/wordmark";

// 404 and error pages render outside the store layout, so they bring their own minimal header.
export function StatusPage({ code, title, text, children }: { code: string; title: string; text: string; children: ReactNode }) {
  return (
    <>
      <header className="border-b">
        <div className="mx-auto flex h-16 max-w-screen-2xl items-center px-4 lg:px-8">
          <Wordmark />
        </div>
      </header>
      <main className="mx-auto flex w-full max-w-xl flex-1 flex-col items-center justify-center px-4 py-24 text-center">
        <p aria-hidden="true" className="text-display text-7xl md:text-9xl">
          {code}
        </p>
        <h1 className="text-display mt-6 text-2xl md:text-3xl">{title}</h1>
        <p className="mt-4 text-sm text-muted-foreground">{text}</p>
        <div className="mt-10 flex flex-wrap justify-center gap-3">{children}</div>
      </main>
    </>
  );
}

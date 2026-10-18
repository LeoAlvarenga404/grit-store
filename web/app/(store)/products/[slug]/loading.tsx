import { Skeleton } from "@/components/ui/skeleton";

export default function Loading() {
  return (
    <main className="mx-auto max-w-screen-2xl px-4 py-6 lg:px-8">
      <Skeleton className="h-4 w-64" />
      <div className="mt-6 grid gap-8 lg:grid-cols-[minmax(0,7fr)_minmax(0,5fr)] lg:gap-14">
        <Skeleton className="aspect-[4/5] w-full" />
        <div>
          <Skeleton className="h-3 w-24" />
          <Skeleton className="mt-3 h-9 w-3/4" />
          <Skeleton className="mt-6 h-7 w-32" />
          <Skeleton className="mt-8 h-16 w-64" />
          <Skeleton className="mt-8 h-11 w-72" />
          <Skeleton className="mt-8 h-14 w-full" />
        </div>
      </div>
      <span role="status" className="sr-only">Carregando produto…</span>
    </main>
  );
}

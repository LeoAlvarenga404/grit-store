import { Skeleton } from "@/components/ui/skeleton";

export default function Loading() {
  return (
    <div className="mx-auto max-w-screen-2xl px-4 py-8 lg:px-8">
      <Skeleton className="h-9 w-64" />
      <div className="mt-8 grid grid-cols-2 gap-x-3 gap-y-8 lg:ml-[17.5rem] lg:grid-cols-4">
        {Array.from({ length: 8 }, (_, index) => (
          <div key={index}>
            <Skeleton className="aspect-[4/5] w-full" />
            <Skeleton className="mt-3 h-4 w-3/4" />
            <Skeleton className="mt-2 h-4 w-1/3" />
          </div>
        ))}
      </div>
      <span role="status" className="sr-only">Carregando produtos…</span>
    </div>
  );
}

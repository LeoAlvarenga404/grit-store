import type { components } from "@/lib/api/schema";
import { cn } from "@/lib/cn";
import { formatDateTime } from "@/lib/format";
import { orderStatusLabel } from "@/lib/order-labels";

export function OrderTimeline({ history }: { history: components["schemas"]["OrderStatusChangeResponse"][] }) {
  return (
    <section aria-labelledby="history-title">
      <h2 id="history-title" className="text-display text-lg">
        Histórico
      </h2>
      <ol className="mt-4 space-y-4 border-l pl-6">
        {history.map((change, index) => {
          const isLast = index === history.length - 1;

          return (
            <li key={`${change.status}-${change.occurredAt}`} className="relative">
              <span
                aria-hidden="true"
                className={cn("absolute top-1.5 -left-[1.8rem] size-2.5 rounded-full border-2 border-background", isLast ? "bg-foreground" : "bg-input")}
              />
              <p className={cn("text-sm", isLast && "font-semibold")}>{orderStatusLabel(change.status)}</p>
              <p className="text-xs text-muted-foreground">
                <time dateTime={change.occurredAt}>{formatDateTime(change.occurredAt)}</time>
              </p>
            </li>
          );
        })}
      </ol>
    </section>
  );
}

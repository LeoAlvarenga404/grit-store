import { cn } from "@/lib/cn";

// Four letters on a 100-unit cap height, drawn from straight strokes and 45° chamfers (the G and the R bowl) so the
// word reads like track markings. It follows currentColor, so one file serves the header, the footer and the admin.
export function Logo({ className }: { className?: string }) {
  return (
    <svg viewBox="0 0 350 100" role="img" aria-label="Grit" className={cn("h-6 w-auto", className)}>
      <g fill="none" stroke="currentColor" strokeWidth="22" strokeLinejoin="miter">
        <polyline points="84,11 30,11 11,30 11,70 30,89 89,89 89,50 55,50" />
        <polyline points="125,11 176,11 197,32 197,38 176,59 125,59" />
      </g>
      <g fill="currentColor">
        <rect x="114" width="22" height="100" />
        <polygon points="152,52 178,52 210,100 182,100" />
        <rect x="224" width="22" height="100" />
        <rect x="260" width="90" height="22" />
        <rect x="294" width="22" height="100" />
      </g>
    </svg>
  );
}

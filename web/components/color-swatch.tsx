import { cn } from "@/lib/cn";
import { swatchFor } from "@/lib/color-swatches";

// Known colour names get a flat swatch; any other falls back to the colour's own photo, then to a neutral grey.
export function ColorSwatch({ name, imageUrl, className }: { name: string; imageUrl: null | string; className?: string }) {
  const swatch = swatchFor(name);
  const background = swatch ?? (imageUrl ? `center / cover url("${imageUrl}")` : "var(--input)");
  return <span aria-hidden="true" className={cn("block size-3.5 shrink-0 rounded-full ring-1 ring-black/15", className)} style={{ background }} />;
}

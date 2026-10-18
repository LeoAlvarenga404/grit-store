import Image from "next/image";
import Link from "next/link";
import { ColorSwatch } from "@/components/color-swatch";
import type { components } from "@/lib/api/schema";
import { cn } from "@/lib/cn";
import { colorLabel, productHref } from "@/lib/product-variants";

type Color = components["schemas"]["GetProductBySlugColorResponse"];

// A colour is a link (?cor=), so the server renders that colour's gallery and sizes and the URL can be shared.
export function ColorPicker({ slug, colors, selected }: { slug: string; colors: Color[]; selected: string | null }) {
  if (colors.length === 0) return null;

  return (
    <div>
      <p className="text-sm">
        <span className="font-semibold">Cor:</span> {selected && colorLabel(selected)}
      </p>
      <ul className="mt-3 flex flex-wrap gap-2">
        {colors.map((color) => {
          const isSelected = color.name === selected;
          return (
            <li key={color.name}>
              <Link
                href={productHref(slug, color.name)}
                replace
                scroll={false}
                aria-label={`Cor ${color.name}`}
                aria-current={isSelected ? "true" : undefined}
                className={cn(
                  "relative flex size-16 items-center justify-center overflow-hidden rounded-sm border bg-muted",
                  isSelected ? "border-foreground ring-1 ring-foreground" : "hover:border-foreground",
                )}
              >
                {color.imageUrls[0] ? (
                  <Image src={color.imageUrls[0]} alt="" fill sizes="64px" className="object-contain p-1 mix-blend-multiply" unoptimized />
                ) : (
                  <ColorSwatch name={color.name} imageUrl={null} className="size-7" />
                )}
              </Link>
            </li>
          );
        })}
      </ul>
    </div>
  );
}

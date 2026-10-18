import Image from "next/image";
import { Trash2 } from "lucide-react";
import type { components } from "@/lib/api/schema";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { NativeSelect, NativeSelectOption } from "@/components/ui/native-select";
import { SubmitButton } from "@/components/submit-button";
import { removeImage, uploadImage } from "../actions";

type Product = components["schemas"]["GetProductResponse"];

export function ProductImages({ product, colors }: { product: Product; colors: string[] }) {
  return (
    <section aria-labelledby="images-title" className="mt-8 rounded-md border bg-background p-6">
      <div className="flex items-baseline justify-between gap-4">
        <h2 id="images-title" className="text-lg font-semibold">Imagens</h2>
        <p className="text-xs text-muted-foreground">JPEG, PNG ou WebP, até 2 MB. Até 8 imagens por cor; a primeira é a capa.</p>
      </div>
      {product.images.length === 0
        ? <p className="mt-4 text-sm text-muted-foreground">Sem imagens. A primeira enviada vira a capa.</p>
        : <ul className="mt-4 grid grid-cols-2 gap-4 sm:grid-cols-4 lg:grid-cols-6">
            {product.images.map((image, index) => (
              <li key={image.key} className="rounded-md border p-2">
                <div className="relative aspect-square bg-muted"><Image src={image.url} alt={`${product.name}, imagem ${index + 1}`} fill sizes="160px" className="object-contain" unoptimized /></div>
                <div className="mt-2 flex flex-wrap gap-1">
                  {index === 0 && <Badge>Capa</Badge>}
                  <Badge variant="outline" className="capitalize">{image.color ?? "Sem cor"}</Badge>
                </div>
                <form action={removeImage} className="mt-1">
                  <input type="hidden" name="productId" value={product.id} />
                  <input type="hidden" name="fileName" value={image.fileName} />
                  <input type="hidden" name="version" value={product.version} />
                  <Button type="submit" variant="ghost" size="sm" className="h-8 px-2 text-destructive"><Trash2 aria-hidden="true" /> Remover</Button>
                </form>
              </li>
            ))}
          </ul>}
      {colors.length === 0
        ? <p className="mt-6 text-sm text-muted-foreground">Cadastre um SKU antes de enviar imagens: cada imagem mostra uma cor dos SKUs.</p>
        : <form action={uploadImage} className="mt-6 grid items-end gap-4 border-t pt-6 sm:grid-cols-[12rem_minmax(0,1fr)_auto]">
            <input type="hidden" name="productId" value={product.id} />
            <input type="hidden" name="version" value={product.version} />
            <div className="space-y-1.5"><Label htmlFor="color">Cor</Label>
              <NativeSelect id="color" name="color" defaultValue={colors[0]} className="w-full">
                {colors.map((color) => <NativeSelectOption key={color} value={color}>{color || "Sem cor"}</NativeSelectOption>)}
              </NativeSelect></div>
            <div className="space-y-1.5"><Label htmlFor="file">Nova imagem</Label>
              <Input id="file" type="file" name="file" accept="image/jpeg,image/png,image/webp" required /></div>
            <SubmitButton size="sm" className="h-9" pendingLabel="Enviando…">Enviar imagem</SubmitButton>
          </form>}
    </section>
  );
}

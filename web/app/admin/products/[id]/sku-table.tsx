import type { components } from "@/lib/api/schema";
import { formatPrice } from "@/lib/format";
import { Input } from "@/components/ui/input";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { SubmitButton } from "@/components/submit-button";
import { changePrice, setStock } from "../actions";

type Product = components["schemas"]["GetProductResponse"];
type StockLevel = components["schemas"]["StockLevelResponse"];

function describe(attributes: Record<string, string>): string {
  return Object.entries(attributes).map(([key, value]) => `${key}: ${value}`).join(" · ") || "Única";
}

export function SkuTable({ product, levels }: { product: Product; levels: StockLevel[] }) {
  const levelBySku = new Map(levels.map((level) => [level.skuId, level]));

  return (
    <section aria-labelledby="skus-title" className="mt-8 rounded-md border bg-background p-6">
      <h2 id="skus-title" className="text-lg font-semibold">Variantes</h2>
      <div className="mt-4"><Table>
        <TableHeader>
          <TableRow>
            <TableHead scope="col">Variante</TableHead>
            <TableHead scope="col">Preço</TableHead>
            <TableHead scope="col">Estoque</TableHead>
          </TableRow>
        </TableHeader>
        <TableBody>
          {product.skus.map((sku) => {
            const level = levelBySku.get(sku.id);
            const variant = describe(sku.attributes);
            return (
              <TableRow key={sku.id} className="align-top">
                <TableCell className="py-3 font-medium">{variant}</TableCell>
                <TableCell className="py-3">
                  <form action={changePrice} className="flex items-center gap-2">
                    <input type="hidden" name="productId" value={product.id} />
                    <input type="hidden" name="skuId" value={sku.id} />
                    <input type="hidden" name="version" value={product.version} />
                    <Input name="priceAmount" type="number" min="0.01" step="0.01" defaultValue={sku.priceAmount} aria-label={`Preço de ${variant}`} required className="h-9 w-28 tabular-nums" />
                    <SubmitButton size="sm" variant="outline" className="h-9" pendingLabel="Salvando…">Salvar</SubmitButton>
                  </form>
                  <p className="mt-1 text-xs text-muted-foreground tabular-nums">Atual: {formatPrice(sku.priceAmount)}</p>
                </TableCell>
                <TableCell className="py-3">
                  <form action={setStock} className="flex items-center gap-2">
                    <input type="hidden" name="productId" value={product.id} />
                    <input type="hidden" name="skuId" value={sku.id} />
                    <Input name="onHand" type="number" min={0} step={1} defaultValue={level?.onHand ?? 0} aria-label={`Estoque de ${variant}`} required className="h-9 w-24 tabular-nums" />
                    <SubmitButton size="sm" variant="outline" className="h-9" pendingLabel="Salvando…">Salvar</SubmitButton>
                  </form>
                  <p className="mt-1 text-xs text-muted-foreground tabular-nums">{level ? `${level.held} reservadas · ${level.available} disponíveis` : "Sem estoque cadastrado"}</p>
                </TableCell>
              </TableRow>
            );
          })}
          {product.skus.length === 0 && (
            <TableRow>
              <TableCell colSpan={3} className="py-6 text-center text-muted-foreground">Nenhuma variante: o produto fica fora da loja.</TableCell>
            </TableRow>
          )}
        </TableBody>
      </Table></div>
    </section>
  );
}

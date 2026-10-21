"use client";

import { Minus, Plus, ShoppingBag } from "lucide-react";
import { useRouter } from "next/navigation";
import { useState, useTransition, type ReactNode } from "react";
import { toast } from "sonner";
import { addToBasket } from "@/app/(store)/basket/actions";
import { useBasketCount } from "@/components/basket-count";
import { Button } from "@/components/ui/button";
import type { components } from "@/lib/api/schema";
import { formatPrice } from "@/lib/format";
import { isSingleOption, lowestPrice, sizeOptions, type SizeOption } from "@/lib/product-variants";

type Sku = components["schemas"]["GetProductBySlugSkuResponse"];

const maxQuantity = 10;

const errorMessages: Record<string, string> = {
  "basket.quantity_limit": "No máximo 10 unidades por item.",
  "basket.line_limit": "A sacola aceita até 50 itens diferentes.",
  "basket.sku_not_found": "Esta variante não está mais à venda.",
  "validation.failed": "Escolha um tamanho e uma quantidade de 1 a 10.",
};

// The page renders this with key = the selected colour, so a colour change starts with no size chosen.
export function PurchasePanel({ skus, colorPicker, children }: { skus: Sku[]; colorPicker: ReactNode; children: ReactNode }) {
  const options = sizeOptions(skus);
  const [skuId, setSkuId] = useState(options.length === 1 ? options[0].sku.id : null);
  const [quantity, setQuantity] = useState(1);
  const [isSizeMissing, setIsSizeMissing] = useState(false);
  const [isPending, startTransition] = useTransition();
  const { addOptimistic } = useBasketCount();
  const router = useRouter();
  const selected = options.find((option) => option.sku.id === skuId);
  const price = selected ? { amount: selected.sku.priceAmount, hasRange: false } : lowestPrice(skus);

  function addToBag() {
    if (!skuId) {
      setIsSizeMissing(true);
      return;
    }
    startTransition(async () => {
      addOptimistic(quantity);
      const { errorCode } = await addToBasket(skuId, quantity);
      if (errorCode) {
        toast.error(errorMessages[errorCode] ?? "Não foi possível adicionar à sacola. Tente de novo.");
        return;
      }
      toast.success("Adicionado à sacola", { action: { label: "Ver sacola", onClick: () => router.push("/basket") } });
    });
  }

  function choose(option: SizeOption) {
    setSkuId(option.sku.id);
    setIsSizeMissing(false);
  }

  return (
    <div className="mt-4 space-y-8">
      <p className="text-2xl font-semibold tabular-nums" aria-live="polite">
        {price ? (
          <>
            {price.hasRange && <span className="text-base font-normal text-muted-foreground">a partir de </span>}
            {formatPrice(price.amount)}
          </>
        ) : (
          "Indisponível"
        )}
      </p>
      {colorPicker}
      {!isSingleOption(options) && <SizePicker options={options} selected={selected} isMissing={isSizeMissing} onChoose={choose} />}
      <div className="flex flex-col gap-3 min-[430px]:flex-row">
        <QuantityStepper quantity={quantity} onChange={setQuantity} />
        <Button size="lg" className="flex-1" onClick={addToBag} disabled={isPending || !price}>
          <ShoppingBag aria-hidden="true" />
          {isPending ? "Adicionando…" : "Adicionar à sacola"}
        </Button>
      </div>
      {children}
    </div>
  );
}

type SizePickerProps = { options: SizeOption[]; selected: SizeOption | undefined; isMissing: boolean; onChoose: (option: SizeOption) => void };

function SizePicker({ options, selected, isMissing, onChoose }: SizePickerProps) {
  return (
    <fieldset aria-describedby={isMissing ? "size-missing" : undefined}>
      <legend className="text-sm">
        <span className="font-semibold">Tamanho:</span> {selected?.label ?? "escolha um"}
      </legend>
      <div className="mt-3 flex flex-wrap gap-2">
        {options.map((option) => (
          <label key={option.sku.id} className="cursor-pointer">
            <input
              type="radio"
              name="size"
              value={option.sku.id}
              checked={selected?.sku.id === option.sku.id}
              onChange={() => onChoose(option)}
              className="peer sr-only"
            />
            <span className="flex h-11 min-w-12 items-center justify-center rounded-sm border px-3 text-sm font-medium peer-checked:border-foreground peer-checked:bg-foreground peer-checked:text-background peer-focus-visible:ring-2 peer-focus-visible:ring-ring hover:border-foreground">
              {option.label}
            </span>
          </label>
        ))}
      </div>
      {isMissing && (
        <p id="size-missing" role="alert" className="mt-2 text-sm text-destructive">
          Escolha um tamanho.
        </p>
      )}
    </fieldset>
  );
}

function QuantityStepper({ quantity, onChange }: { quantity: number; onChange: (quantity: number) => void }) {
  const step = "flex size-11 items-center justify-center disabled:opacity-30";

  return (
    <div className="flex h-14 w-fit items-center rounded-sm border">
      <button type="button" className={step} aria-label="Diminuir quantidade" disabled={quantity <= 1} onClick={() => onChange(quantity - 1)}>
        <Minus aria-hidden="true" className="size-4" />
      </button>
      <output className="w-6 text-center text-sm tabular-nums" aria-label={`Quantidade: ${quantity}`}>
        {quantity}
      </output>
      <button type="button" className={step} aria-label="Aumentar quantidade" disabled={quantity >= maxQuantity} onClick={() => onChange(quantity + 1)}>
        <Plus aria-hidden="true" className="size-4" />
      </button>
    </div>
  );
}

import type { Metadata } from "next";
import Link from "next/link";
import { notFound } from "next/navigation";
import { ArrowLeft, ExternalLink } from "lucide-react";
import type { components } from "@/lib/api/schema";
import { api } from "@/lib/api/client";
import { callApi } from "@/lib/api/call-api";
import { Button } from "@/components/ui/button";
import { ErrorMessage } from "@/components/error-message";
import { ProductImages } from "./product-images";
import { SkuTable } from "./sku-table";

type StockLevel = components["schemas"]["StockLevelResponse"];

export const metadata: Metadata = { title: "Produto" };

const errorMessages: Record<string, string> = {
  "catalog.product_changed": "Outra pessoa alterou o produto. Confira os valores e tente de novo.",
  "catalog.image_limit": "Essa cor já tem 8 imagens. Remova uma antes.",
  "catalog.image_color_not_found": "Nenhum SKU do produto tem essa cor. Cadastre o SKU antes.",
  "catalog.duplicate_image": "Essa imagem já foi anexada.",
  "catalog.invalid_image": "Use uma imagem JPEG, PNG ou WebP de até 2 MB.",
  "catalog.image_not_uploaded": "O arquivo não chegou ao armazenamento. Tente de novo.",
  "catalog.image_not_found": "A imagem já tinha sido removida.",
  "inventory.below_held_units": "O estoque não pode ficar abaixo das unidades reservadas.",
  "validation.failed": "Confira os valores: preço maior que zero, estoque a partir de zero, imagem JPEG, PNG ou WebP de até 2 MB.",
  image_missing: "Escolha um arquivo de imagem.",
  upload_failed: "O envio da imagem falhou. Tente de novo.",
};

export default async function AdminProductPage({
  params,
  searchParams,
}: {
  params: Promise<{ id: string }>;
  searchParams: Promise<{ error?: string }>;
}) {
  const { id } = await params;
  const { error } = await searchParams;
  const { data: product, response } = await callApi((headers) =>
    api.GET("/api/v1/admin/products/{id}", { headers, params: { path: { id } } }),
  );
  if (response.status === 404) notFound();
  if (!product) throw new Error(`The admin product answered ${response.status}.`);
  const skuIds = product.skus.map((sku) => sku.id);
  const levels: StockLevel[] =
    skuIds.length === 0
      ? []
      : ((await callApi((headers) => api.GET("/api/v1/admin/stock", { headers, params: { query: { skuId: skuIds } } }))).data?.items ?? []);
  // "" stands for SKUs without a colour: their images are sent without one.
  const colors = [...new Set(product.skus.map((sku) => sku.attributes["cor"] ?? ""))];

  return (
    <>
      <Link href="/admin/products" className="inline-flex items-center gap-1 text-sm text-muted-foreground hover:text-foreground"><ArrowLeft aria-hidden="true" className="size-4" /> Produtos</Link>
      <div className="mt-3 flex flex-wrap items-end justify-between gap-3">
        <div>
          <h1 className="text-2xl font-semibold tracking-tight">{product.name}</h1>
          <p className="mt-1 font-mono text-xs text-muted-foreground">/products/{product.slug}</p>
        </div>
        <Button asChild variant="outline" size="sm"><Link href={`/products/${product.slug}`} target="_blank">Ver na loja <ExternalLink aria-hidden="true" /></Link></Button>
      </div>
      {error && <ErrorMessage className="mt-6">{errorMessages[error] ?? "Não foi possível salvar. Tente de novo."}</ErrorMessage>}
      <ProductImages product={product} colors={colors} />
      <SkuTable product={product} levels={levels} />
    </>
  );
}

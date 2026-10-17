import Image from "next/image";
import Link from "next/link";
import { notFound } from "next/navigation";
import type { components } from "@/lib/api/schema";
import { api } from "@/lib/api/client";
import { callApi } from "@/lib/api/call-api";
import { formatPrice } from "@/lib/format";
import { changePrice, removeImage, setStock, uploadImage } from "../actions";

type StockLevel = components["schemas"]["StockLevelResponse"];

const errorMessages: Record<string, string> = {
  "catalog.product_changed": "Outra pessoa alterou o produto. Confira os valores e tente de novo.",
  "catalog.image_limit": "O produto já tem 8 imagens. Remova uma antes.",
  "catalog.invalid_image": "Use uma imagem JPEG, PNG ou WebP de até 2 MB.",
  "catalog.image_not_uploaded": "O arquivo não chegou ao armazenamento. Tente de novo.",
  "catalog.image_not_found": "A imagem já tinha sido removida.",
  "inventory.below_held_units": "O estoque não pode ficar abaixo das unidades reservadas.",
  "validation.failed": "Confira os valores: preço maior que zero, estoque a partir de zero, imagem JPEG, PNG ou WebP de até 2 MB.",
  image_missing: "Escolha um arquivo de imagem.",
  upload_failed: "O envio da imagem falhou. Tente de novo.",
};

function describe(attributes: Record<string, string>): string {
  return Object.entries(attributes).map(([key, value]) => `${key}: ${value}`).join(" · ") || "Única";
}

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
  const levelBySku = new Map(levels.map((level) => [level.skuId, level]));

  return (
    <main className="narrow">
      <p>
        <Link href="/admin/products">Produtos</Link>
      </p>
      <h1>{product.name}</h1>
      <p className="hint">/products/{product.slug}</p>
      {error && (
        <p role="alert" className="error">
          {errorMessages[error] ?? "Não foi possível salvar. Tente de novo."}
        </p>
      )}

      <h2>Imagens</h2>
      {product.images.length === 0 ? (
        <p>Sem imagens. A primeira enviada vira a capa.</p>
      ) : (
        <ul className="thumbnails">
          {product.images.map((image, index) => (
            <li key={image.key}>
              <Image src={image.url} alt={`${product.name}, imagem ${index + 1}`} width={120} height={120} unoptimized />
              <form action={removeImage}>
                <input type="hidden" name="productId" value={product.id} />
                <input type="hidden" name="fileName" value={image.fileName} />
                <input type="hidden" name="version" value={product.version} />
                <button type="submit" className="link-button">
                  Remover
                </button>
              </form>
            </li>
          ))}
        </ul>
      )}
      <form action={uploadImage} className="stack">
        <input type="hidden" name="productId" value={product.id} />
        <input type="hidden" name="version" value={product.version} />
        <label>
          Nova imagem (JPEG, PNG ou WebP, até 2 MB)
          <input type="file" name="file" accept="image/jpeg,image/png,image/webp" required />
        </label>
        <button type="submit">Enviar imagem</button>
      </form>

      <h2>Variantes</h2>
      <table>
        <thead>
          <tr>
            <th scope="col">Variante</th>
            <th scope="col">Preço</th>
            <th scope="col">Estoque</th>
          </tr>
        </thead>
        <tbody>
          {product.skus.map((sku) => {
            const level = levelBySku.get(sku.id);
            const variant = describe(sku.attributes);
            return (
              <tr key={sku.id}>
                <td>{variant}</td>
                <td>
                  <form action={changePrice} className="inline-form">
                    <input type="hidden" name="productId" value={product.id} />
                    <input type="hidden" name="skuId" value={sku.id} />
                    <input type="hidden" name="version" value={product.version} />
                    <input name="priceAmount" type="number" min="0.01" step="0.01" defaultValue={sku.priceAmount} aria-label={`Preço de ${variant}`} required />
                    <button type="submit">Salvar</button>
                  </form>
                  <span className="hint"> {formatPrice(sku.priceAmount)}</span>
                </td>
                <td>
                  <form action={setStock} className="inline-form">
                    <input type="hidden" name="productId" value={product.id} />
                    <input type="hidden" name="skuId" value={sku.id} />
                    <input name="onHand" type="number" min={0} step={1} defaultValue={level?.onHand ?? 0} aria-label={`Estoque de ${variant}`} required />
                    <button type="submit">Salvar</button>
                  </form>
                  <span className="hint"> {level ? `${level.held} reservadas, ${level.available} disponíveis` : "Sem estoque"}</span>
                </td>
              </tr>
            );
          })}
        </tbody>
      </table>
    </main>
  );
}

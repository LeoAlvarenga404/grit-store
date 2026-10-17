import Image from "next/image";
import Link from "next/link";
import { notFound } from "next/navigation";
import { api } from "@/lib/api/client";
import { formatPrice } from "@/lib/format";
import { addToBasket } from "@/app/basket/actions";

const errorMessages: Record<string, string> = {
  "basket.quantity_limit": "No máximo 10 unidades por item.",
  "basket.line_limit": "O carrinho aceita até 50 itens diferentes.",
  "basket.sku_not_found": "Esta variante não está mais à venda.",
  "validation.failed": "Escolha uma variante e uma quantidade de 1 a 10.",
};

function describe(attributes: Record<string, string>): string {
  return Object.entries(attributes)
    .map(([key, value]) => `${key}: ${value}`)
    .join(" · ");
}

export default async function ProductPage({
  params,
  searchParams,
}: {
  params: Promise<{ slug: string }>;
  searchParams: Promise<{ error?: string }>;
}) {
  const { slug } = await params;
  const { error } = await searchParams;
  const { data: product, response } = await api.GET("/api/v1/catalog/products/{slug}", {
    params: { path: { slug } },
  });
  if (response.status === 404) notFound();
  if (!product) throw new Error(`The catalog answered ${response.status} for the product ${slug}.`);

  return (
    <main className="legacy narrow">
      <p>
        <Link href={`/?category=${encodeURIComponent(product.categorySlug)}`}>{product.categoryName}</Link>
      </p>
      <h1>{product.name}</h1>
      {product.imageUrls.length > 0 && (
        <ul className="gallery">
          {product.imageUrls.map((url, index) => (
            <li key={url}>
              <Image src={url} alt={`${product.name}, imagem ${index + 1}`} width={320} height={320} unoptimized />
            </li>
          ))}
        </ul>
      )}
      <p>{product.description}</p>
      {Object.keys(product.attributes).length > 0 && <p className="hint">{describe(product.attributes)}</p>}
      {error && (
        <p role="alert" className="error">
          {errorMessages[error] ?? "Não foi possível adicionar ao carrinho. Tente de novo."}
        </p>
      )}
      <form action={addToBasket} className="stack">
        <input type="hidden" name="slug" value={product.slug} />
        <fieldset>
          <legend>Variante</legend>
          {product.skus.map((sku, index) => (
            <label key={sku.id} className="option">
              <input type="radio" name="skuId" value={sku.id} defaultChecked={index === 0} required />
              {describe(sku.attributes) || "Única"} — {formatPrice(sku.priceAmount)}
            </label>
          ))}
        </fieldset>
        <label>
          Quantidade
          <input name="quantity" type="number" min={1} max={10} defaultValue={1} required />
        </label>
        <button type="submit">
          Adicionar ao carrinho
        </button>
      </form>
    </main>
  );
}

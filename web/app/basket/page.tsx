import Link from "next/link";
import { api } from "@/lib/api/client";
import { callApi } from "@/lib/api/call-api";
import { formatPrice } from "@/lib/format";
import { changeQuantity, removeLine } from "./actions";

const errorMessages: Record<string, string> = {
  "basket.quantity_limit": "No máximo 10 unidades por item.",
  "basket.line_not_found": "Este item não está mais no carrinho.",
  "validation.failed": "Use uma quantidade de 1 a 10.",
};

export default async function BasketPage({ searchParams }: { searchParams: Promise<{ error?: string }> }) {
  const { error } = await searchParams;
  const { data: basket, response } = await callApi((headers) => api.GET("/api/v1/basket", { headers }));
  if (!basket) throw new Error(`The basket answered ${response.status}.`);

  return (
    <main>
      <h1>Carrinho</h1>
      {error && (
        <p role="alert" className="error">
          {errorMessages[error] ?? "Não foi possível alterar o carrinho. Tente de novo."}
        </p>
      )}
      {basket.lines.length === 0 ? (
        <p>
          Seu carrinho está vazio. <Link href="/">Ver produtos</Link>
        </p>
      ) : (
        <>
          <table>
            <thead>
              <tr>
                <th scope="col">Produto</th>
                <th scope="col">Preço</th>
                <th scope="col">Quantidade</th>
                <th scope="col">
                  <span className="hint">Ações</span>
                </th>
              </tr>
            </thead>
            <tbody>
              {basket.lines.map((line) => (
                <tr key={line.skuId}>
                  <td>
                    <Link href={`/products/${line.productSlug}`}>{line.productName}</Link>
                    {!line.isAvailable && <p className="error">Indisponível</p>}
                  </td>
                  <td>
                    {formatPrice(line.currentUnitPriceAmount ?? line.unitPriceAmount)}
                    {line.hasPriceChanged && <p className="hint">Era {formatPrice(line.unitPriceAmount)}</p>}
                  </td>
                  <td>
                    <form action={changeQuantity} className="inline">
                      <input type="hidden" name="skuId" value={line.skuId} />
                      <input
                        name="quantity"
                        type="number"
                        min={1}
                        max={10}
                        defaultValue={line.quantity}
                        required
                        aria-label={`Quantidade de ${line.productName}`}
                      />
                      <button type="submit" className="secondary">
                        Atualizar
                      </button>
                    </form>
                  </td>
                  <td>
                    <form action={removeLine}>
                      <input type="hidden" name="skuId" value={line.skuId} />
                      <button type="submit" className="link-button" aria-label={`Remover ${line.productName}`}>
                        Remover
                      </button>
                    </form>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
          <p>
            Subtotal: <strong>{formatPrice(basket.subtotalAmount)}</strong>
          </p>
          <Link href="/checkout">Finalizar compra</Link>
        </>
      )}
    </main>
  );
}

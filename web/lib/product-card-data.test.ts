import assert from "node:assert/strict";
import { test } from "node:test";
import { toCardProduct } from "./product-card-data.ts";

const product = {
  id: "1",
  slug: "camiseta",
  name: "Camiseta",
  description: "",
  attributes: {},
  categorySlug: "camisetas",
  categoryName: "Camisetas",
  skus: [
    { id: "a", attributes: {}, priceAmount: 89.9, priceCurrency: "BRL" },
    { id: "b", attributes: {}, priceAmount: 69.9, priceCurrency: "BRL" },
  ],
  imageUrls: ["cover.webp", "second.webp"],
  colors: [{ name: "Preto", imageUrls: ["preto-1.webp"] }, { name: "Cinza", imageUrls: [] }],
};

test("the card shows the cheapest SKU and says when prices differ", () => {
  const card = toCardProduct(product);
  assert.equal(card.priceAmount, 69.9);
  assert.equal(card.hasPriceRange, true);
});

test("one price across SKUs is not a range", () => {
  const card = toCardProduct({ ...product, skus: [product.skus[0], { ...product.skus[0], id: "c" }] });
  assert.equal(card.hasPriceRange, false);
});

test("the cover and each colour's first photo are used, a colour without photos gets null", () => {
  const card = toCardProduct(product);
  assert.equal(card.imageUrl, "cover.webp");
  assert.deepEqual(card.colors, [{ name: "Preto", imageUrl: "preto-1.webp" }, { name: "Cinza", imageUrl: null }]);
  assert.equal(toCardProduct({ ...product, imageUrls: [] }).imageUrl, null);
});

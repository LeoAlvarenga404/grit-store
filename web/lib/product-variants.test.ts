import assert from "node:assert/strict";
import { test } from "node:test";
import {
  galleryOf,
  isSingleOption,
  lowestPrice,
  productDetails,
  productHref,
  selectColor,
  sizeOptions,
  skusOfColor,
  variantLabel,
} from "./product-variants.ts";

const sku = (id: string, attributes: Record<string, string>, priceAmount = 219.9) => ({ id, attributes, priceAmount, priceCurrency: "BRL" });
const colors = [
  { name: "preto", imageUrls: ["preto-1.webp", "preto-2.webp"] },
  { name: "off white", imageUrls: [] },
];

test("selectColor takes the requested colour, else the first, else none", () => {
  assert.equal(selectColor(colors, "off white")?.name, "off white");
  assert.equal(selectColor(colors, "rosa")?.name, "preto");
  assert.equal(selectColor(colors, undefined)?.name, "preto");
  assert.equal(selectColor([], "preto"), null);
});

test("skusOfColor keeps the colour's SKUs, or every SKU when the product has no colours", () => {
  const skus = [sku("1", { cor: "preto", tamanho: "M" }), sku("2", { cor: "off white", tamanho: "M" })];

  assert.deepEqual(skusOfColor(skus, colors[1]).map((item) => item.id), ["2"]);
  assert.deepEqual(skusOfColor(skus, null).map((item) => item.id), ["1", "2"]);
});

test("sizeOptions lists only existing sizes from small to large", () => {
  const skus = [sku("gg", { cor: "preto", tamanho: "GG" }), sku("p", { cor: "preto", tamanho: "P" }), sku("m", { cor: "preto", tamanho: "M" })];

  assert.deepEqual(sizeOptions(skus).map((option) => option.label), ["P", "M", "GG"]);
});

test("a SKU without size is the single option, labelled by its other attributes when it has some", () => {
  assert.equal(isSingleOption(sizeOptions([sku("1", { cor: "preto" })])), true);
  assert.equal(isSingleOption(sizeOptions([sku("1", { cor: "preto", tamanho: "U" })])), false);
  assert.deepEqual(sizeOptions([sku("1", { voltagem: "110 V" })]).map((option) => option.label), ["110 V"]);
});

test("lowestPrice tells whether the SKUs have different prices", () => {
  assert.deepEqual(lowestPrice([sku("1", {}, 229.9), sku("2", {}, 219.9)]), { amount: 219.9, hasRange: true });
  assert.deepEqual(lowestPrice([sku("1", {}, 219.9), sku("2", {}, 219.9)]), { amount: 219.9, hasRange: false });
  assert.equal(lowestPrice([]), null);
});

test("galleryOf shows the colour's photos, else the product's", () => {
  assert.deepEqual(galleryOf(["capa.webp"], colors[0]), ["preto-1.webp", "preto-2.webp"]);
  assert.deepEqual(galleryOf(["capa.webp"], colors[1]), ["capa.webp"]);
  assert.deepEqual(galleryOf(["capa.webp"], null), ["capa.webp"]);
});

test("productDetails humanises keys and hides seller bookkeeping", () => {
  assert.deepEqual(productDetails({ "materiais-da-sola": "EVA", mpn: "123", marca: "GRIT" }), [
    { label: "Materiais da sola", value: "EVA" },
    { label: "Marca", value: "GRIT" },
  ]);
});

test("variantLabel names the colour, every word capitalised, and the size", () => {
  assert.equal(variantLabel("azul marinho", "M"), "Cor: Azul Marinho · Tamanho: M");
  assert.equal(variantLabel("preto/branco", null), "Cor: Preto/Branco");
  assert.equal(variantLabel(null, "42"), "Tamanho: 42");
  assert.equal(variantLabel(null, null), "");
});

test("productHref opens the product on the line's colour, encoded for the URL", () => {
  assert.equal(productHref("tenis-corrida", "off white"), "/products/tenis-corrida?cor=off%20white");
  assert.equal(productHref("tenis-corrida", null), "/products/tenis-corrida");
});

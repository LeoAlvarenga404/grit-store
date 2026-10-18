import assert from "node:assert/strict";
import { test } from "node:test";
import { catalogHref, priceRangeHref, readFilters, toggleAttribute, toSearchQuery } from "./catalog-href.ts";

test("readFilters keeps repeated attributes and drops malformed ones", () => {
  const filters = readFilters({ q: "tenis", attribute: ["cor:preto", "semdoispontos", ":vazio", "tamanho:M"], sort: "" });

  assert.deepEqual(filters, { q: "tenis", category: undefined, minPrice: undefined, maxPrice: undefined, attributes: ["cor:preto", "tamanho:M"], sort: undefined });
});

test("readFilters accepts a single attribute", () => {
  assert.deepEqual(readFilters({ attribute: "genero:Feminino" }).attributes, ["genero:Feminino"]);
});

test("catalogHref repeats attributes, applies changes and leaves relevance out", () => {
  const filters = readFilters({ category: "tenis", attribute: ["cor:preto"], sort: "price_asc" });

  assert.equal(catalogHref(filters, {}), "/catalog?category=tenis&attribute=cor%3Apreto&sort=price_asc");
  assert.equal(catalogHref(filters, { sort: "relevance", category: undefined }), "/catalog?attribute=cor%3Apreto");
  assert.equal(catalogHref(readFilters({}), {}), "/catalog");
});

test("toggleAttribute replaces the value of the same key and removes a repeated value", () => {
  const filters = readFilters({ attribute: ["cor:preto", "tamanho:M"] });

  assert.deepEqual(toggleAttribute(filters, "cor", "azul"), ["tamanho:M", "cor:azul"]);
  assert.deepEqual(toggleAttribute(filters, "cor", "preto"), ["tamanho:M"]);
  assert.deepEqual(toggleAttribute(filters, "cor-principal", "Preto"), ["cor:preto", "tamanho:M", "cor-principal:Preto"]);
});

test("priceRangeHref intersects the range with the current price filter", () => {
  assert.equal(priceRangeHref(readFilters({}), { minAmount: 100, maxAmount: 250, count: 3 }), "/catalog?minPrice=100&maxPrice=249.99");
  assert.equal(priceRangeHref(readFilters({ minPrice: "150" }), { minAmount: 100, maxAmount: 250, count: 3 }), "/catalog?minPrice=150&maxPrice=249.99");
  assert.equal(priceRangeHref(readFilters({ maxPrice: "1200" }), { minAmount: 1000, maxAmount: null, count: 1 }), "/catalog?minPrice=1000&maxPrice=1200.00");
  assert.equal(priceRangeHref(readFilters({}), { minAmount: 1000, maxAmount: null, count: 1 }), "/catalog?minPrice=1000");
});

test("toSearchQuery turns prices into numbers and asks for a full page", () => {
  const query = toSearchQuery(readFilters({ minPrice: "100", maxPrice: "249.99", attribute: "cor:preto", sort: "price_desc" }));

  assert.deepEqual(query, { q: undefined, category: undefined, minPrice: 100, maxPrice: 249.99, attribute: ["cor:preto"], sort: "price_desc", limit: 24 });
});

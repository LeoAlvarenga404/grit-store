import assert from "node:assert/strict";
import { test } from "node:test";
import { swatchFor } from "./color-swatches.ts";

test("swatchFor known single name returns its hex", () => {
  assert.equal(swatchFor("Preto"), "#111111");
});

test("swatchFor hyphenated name matches the spaced entry", () => {
  assert.equal(swatchFor("azul-claro"), swatchFor("Azul Claro"));
  assert.notEqual(swatchFor("azul-claro"), null);
});

test("swatchFor cap brim colour drops the aba prefix", () => {
  assert.equal(swatchFor("Preto/Aba Bordô"), "linear-gradient(135deg, #111111 50%, #5e1a24 50%)");
});

test("swatchFor unknown part returns null", () => {
  assert.equal(swatchFor("Mc/Pto"), null);
});

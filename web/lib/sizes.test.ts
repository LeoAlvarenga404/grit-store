import assert from "node:assert/strict";
import { test } from "node:test";
import { compareSizes } from "./sizes.ts";

test("letter sizes go from small to large", () => {
  assert.deepEqual(["GG", "P", "G", "M", "PP"].sort(compareSizes), ["PP", "P", "M", "G", "GG"]);
});

test("numeric sizes follow the letters in number order, other labels last", () => {
  assert.deepEqual(["U", "40 BR", "M", "38 BR", "P/M"].sort(compareSizes), ["M", "38 BR", "40 BR", "P/M", "U"]);
});

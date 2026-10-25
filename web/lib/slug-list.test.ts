import assert from "node:assert/strict";
import { test } from "node:test";
import { addSlug, parseSlugs, toggleSlug } from "./slug-list.ts";

test("a damaged or foreign value reads as an empty list", () => {
  assert.deepEqual(parseSlugs(null), []);
  assert.deepEqual(parseSlugs("not json"), []);
  assert.deepEqual(parseSlugs('{"a":1}'), []);
  assert.deepEqual(parseSlugs('["a",1,null,"b"]'), ["a", "b"]);
});

test("adding puts the slug first and moves a repeated one instead of duplicating it", () => {
  assert.deepEqual(addSlug(["a", "b"], "c", 5), ["c", "a", "b"]);
  assert.deepEqual(addSlug(["a", "b", "c"], "c", 5), ["c", "a", "b"]);
});

test("the list keeps only the newest entries", () => {
  assert.deepEqual(addSlug(["a", "b", "c"], "d", 3), ["d", "a", "b"]);
});

test("toggling adds a missing slug and removes a present one", () => {
  assert.deepEqual(toggleSlug(["a"], "b", 5), ["b", "a"]);
  assert.deepEqual(toggleSlug(["b", "a"], "b", 5), ["a"]);
});

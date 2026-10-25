import assert from "node:assert/strict";
import { test } from "node:test";
import { loginHref, safeReturnPath } from "./return-path.ts";

test("paths inside the site are kept, with their query", () => {
  assert.equal(safeReturnPath("/checkout"), "/checkout");
  assert.equal(safeReturnPath("/products/jaqueta?cor=Preto"), "/products/jaqueta?cor=Preto");
});

test("anything that could leave the site goes home", () => {
  for (const value of ["https://evil.example", "//evil.example", "/\\evil.example", "evil", "", null, undefined, 42, `/${"a".repeat(300)}`]) {
    assert.equal(safeReturnPath(value), "/");
  }
});

test("the login link carries the page, except home and the auth pages themselves", () => {
  assert.equal(loginHref("/checkout"), "/login?next=%2Fcheckout");
  assert.equal(loginHref("/orders?x=1"), "/login?next=%2Forders%3Fx%3D1");
  assert.equal(loginHref("/"), "/login");
  assert.equal(loginHref(null), "/login");
  assert.equal(loginHref("/login?next=%2Fbasket"), "/login");
  assert.equal(loginHref("//evil.example"), "/login");
});

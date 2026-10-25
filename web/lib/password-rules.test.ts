import assert from "node:assert/strict";
import { test } from "node:test";
import { isPasswordValid, passwordRules } from "./password-rules.ts";

const unmet = (password: string) => passwordRules.filter((rule) => !rule.isMet(password)).map((rule) => rule.key);

test("a password with every class and 8 characters is valid", () => {
  assert.equal(isPasswordValid("Aa1!aaaa"), true);
});

test("each missing class is reported on its own", () => {
  assert.deepEqual(unmet("SENHA-123!"), ["lowercase"]);
  assert.deepEqual(unmet("senha-123!"), ["uppercase"]);
  assert.deepEqual(unmet("Senha-forte!"), ["digit"]);
  assert.deepEqual(unmet("Senhaforte123"), ["symbol"]);
});

test("the length rule stops at 7 and at 129 characters", () => {
  assert.deepEqual(unmet("Aa1!aaa"), ["length"]);
  assert.equal(isPasswordValid(`Aa1!${"a".repeat(124)}`), true);
  assert.deepEqual(unmet(`Aa1!${"a".repeat(125)}`), ["length"]);
});

test("accented letters count as letters, not as symbols", () => {
  assert.deepEqual(unmet("Ação12345"), ["symbol"]);
});

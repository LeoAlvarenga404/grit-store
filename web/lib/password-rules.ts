// Mirrors RegisterValidator in Grit.Modules.Identity: the API is the authority, this only shows what is still missing.
export const passwordRules = [
  { key: "length", label: "8 a 128 caracteres", isMet: (password: string) => password.length >= 8 && password.length <= 128 },
  { key: "lowercase", label: "Letra minúscula", isMet: (password: string) => /\p{Ll}/u.test(password) },
  { key: "uppercase", label: "Letra maiúscula", isMet: (password: string) => /\p{Lu}/u.test(password) },
  { key: "digit", label: "Um número", isMet: (password: string) => /\p{Nd}/u.test(password) },
  { key: "symbol", label: "Um símbolo (! ? # @)", isMet: (password: string) => /[^\p{L}\p{Nd}]/u.test(password) },
];

export function isPasswordValid(password: string): boolean {
  return passwordRules.every((rule) => rule.isMet(password));
}

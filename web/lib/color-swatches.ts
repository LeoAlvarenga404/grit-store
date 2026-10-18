// Seller colour names are free text; common ones map to a swatch, anything else falls back to the colour's own photo.
const swatches: Record<string, string> = {
  preto: "#111111",
  branco: "#ffffff",
  "off white": "#f3efe6",
  cinza: "#9e9e9e",
  "cinza mescla": "#b9b9b9",
  "cinza chumbo": "#4a4a4a",
  chumbo: "#4a4a4a",
  "cinza claro": "#d0d0d0",
  grafite: "#3c3c3c",
  prata: "#c0c0c0",
  azul: "#1e5bd8",
  "azul marinho": "#1b2a4a",
  marinho: "#1b2a4a",
  "azul royal": "#2a4fd6",
  "azul petroleo": "#1f4e5f",
  "azul claro": "#8ec5ec",
  verde: "#2e7d32",
  "verde oliva": "#6b7140",
  "verde militar": "#4b5320",
  "verde limao": "#b5e61d",
  vermelho: "#c62828",
  vinho: "#6d1f2b",
  bordo: "#5e1a24",
  rosa: "#e8a0b4",
  pink: "#e8448a",
  roxo: "#6a3fa0",
  lilas: "#b39ddb",
  amarelo: "#f2c94c",
  laranja: "#ef7d22",
  bege: "#d8c7a8",
  areia: "#d9c9a3",
  marrom: "#6d4c41",
  caqui: "#b8a77a",
  dourado: "#c9a646",
};

function normalize(name: string): string {
  return name.normalize("NFD").replace(/[̀-ͯ]/g, "").toLowerCase().replace(/-/g, " ").trim();
}

// "Preto/Branco" (or a cap's "Preto/Aba Bordô") becomes a split swatch; null when any part is unknown.
export function swatchFor(colorName: string): string | null {
  const parts = normalize(colorName).split("/").map((part) => swatches[part.trim().replace(/^aba /, "")]);
  if (parts.some((part) => part === undefined)) return null;
  if (parts.length === 1) return parts[0];
  return `linear-gradient(135deg, ${parts[0]} 50%, ${parts[1]} 50%)`;
}

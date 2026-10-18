const letterSizes = ["PP", "P", "M", "G", "GG", "XG", "XGG"];

// Letter sizes from small to large, then numeric sizes ("38 BR") in number order, then anything else ("U", "P/M") by name.
export function compareSizes(left: string, right: string): number {
  return rank(left) - rank(right) || left.localeCompare(right, "pt-BR", { numeric: true });
}

function rank(size: string): number {
  const letter = letterSizes.indexOf(size.toUpperCase());
  if (letter >= 0) return letter;
  return /^\d/.test(size) ? 100 : 200;
}

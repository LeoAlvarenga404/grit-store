const currency = new Intl.NumberFormat("pt-BR", { style: "currency", currency: "BRL" });
const dateTime = new Intl.DateTimeFormat("pt-BR", { dateStyle: "short", timeStyle: "short", timeZone: "America/Sao_Paulo" });

export function formatPrice(amount: number): string {
  return currency.format(amount);
}

export function formatDateTime(isoDate: string): string {
  return dateTime.format(new Date(isoDate));
}

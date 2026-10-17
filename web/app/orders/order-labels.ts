const statusLabels: Record<string, string> = {
  Placed: "Processando",
  Confirmed: "Confirmado",
  Cancelled: "Cancelado",
};

const cancellationReasonLabels: Record<string, string> = {
  out_of_stock: "sem estoque",
  payment_declined: "pagamento recusado",
  customer_request: "a seu pedido",
};

export function statusLabel(status: string): string {
  return statusLabels[status] ?? status;
}

export function cancellationReasonLabel(reason: string): string {
  return cancellationReasonLabels[reason] ?? reason;
}

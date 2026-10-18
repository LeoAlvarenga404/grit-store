const statusLabels: Record<string, string> = {
  Placed: "Processando",
  Confirmed: "Confirmado",
  Shipped: "Enviado",
  Cancelled: "Cancelado",
};

const cancellationReasonLabels: Record<string, string> = {
  out_of_stock: "sem estoque",
  payment_declined: "pagamento recusado",
  customer_request: "a seu pedido",
};

export function orderStatusLabel(status: string): string {
  return statusLabels[status] ?? status;
}

export function cancellationReasonLabel(reason: string): string {
  return cancellationReasonLabels[reason] ?? reason;
}

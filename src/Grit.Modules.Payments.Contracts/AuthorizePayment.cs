namespace Grit.Modules.Payments.Contracts;

public sealed record AuthorizePayment(Guid OrderId, decimal Amount, string Currency, string PaymentToken);

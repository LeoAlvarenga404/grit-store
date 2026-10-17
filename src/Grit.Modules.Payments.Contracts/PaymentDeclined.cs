namespace Grit.Modules.Payments.Contracts;

public sealed record PaymentDeclined(Guid OrderId, string Reason);

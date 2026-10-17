namespace Grit.Modules.Payments.Contracts;

public sealed record RefundPayment(Guid OrderId, string AuthorizationId);

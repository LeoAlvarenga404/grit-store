namespace Grit.Modules.Payments.Contracts;

public sealed record PaymentAuthorized(Guid OrderId, string AuthorizationId);

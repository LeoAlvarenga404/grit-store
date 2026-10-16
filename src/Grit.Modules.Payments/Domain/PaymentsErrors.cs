using Grit.SharedKernel;

namespace Grit.Modules.Payments.Domain;

internal static class PaymentsErrors
{
    public static readonly Error Declined = new("payments.declined", "The payment was declined.", ErrorType.Validation);
}

using FluentValidation;

namespace Grit.Modules.Catalog.Features.AddSku;

internal sealed class AddSkuValidator : AbstractValidator<AddSkuCommand>
{
    public AddSkuValidator()
    {
        RuleFor(command => command.Attributes).Must(AttributesRule.IsValid).WithMessage(AttributesRule.Message);
        RuleFor(command => command.PriceAmount).GreaterThan(0);
        RuleFor(command => command.PriceCurrency).NotEmpty();
        RuleFor(command => command.Version).NotEmpty();
    }
}

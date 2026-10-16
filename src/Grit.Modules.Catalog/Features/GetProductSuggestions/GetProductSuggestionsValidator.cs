using FluentValidation;

namespace Grit.Modules.Catalog.Features.GetProductSuggestions;

internal sealed class GetProductSuggestionsValidator : AbstractValidator<GetProductSuggestionsQuery>
{
    public GetProductSuggestionsValidator()
    {
        RuleFor(query => query.Query)
            .NotEmpty()
            .Length(2, 50)
            .Must(TextRule.HasNoNullCharacter).WithMessage(TextRule.Message)
            .OverridePropertyName("q");
    }
}

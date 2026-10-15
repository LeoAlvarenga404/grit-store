using Grit.Modules.Catalog.Domain;
using Grit.Modules.Catalog.Infrastructure;
using Grit.SharedKernel;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Grit.Modules.Catalog.Features.CreateCategory;

internal sealed class CreateCategoryHandler(CatalogDbContext db)
    : IRequestHandler<CreateCategoryCommand, Result<CreateCategoryResponse>>
{
    public async Task<Result<CreateCategoryResponse>> Handle(CreateCategoryCommand command, CancellationToken cancellationToken)
    {
        var created = Category.Create(command.Name);
        if (created.IsFailure)
        {
            return created.Error;
        }

        var category = created.Value;
        if (await db.Categories.AnyAsync(existing => existing.Slug == category.Slug, cancellationToken))
        {
            return CatalogErrors.CategorySlugTaken;
        }

        db.Categories.Add(category);
        await db.SaveChangesAsync(cancellationToken);
        return new CreateCategoryResponse(category.Id.Value, category.Slug.Value);
    }
}

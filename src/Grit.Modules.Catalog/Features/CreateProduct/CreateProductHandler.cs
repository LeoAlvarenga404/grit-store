using Grit.Modules.Catalog.Domain;
using Grit.Modules.Catalog.Infrastructure;
using Grit.SharedKernel;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Grit.Modules.Catalog.Features.CreateProduct;

internal sealed class CreateProductHandler(CatalogDbContext db)
    : IRequestHandler<CreateProductCommand, Result<CreateProductResponse>>
{
    public async Task<Result<CreateProductResponse>> Handle(CreateProductCommand command, CancellationToken cancellationToken)
    {
        var categoryId = new CategoryId(command.CategoryId);
        if (!await db.Categories.AnyAsync(category => category.Id == categoryId, cancellationToken))
        {
            return CatalogErrors.CategoryNotFound;
        }

        var created = Product.Create(categoryId, command.Name, command.Description, command.Attributes);
        if (created.IsFailure)
        {
            return created.Error;
        }

        var product = created.Value;
        if (await db.Products.AnyAsync(existing => existing.Slug == product.Slug, cancellationToken))
        {
            return CatalogErrors.ProductSlugTaken;
        }

        db.Products.Add(product);
        await db.SaveChangesAsync(cancellationToken);
        return new CreateProductResponse(product.Id.Value, product.Slug.Value, db.VersionOf(product));
    }
}

using Grit.Modules.Catalog.Domain;
using Grit.Modules.Catalog.Infrastructure;
using Grit.SharedKernel;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Grit.Modules.Catalog.Features.UpdateProduct;

internal sealed class UpdateProductHandler(CatalogDbContext db)
    : IRequestHandler<UpdateProductCommand, Result<ProductVersionResponse>>
{
    public async Task<Result<ProductVersionResponse>> Handle(UpdateProductCommand command, CancellationToken cancellationToken)
    {
        var id = new ProductId(command.Id);
        var product = await db.Products.SingleOrDefaultAsync(product => product.Id == id, cancellationToken);
        if (product is null)
        {
            return CatalogErrors.ProductNotFound;
        }

        var categoryId = new CategoryId(command.CategoryId);
        if (!await db.Categories.AnyAsync(category => category.Id == categoryId, cancellationToken))
        {
            return CatalogErrors.CategoryNotFound;
        }

        product.Update(categoryId, command.Name, command.Description, command.Attributes);
        var version = await db.SaveProductAsync(product, command.Version, cancellationToken);
        if (version.IsFailure)
        {
            return version.Error;
        }

        return new ProductVersionResponse(version.Value);
    }
}

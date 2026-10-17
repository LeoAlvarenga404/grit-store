using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Grit.IntegrationTests;

public class OrderingSchemaTests(ApiFactory factory)
{
    // An order line copies the catalog product name, whose column holds at most 200 characters.
    [Fact]
    public async Task OrderLines_ProductName_HoldsAtMost200Characters()
    {
        var dataSource = factory.Services.GetRequiredService<NpgsqlDataSource>();
        await using var command = dataSource.CreateCommand(
            "SELECT character_maximum_length FROM information_schema.columns "
            + "WHERE table_schema = 'ordering' AND table_name = 'order_lines' AND column_name = 'product_name'");

        var length = await command.ExecuteScalarAsync(TestContext.Current.CancellationToken);

        length.ShouldBe(200);
    }
}

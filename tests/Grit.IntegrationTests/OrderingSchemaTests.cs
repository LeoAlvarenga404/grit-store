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

    // The line copies the catalog slug and SKU attribute values, both bounded at 100 characters there.
    [Theory]
    [InlineData("product_slug")]
    [InlineData("color")]
    [InlineData("size")]
    public async Task OrderLines_VariantColumns_AreNullableAndHoldAtMost100Characters(string column)
    {
        var dataSource = factory.Services.GetRequiredService<NpgsqlDataSource>();
        await using var command = dataSource.CreateCommand(
            "SELECT character_maximum_length, is_nullable FROM information_schema.columns "
            + "WHERE table_schema = 'ordering' AND table_name = 'order_lines' AND column_name = $1");
        command.Parameters.AddWithValue(column);
        await using var reader = await command.ExecuteReaderAsync(TestContext.Current.CancellationToken);

        (await reader.ReadAsync(TestContext.Current.CancellationToken)).ShouldBeTrue();
        reader.GetInt32(0).ShouldBe(100);
        reader.GetString(1).ShouldBe("YES");
    }
}

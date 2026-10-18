namespace Grit.MlImporter.Tests;

public class ImportOptionsTests
{
    [Fact]
    public void Parse_EveryOption_ReadsThem()
    {
        var options = ImportOptions.Parse(["--category", "Camping=barraca camping@MLB-TENTS", "--category", "Trilha = bastão", "--count", "3", "--output", "out"]);

        options.IsSuccess.ShouldBeTrue();
        options.Value.Categories.ShouldBe([new ImportCategory("Camping", "barraca camping", "MLB-TENTS"), new ImportCategory("Trilha", "bastão")]);
        options.Value.ProductsPerCategory.ShouldBe(3);
        options.Value.OutputDirectory.ShouldBe("out");
    }

    [Fact]
    public void Parse_OnlyACategory_UsesTheDefaults()
    {
        var options = ImportOptions.Parse(["--category", "Camping=barraca"]);

        options.Value.ProductsPerCategory.ShouldBe(10);
        options.Value.OutputDirectory.ShouldBe("seed");
    }

    [Fact]
    public void Parse_SameStoreCategoryTwice_KeepsBothSearches()
    {
        var options = ImportOptions.Parse(["--category", "Tênis=tenis corrida@MLB-SNEAKERS", "--category", "Tênis=tenis casual"]);

        options.Value.Categories.ShouldBe([new ImportCategory("Tênis", "tenis corrida", "MLB-SNEAKERS"), new ImportCategory("Tênis", "tenis casual")]);
    }

    [Theory]
    [InlineData]
    [InlineData("--count", "3")]
    [InlineData("--category", "Camping")]
    [InlineData("--category", "=barraca")]
    [InlineData("--category", "Camping=")]
    [InlineData("--category", "---=barraca")]
    [InlineData("--category", "Camping=barraca", "--count", "0")]
    [InlineData("--category", "Camping=barraca", "--count", "21")]
    [InlineData("--category", "Camping=barraca", "--count", "x")]
    [InlineData("--category", "Camping=barraca", "--output")]
    [InlineData("--category", "Camping=barraca", "--token", "abc")]
    [InlineData("--category", "Calçados=tênis", "--category", "calcados=bota")]
    [InlineData("--category", "Camping=barraca@")]
    [InlineData("--category", "Camping=@MLB-TENTS")]
    [InlineData("--category", "Camping=barraca@tents")]
    [InlineData("--category", "Camping=barraca@mlb-tents")]
    [InlineData("--category", "Camping=barraca@MLB-TENTS X")]
    public void Parse_InvalidArguments_Fails(params string[] args)
    {
        var options = ImportOptions.Parse(args);

        options.IsFailure.ShouldBeTrue();
        options.Error.Code.ShouldBe("importer.invalid_arguments");
    }
}

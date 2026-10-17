namespace Grit.MlImporter.Tests;

public class ImportOptionsTests
{
    [Fact]
    public void Parse_EveryOption_ReadsThem()
    {
        var options = ImportOptions.Parse(["--category", "Camping=barraca camping", "--category", "Trilha = bastão", "--count", "3", "--output", "out"]);

        options.IsSuccess.ShouldBeTrue();
        options.Value.Categories.ShouldBe([new ImportCategory("Camping", "barraca camping"), new ImportCategory("Trilha", "bastão")]);
        options.Value.ProductsPerCategory.ShouldBe(3);
        options.Value.OutputDirectory.ShouldBe("out");
    }

    [Fact]
    public void Parse_OnlyACategory_UsesTheDefaults()
    {
        var options = ImportOptions.Parse(["--category", "Camping=barraca"]);

        options.Value.ProductsPerCategory.ShouldBe(5);
        options.Value.OutputDirectory.ShouldBe("seed");
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
    public void Parse_InvalidArguments_Fails(params string[] args)
    {
        var options = ImportOptions.Parse(args);

        options.IsFailure.ShouldBeTrue();
        options.Error.Code.ShouldBe("importer.invalid_arguments");
    }
}

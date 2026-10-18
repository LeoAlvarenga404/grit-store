using Grit.Modules.Catalog.Infrastructure.Seed;

namespace Grit.MlImporter;

internal static class ImportCommand
{
    public const int Succeeded = 0;

    public const int InvalidArguments = 1;

    public const int MissingToken = 2;

    public const int RequestFailed = 3;

    // The real run waits 5, 10, 15 and 20 s between attempts (MercadoLivreClient).
    public static readonly TimeSpan DefaultRetryDelay = TimeSpan.FromSeconds(5);

    public static async Task<int> RunAsync(string[] args, string? accessToken, HttpMessageHandler handler, TextWriter output, TextWriter error, TimeSpan? retryDelay = null)
    {
        var options = ImportOptions.Parse(args);
        if (options.IsFailure)
        {
            await error.WriteLineAsync(options.Error.Message + Environment.NewLine + ImportOptions.Usage);
            return InvalidArguments;
        }

        var ownLine = await OwnLine.ReadAsync(options.Value.OutputDirectory, CancellationToken.None);
        if (OwnLine.CategoryTakenBy(options.Value, ownLine) is { } taken)
        {
            await error.WriteLineAsync($"'{taken}' is a category of the GRIT own line in {CatalogSeeder.OwnLineFolder}/catalog.json; import other store categories.");
            return InvalidArguments;
        }

        if (string.IsNullOrWhiteSpace(accessToken))
        {
            await error.WriteLineAsync("""
                The Mercado Livre access token is missing. Set it with:
                  dotnet user-secrets set "MercadoLivre:AccessToken" "<token>" --project tools/Grit.MlImporter
                """);
            return MissingToken;
        }

        using var http = new HttpClient(handler, disposeHandler: false);
        try
        {
            await new CatalogImporter(new MercadoLivreClient(http, accessToken, retryDelay ?? DefaultRetryDelay), output, ownLine: ownLine).ImportAsync(options.Value, CancellationToken.None);
            return Succeeded;
        }
        catch (HttpRequestException exception)
        {
            // 401 is the usual case: the token lasts six hours.
            await error.WriteLineAsync($"A Mercado Livre request failed, nothing was written: {exception.Message}");
            return RequestFailed;
        }
    }
}

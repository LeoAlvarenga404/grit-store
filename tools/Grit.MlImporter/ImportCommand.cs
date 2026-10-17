namespace Grit.MlImporter;

internal static class ImportCommand
{
    public const int Succeeded = 0;

    public const int InvalidArguments = 1;

    public const int MissingToken = 2;

    public const int RequestFailed = 3;

    public static async Task<int> RunAsync(string[] args, string? accessToken, HttpMessageHandler handler, TextWriter output, TextWriter error, TimeSpan? retryDelay = null)
    {
        var options = ImportOptions.Parse(args);
        if (options.IsFailure)
        {
            await error.WriteLineAsync(options.Error.Message + Environment.NewLine + ImportOptions.Usage);
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
            await new CatalogImporter(new MercadoLivreClient(http, accessToken, retryDelay ?? TimeSpan.FromSeconds(5)), output).ImportAsync(options.Value, CancellationToken.None);
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

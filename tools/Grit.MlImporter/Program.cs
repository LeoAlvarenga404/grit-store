using Grit.MlImporter;
using Microsoft.Extensions.Configuration;

var configuration = new ConfigurationBuilder()
    .AddUserSecrets(typeof(ImportCommand).Assembly, optional: true)
    .AddEnvironmentVariables()
    .Build();

using var handler = new HttpClientHandler();
return await ImportCommand.RunAsync(args, configuration["MercadoLivre:AccessToken"], handler, Console.Out, Console.Error);

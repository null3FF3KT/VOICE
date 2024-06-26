using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Configuration;

namespace VOICE.Configuration;
public static class ConfigurationService
{
    private static IConfiguration Configuration => new ConfigurationBuilder()
        .SetBasePath(AppContext.BaseDirectory)
        .AddJsonFile("appsettings.json")
		.AddJsonFile("local.settings.json")
        .Build();

    private static SecretClient GetSecretClient()
    {
        var keyVaultUrl = Configuration["AzureKeyVault:Url"];
        if (string.IsNullOrEmpty(keyVaultUrl))
        {
            throw new Exception("Azure Key Vault URL is missing in appsettings.json");
        }

        return new SecretClient(new Uri(keyVaultUrl), new DefaultAzureCredential());
    }

    public static string GetSpeechKey()
    {
        return GetSecretClient().GetSecret(Configuration["AzureSpeech:Secret"]).Value.Value;
    }

    public static string GetRegion()
    {
        return Configuration["AzureSpeech:Region"] ?? "eastus";
    }

    public static string GetOpenAiApiKey()
    {
        return GetSecretClient().GetSecret(Configuration["OpenAI:ApiKey"]).Value.Value;
    }

    public static string GetDatabaseConnectionString()
    {
        return GetSecretClient().GetSecret(Configuration["ConnectionStrings:DefaultConnection"]).Value.Value;
    }
		public static string GetBlobStorageConnectionString()
    {
        return Configuration["Values:AzureWebJobsStorage"];
    }
}
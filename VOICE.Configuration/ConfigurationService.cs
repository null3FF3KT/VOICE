using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Configuration;

namespace VOICE.Configuration;
public class ConfigurationService
{
	public IConfiguration Configuration { get; }
	public string SpeechKey { get; }
	public string Region { get; }
	public string OpenAiApiKey { get; }
	public string DatabaseConnectionString { get; }

	public ConfigurationService()
	{
		var builder = new ConfigurationBuilder()
				.SetBasePath(AppContext.BaseDirectory)
				.AddJsonFile("appsettings.json");
		Configuration = builder.Build();

		var keyVaultUrl = Configuration["AzureKeyVault:Url"];
		if (string.IsNullOrEmpty(keyVaultUrl))
		{
				throw new Exception("Azure Key Vault URL is missing in appsettings.json");
		}
		builder.AddAzureKeyVault(keyVaultUrl);

		var client = new SecretClient(new Uri(keyVaultUrl), new DefaultAzureCredential());
		SpeechKey = client.GetSecret(Configuration["AzureSpeech:Secret"]).Value.Value;
		Region = Configuration["AzureSpeech:Region"]?? "eastus";
		OpenAiApiKey = client.GetSecret(Configuration["OpenAI:ApiKey"]).Value.Value;
		DatabaseConnectionString = client.GetSecret(Configuration["ConnectionStrings:DefaultConnection"]).Value.Value;
	}
}
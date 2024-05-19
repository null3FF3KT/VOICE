using System;
using System.Threading.Tasks;
using Microsoft.CognitiveServices.Speech;
using Microsoft.Extensions.Configuration;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;


namespace VOICE
{
  class Program
  {
    static async Task Main(string[] args)
    {
      // Build configuration
      var builder = new ConfigurationBuilder();
      builder
      .SetBasePath(Directory.GetCurrentDirectory())
      .AddJsonFile("appsettings.json");
      var configuration = builder.Build();
      var url = configuration["AzureSpeech:Url"];
      if (string.IsNullOrEmpty(url))
      {
        Console.WriteLine("Azure Speech Service URL is missing in appsettings.json");
        return;
      }
      builder.AddAzureKeyVault(url);
      var client = new SecretClient(new Uri(url), new DefaultAzureCredential());
      var secret = client.GetSecret(configuration["AzureSpeech:Secret"]);
      var region = configuration["AzureSpeech:Region"];

      var speechConfig = SpeechConfig.FromSubscription(secret.Value.Value, region);

      using var recognizer = new SpeechRecognizer(speechConfig);
      Console.WriteLine("Say something...");

      var result = await recognizer.RecognizeOnceAsync();
      if (result.Reason == ResultReason.RecognizedSpeech)
      {
        Console.WriteLine($"You said: {result.Text}");
      }
      else if (result.Reason == ResultReason.NoMatch)
      {
        Console.WriteLine("No speech could be recognized.");
      }
      else if (result.Reason == ResultReason.Canceled)
      {
        var cancellation = CancellationDetails.FromResult(result);
        Console.WriteLine($"CANCELED: Reason={cancellation.Reason}");
        Console.WriteLine($"CANCELED: ErrorDetails={cancellation.ErrorDetails}");
      }
    }
  }
}

using VOICE.Services;


namespace VOICE
{
  class Program
  {
    static async Task Main(string[] args)
    {
      var configService = new ConfigurationService();
      var speechService = new SpeechService(configService.SpeechKey, configService.Region);
      var openAiService = new OpenAIService(configService.OpenAiApiKey);
      bool continueRunning = true;

      while (continueRunning)
      {
        var recognizedText = await speechService.RecognizeSpeechAsync();
        if (!string.IsNullOrEmpty(recognizedText))
        {
          var chatResponse = await openAiService.GetChatGPTResponse(recognizedText);
          await speechService.SynthesizeSpeechAsync(chatResponse);
        }
        Console.WriteLine("Do you want to go again? (yes to continue, any other key to quit):");
        var userInput = Console.ReadLine();
        if (!userInput.Equals("yes", StringComparison.OrdinalIgnoreCase))
        {
          continueRunning = false;
        }
      }
      Console.WriteLine("Goodbye!");
    }
  }
}

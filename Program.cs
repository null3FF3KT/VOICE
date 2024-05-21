using VOICE.Models;
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
      List<Message> messages = new List<Message>();


      while (continueRunning)
      {
        var recognizedText = await speechService.RecognizeSpeechAsync();
        messages.Add(new Message { content = recognizedText, role = nameof(Roles.user) });
        if (!string.IsNullOrEmpty(recognizedText))
        {
          var chatResponse = await openAiService.GetChatGPTResponse(messages);
          messages.Add(new Message { content = chatResponse, role = nameof(Roles.assistant) });
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

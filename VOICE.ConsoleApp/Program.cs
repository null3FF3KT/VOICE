using VOICE.Configuration;
using VOICE.Services;
using VOICE.Models;

namespace VOICE.ConsoleApp
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var configService = new ConfigurationService();
            var speechService = new CognitiveServicesSpeech(configService.SpeechKey, configService.Region);
            var openAiService = new OpenAIService(configService.OpenAiApiKey);
            var conversation = new Conversation();

            bool continueRunning = true;

            while (continueRunning)
            {
                var recognizedText = await speechService.RecognizeSpeechAsync();
                if (!string.IsNullOrEmpty(recognizedText))
                {
                    conversation.AddUserMessage(recognizedText);

                    var chatResponse = await openAiService.GetChatGPTResponse(conversation.GetHistory());
                    conversation.AddBotMessage(chatResponse);
                    var speechTask = speechService.SynthesizeSpeechAsync(chatResponse);

                    Console.WriteLine("\nPress 'c' to cancel the speech.\n");
                    while (!speechTask.IsCompleted)
                    {
                        if (Console.KeyAvailable && Console.ReadKey(true).Key == ConsoleKey.C)
                        {
                            speechService.CancelSpeech();
                            break;
                        }
                    }

                    await speechTask;
                }

                Console.WriteLine("To quit type 'exit' and press return (To continue, just press return): ");
                var userInput = Console.ReadLine();
                if (userInput.Equals("exit", StringComparison.OrdinalIgnoreCase))
                {
                    continueRunning = false;
                }
            }

            Console.WriteLine("Goodbye!");
        }
    }
}

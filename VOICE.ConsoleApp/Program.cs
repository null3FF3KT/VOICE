using VOICE.Configuration;
using VOICE.Services;
using VOICE.Data.Context;
using VOICE.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using VOICE.Data.Models;

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
            var host = CreateHostBuilder(args).Build();
            ApplyMigrations(host);

            var hostTask = host.RunAsync();

            var scope = host.Services.CreateScope();
            var conversationRepository = scope.ServiceProvider.GetRequiredService<ConversationRepository>();


            conversation.AddSystemMessage("You are a deliberate and concise chatbot.");
            conversation.AddSystemMessage("Take your time to think about your responses.");
            conversation.AddSystemMessage("Cite your sources.");
            conversation.AddSystemMessage("Be polite and respectful.");
            conversation.AddSystemMessage("Do not lie.");

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
                if (!string.IsNullOrEmpty(userInput) && userInput.Equals("exit", StringComparison.OrdinalIgnoreCase))
                {
                    continueRunning = false;
                }
            }

            // need to map the conversation object to the Conversation model.  History = Messages
            var dataConversation = new VOICE.Data.Models.Conversation();
            
            foreach (var message in conversation.GetHistory())
            {
                dataConversation.Messages.Add(new VOICE.Data.Models.Message
                {
                    role = message.role,
                    content = message.content
                });
            }   
            await conversationRepository.AddConversationAsync(dataConversation);
            Console.WriteLine("Goodbye!");
            await host.StopAsync();
            await hostTask;
        }
        public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureServices((hostContext, services) =>
            {
                services.AddDbContext<DataContext>(options =>
                    options.UseMySql("Server=localhost;port=3306;Database=VoiceDb;User Id=root;Password=mark77Paris!;", 
                    new MySqlServerVersion(new Version(8, 0, 0))));
                services.AddScoped<ConversationRepository>();
            });

        private static void ApplyMigrations(IHost host)
        {
            using var scope = host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<DataContext>();
            db.Database.Migrate();
        }
    }
}

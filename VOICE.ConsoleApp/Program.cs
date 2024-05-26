using VOICE.Configuration;
using VOICE.Services;
using VOICE.Data.Context;
using VOICE.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace VOICE.ConsoleApp
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var configService = new ConfigurationService();
            var speechService = new CognitiveServicesSpeech(configService.SpeechKey, configService.Region);
            var openAiService = new OpenAIService(configService.OpenAiApiKey);
            var connectionString = configService.DatabaseConnectionString;

            var host = CreateHostBuilder(args, connectionString).Build();
            ApplyMigrations(host);

            var hostTask = host.RunAsync();

            var scope = host.Services.CreateScope();
            var conversationRepository = scope.ServiceProvider.GetRequiredService<ConversationRepository>();

            var speak = new Speak(new Conversation(), openAiService, conversationRepository, speechService);
            await speak.RunAsync();

            await host.StopAsync();
            await hostTask;
        }
        public static IHostBuilder CreateHostBuilder(string[] args, string connectionString) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureServices((hostContext, services) =>
            {
                services.AddDbContext<DataContext>(options =>
                    options.UseMySql(connectionString, 
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

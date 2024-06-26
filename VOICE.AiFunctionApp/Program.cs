using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using VOICE.Configuration;
using VOICE.Services;
using VOICE.AiFunctionApp;
using VOICE.Data.Context;
using Microsoft.EntityFrameworkCore;
using VOICE.Data;

var host = new HostBuilder()
        .ConfigureFunctionsWorkerDefaults()
        .ConfigureServices(services => {
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();

        var openAiApiKey = ConfigurationService.GetOpenAiApiKey();
        var speechKey = ConfigurationService.GetSpeechKey();
        var region = ConfigurationService.GetRegion();
        var connectionString = ConfigurationService.GetDatabaseConnectionString();

        services.AddSingleton(new OpenAIService(openAiApiKey));
        services.AddSingleton(new CognitiveServicesSpeech(speechKey, region));
        services.AddSingleton<SpeechFunction>();

        services.AddDbContext<DataContext>(options =>
                    options.UseMySql(connectionString, 
                    new MySqlServerVersion(new Version(8, 0, 0))));
                services.AddScoped<ConversationRepository>();
    })
    .Build();

host.Run();

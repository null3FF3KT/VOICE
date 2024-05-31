using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using VOICE.Configuration;
using VOICE.Services;
using VOICE.Data;
using VOICE.AiFunctionApp;

var host = new HostBuilder()
        .ConfigureServices(services => {
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();
        services.AddScoped<ConfigurationService>();
        services.AddScoped<OpenAIService>();
        services.AddScoped<CognitiveServicesSpeech>();
        services.AddScoped<ConversationRepository>();
        services.AddScoped<SpeechFunction>();
    })
    .Build();

host.Run();

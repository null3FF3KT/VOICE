﻿using VOICE.Configuration;
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

    public class Conversation
    {
        public List<Message> History { get; private set; }

        public Conversation()
        {
            History = new List<Message>();
        }

        public void AddUserMessage(string message)
        {
            History.Add(new Message { role = "user", content = message });
        }

        public void AddBotMessage(string message)
        {
            History.Add(new Message { role = "assistant", content = message });
        }

        public List<Message> GetHistory()
        {
            return History;
        }
    }
}

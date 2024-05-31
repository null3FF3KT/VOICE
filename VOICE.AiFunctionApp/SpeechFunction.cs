using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using VOICE.Configuration;
using VOICE.Services;
using VOICE.Data;

namespace VOICE.AiFunctionApp
{
    public class SpeechFunction
    {
         private readonly HttpClient httpClient = new HttpClient();
	    private readonly ConfigurationService _configService;
        private readonly OpenAIService _openAiService;
        private readonly CognitiveServicesSpeech _speechService;
        private readonly ConversationRepository _conversationRepository;
        //private List<VOICE.Data.Models.Conversation> _conversations;
        private Conversation _conversation = new Conversation();
        private readonly ILogger<SpeechFunction> _logger;

        public SpeechFunction(ConfigurationService configurationService, OpenAIService openAiService, CognitiveServicesSpeech speechService, ConversationRepository conversationRepository, ILogger<SpeechFunction> logger)
        {
            _configService = configurationService;
            _openAiService = openAiService;
            _speechService = speechService;
            _conversationRepository = conversationRepository;
            _logger = logger;
        }

        [Function("ProcessAudio")]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");

        try
            {
                if (req.Form.Files.Count == 0)
                {
                    return new BadRequestObjectResult("No audio file uploaded.");
                }

                var audioFile = req.Form.Files[0];

                // Because this is a serverless function, we need to get the conversation from the database
                // in order to continue the conversation. Will circle back to this later.

                // _conversations = _conversationRepository.GetAllConversationsAsync().Result.ToList();
                // if (_conversations.Count > 0)
                // {
                //     _conversation = new Conversation(_conversations.Last());
                // }


                // Speech-to-Text
                string transcribedText;
                using (var audioStream = audioFile.OpenReadStream())
                {
                    var pushStream = AudioInputStream.CreatePushStream();
                    byte[] buffer = new byte[4096];
                    int bytesRead;
                    while ((bytesRead = audioStream.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        pushStream.Write(buffer, bytesRead);
                    }
                    pushStream.Close();

                    using var audioInput = AudioConfig.FromStreamInput(pushStream);
                    using var recognizer = new SpeechRecognizer(_speechService.GetSpeechConfig(), audioInput);
                    var result = await recognizer.RecognizeOnceAsync();
                    if (result.Reason != ResultReason.RecognizedSpeech)
                    {
                        return new BadRequestObjectResult("Speech recognition failed.");
                    }
                    transcribedText = result.Text;
                }

                
                _conversation.AddUserMessage(transcribedText);
                            

                // Query OpenAI's ChatGPT
                var openAiResponseText = await _openAiService.GetChatGPTResponse(_conversation.GetHistory());//(transcribedText, _openAiService.);
                _conversation.AddBotMessage(openAiResponseText);
                var dataConversation = await ConvertConversation();
                await _conversationRepository.UpdateConversationAsync(dataConversation);
                // Text-to-Speech
                byte[] audioData;
                using (var synthesizer = new SpeechSynthesizer(_speechService.GetSpeechConfig()))
                {
                    var result = await synthesizer.SpeakTextAsync(openAiResponseText);
                    if (result.Reason != ResultReason.SynthesizingAudioCompleted)
                    {
                        return new BadRequestObjectResult("Text-to-speech synthesis failed.");
                    }
                    audioData = result.AudioData;
                }

                // Return audio file to frontend
                return new FileContentResult(audioData, "audio/wav")
                {
                    FileDownloadName = "response.wav"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing audio.");
                return new StatusCodeResult(500);
            }
        }

        private async Task<VOICE.Data.Models.Conversation> ConvertConversation()
        {
            _conversation.AddUserMessage("Describe our conversation in five words or less.");
            var nameConversation = await _openAiService.GetChatGPTResponse(_conversation.GetHistory());
            _conversation.RemoveLastMessage();
            var dataConversation = new VOICE.Data.Models.Conversation
            {
                name = nameConversation,
                created = DateTime.Now
            };
            foreach (var message in _conversation.GetHistory())
            {
                dataConversation.Messages.Add(new VOICE.Data.Models.Message
                {
                    role = message.role,
                    content = message.content
                });
            }
            return dataConversation;
        }
    }
}

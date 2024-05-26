using VOICE.Services;
using VOICE.Data;
using Microsoft.Extensions.Hosting;

public class Speak
{
	private readonly Conversation _conversation;
	private readonly OpenAIService _openAIService;
	private readonly ConversationRepository _conversationRepository;
	private readonly CognitiveServicesSpeech _cognitiveServicesSpeech;
	public Speak(Conversation conversation, OpenAIService openAIService, ConversationRepository conversationRepository, CognitiveServicesSpeech cognitiveServicesSpeech)
	{
		_conversation = conversation;
		_openAIService = openAIService;
		_conversationRepository = conversationRepository;
		_cognitiveServicesSpeech = cognitiveServicesSpeech;
	}

	public async Task<int> RunAsync()
    {
        CreatePoliteAssistant();

				bool continueRunning = true;

            while (continueRunning)
            {
                var recognizedText = await _cognitiveServicesSpeech.RecognizeSpeechAsync();
                if (!string.IsNullOrEmpty(recognizedText))
                {
                    _conversation.AddUserMessage(recognizedText);

                    var chatResponse = await _openAIService.GetChatGPTResponse(_conversation.GetHistory());
                    _conversation.AddBotMessage(chatResponse);
                    var speechTask = _cognitiveServicesSpeech.SynthesizeSpeechAsync(chatResponse);

                    Console.WriteLine("\nPress 'c' to cancel the speech.\n");
                    while (!speechTask.IsCompleted)
                    {
                        if (Console.KeyAvailable && Console.ReadKey(true).Key == ConsoleKey.C)
                        {
                            _cognitiveServicesSpeech.CancelSpeech();
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

        VOICE.Data.Models.Conversation dataConversation = await ConvertConversation();
        await _conversationRepository.AddConversationAsync(dataConversation);
        Console.WriteLine("Goodbye!");
				return 0;
    }

    private void CreatePoliteAssistant()
    {
        _conversation.AddSystemMessage("You are a deliberate and concise chatbot.");
        _conversation.AddSystemMessage("Take your time to think about your responses.");
        _conversation.AddSystemMessage("Cite your sources.");
        _conversation.AddSystemMessage("Be polite and respectful.");
        _conversation.AddSystemMessage("Do not lie.");
        _conversation.AddSystemMessage("Describe our conversation in five words or less.");
    }

    private async Task<VOICE.Data.Models.Conversation> ConvertConversation()
    {
				_conversation.AddUserMessage("Describe our conversation in five words or less.");
        var nameConversation = await _openAIService.GetChatGPTResponse(_conversation.GetHistory());
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
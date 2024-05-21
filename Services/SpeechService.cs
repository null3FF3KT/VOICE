using Microsoft.CognitiveServices.Speech;

namespace VOICE.Services
{
    public class SpeechService
    {
        private readonly SpeechConfig _speechConfig;

        public SpeechService(string speechKey, string region)
        {
            _speechConfig = SpeechConfig.FromSubscription(speechKey, region);
        }

        public async Task<string> RecognizeSpeechAsync()
        {
            using var recognizer = new SpeechRecognizer(_speechConfig);
            Console.WriteLine("Say something...");

            var result = await recognizer.RecognizeOnceAsync();
            if (result.Reason == ResultReason.RecognizedSpeech)
            {
                Console.WriteLine($"You said: {result.Text}");
                return result.Text;
            }
            else if (result.Reason == ResultReason.NoMatch)
            {
                Console.WriteLine("No speech could be recognized.");
								return "Would you like to continue?";
            }
            else if (result.Reason == ResultReason.Canceled)
            {
                var cancellation = CancellationDetails.FromResult(result);
                Console.WriteLine($"CANCELED: Reason={cancellation.Reason}");
                Console.WriteLine($"CANCELED: ErrorDetails={cancellation.ErrorDetails}");
								return "Would you like to continue?";
            }
						return "Something went wrong. Please try again.";
        }

        public async Task SynthesizeSpeechAsync(string text)
        {
            using var synthesizer = new SpeechSynthesizer(_speechConfig);
						Console.WriteLine($"ChatGPT said: {text}");
            var result = await synthesizer.SpeakTextAsync(text);

            if (result.Reason == ResultReason.SynthesizingAudioCompleted)
            {
                Console.WriteLine("That's the end.");
            }
            else if (result.Reason == ResultReason.Canceled)
            {
                var cancellation = SpeechSynthesisCancellationDetails.FromResult(result);
                Console.WriteLine($"CANCELED: Reason={cancellation.Reason}");
                Console.WriteLine($"CANCELED: ErrorDetails={cancellation.ErrorDetails}");
            }
        }
    }
}

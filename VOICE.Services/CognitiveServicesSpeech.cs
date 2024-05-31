using Microsoft.CognitiveServices.Speech;

namespace VOICE.Services
{
public class CognitiveServicesSpeech
{
		private readonly SpeechConfig _speechConfig;
		private SpeechSynthesizer _synthesizer;
		public CognitiveServicesSpeech(string speechKey, string region)
		{
			_speechConfig = SpeechConfig.FromSubscription(speechKey, region);
			_synthesizer = new SpeechSynthesizer(_speechConfig);
		}

		public SpeechConfig GetSpeechConfig()
		{
			return _speechConfig;
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
			Console.WriteLine($"ChatGPT said: {text}");
			var result = await _synthesizer.SpeakTextAsync(text);

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
		public void CancelSpeech()
        {
            _synthesizer.StopSpeakingAsync();
        }
	}
}
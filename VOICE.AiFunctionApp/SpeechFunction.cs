using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using VOICE.Services;
using VOICE.Data;
using Microsoft.Azure.Functions.Worker.Http;
using System.Net;
using System.Diagnostics;
using Microsoft.AspNetCore.WebUtilities;
using System.Net.Http.Headers;

namespace VOICE.AiFunctionApp
{
    public class SpeechFunction
    {
        private readonly OpenAIService _openAiService;
        private readonly CognitiveServicesSpeech _speechService;
        private readonly ConversationRepository _conversationRepository;
        private readonly ILogger<SpeechFunction> _logger;
        private Conversation _conversation = new Conversation();

        public SpeechFunction(OpenAIService openAiService, CognitiveServicesSpeech speechService, ConversationRepository conversationRepository, ILogger<SpeechFunction> logger)
        {
            _openAiService = openAiService;
            _speechService = speechService;
            _conversationRepository = conversationRepository;
            _logger = logger;
        }

        [Function("ProcessAudio")]
        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Function, "post", Route = "ProcessAudio")] HttpRequestData req)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");
            _logger.LogInformation($"Request content type: {req.Headers.GetValues("Content-Type").FirstOrDefault()}");
            try
            {
                 // Read the request body as a stream
                using var audioStream = await ExtractAudioFileFromRequest(req);
                _logger.LogInformation($"Extracted audio stream length: {audioStream.Length}");

                // Convert WebM to WAV
                byte[] wavData = ConvertWebMToWav(audioStream);

                // Recognize speech
                string recognizedText = await RecognizeSpeechAsync(wavData);

                // Save recognized text to conversation history
                _conversation.AddUserMessage(recognizedText);

                // Get ChatGPT response
                string chatGptResponse = await _openAiService.GetChatGPTResponse(_conversation.GetHistory());

                // Save ChatGPT response to conversation history
                _conversation.AddBotMessage(chatGptResponse);

                // Save conversation to database
                var dataConversation = await MapToDataModelConversationAsync(_conversation);
                await _conversationRepository.AddConversationAsync(dataConversation);

                // Convert ChatGPT response to speech
                byte[] responseSpeechWav = await TextToSpeechAsync(chatGptResponse);

                // Create response and write WAV data
                var response = req.CreateResponse(HttpStatusCode.OK);
                response.Headers.Add("Content-Type", "audio/wav");
                await response.Body.WriteAsync(responseSpeechWav, 0, responseSpeechWav.Length);

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing audio request");
                var response = req.CreateResponse(HttpStatusCode.InternalServerError);
                await response.WriteStringAsync("An error occurred while processing the request.");
                return response;
            }
        }

        private byte[] ConvertWebMToWav(Stream webmStream)
        {
            // Ensure FFmpeg is initialized
            var ffmpegPath = FFmpegBinariesHelper.GetFFmpegPath();

            // Create temporary file paths
            var tempWebMPath = Path.GetTempFileName();
            var tempWavPath = Path.ChangeExtension(tempWebMPath, "wav");


            try
            {
                // Log the size of the incoming stream
                _logger.LogInformation($"Incoming stream length: {webmStream.Length}");

                using (var fileStream = File.Create(tempWebMPath))
                {
                    webmStream.CopyTo(fileStream);
                }

                // Log the size of the saved file
                _logger.LogInformation($"Saved WebM file size: {new FileInfo(tempWebMPath).Length}");

                if (!IsWebMFile(tempWebMPath))
                {
                    throw new InvalidDataException("The input file does not appear to be a valid WebM file.");
                }

                var startInfo = new ProcessStartInfo
                {
                    FileName = ffmpegPath,
                    // Add -v verbose for more detailed FFmpeg output
                    Arguments = $"-v verbose -f webm -i \"{tempWebMPath}\" -acodec pcm_s16le -ar 44100 \"{tempWavPath}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                string output = "";
                string error = "";

                using (var process = Process.Start(startInfo))
                {
                    if (process == null)
                    {
                        throw new ApplicationException("FFmpeg process failed to start.");
                    }
                    output = process.StandardOutput.ReadToEnd();
                    error = process.StandardError.ReadToEnd();
                    process.WaitForExit();

                    if (process.ExitCode != 0)
                    {
                        _logger.LogError($"FFmpeg output: {output}");
                        _logger.LogError($"FFmpeg error: {error}");
                        throw new Exception($"FFmpeg conversion failed. Exit code: {process.ExitCode}");
                    }
                }

                if (File.Exists(tempWavPath))
                {
                    _logger.LogInformation($"WAV file created. Size: {new FileInfo(tempWavPath).Length}");
                    return File.ReadAllBytes(tempWavPath);
                }
                else
                {
                    throw new FileNotFoundException("WAV file was not created by FFmpeg.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in ConvertWebMToWav: {ex.Message}");
                throw;
            }
            finally
            {
                if (File.Exists(tempWebMPath))
                    File.Delete(tempWebMPath);
                if (File.Exists(tempWavPath))
                    File.Delete(tempWavPath);
            }
        }

        private bool IsWebMFile(string filePath)
        {
            byte[] webmSignature = new byte[] { 0x1A, 0x45, 0xDF, 0xA3 };
            using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                byte[] buffer = new byte[4];
                fileStream.Read(buffer, 0, 4);
                return buffer.SequenceEqual(webmSignature);
            }
        }

        private async Task<string> RecognizeSpeechAsync(byte[] wavData)
        {
            var speechConfig = _speechService.GetSpeechConfig();
            using var audioInputStream = AudioInputStream.CreatePushStream(AudioStreamFormat.GetWaveFormatPCM(16000, 16, 1));
            using var audioConfig = AudioConfig.FromStreamInput(audioInputStream);
            using var recognizer = new SpeechRecognizer(speechConfig, audioConfig);

            audioInputStream.Write(wavData);
            audioInputStream.Close();

            var result = await recognizer.RecognizeOnceAsync();
            return result.Text;
        }

        private async Task<byte[]> TextToSpeechAsync(string text)
        {
            var speechConfig = _speechService.GetSpeechConfig();
            using var synthesizer = new SpeechSynthesizer(speechConfig);
            var result = await synthesizer.SpeakTextAsync(text);
            return result.AudioData;
        }

        private async Task<Stream> ExtractAudioFileFromRequest(HttpRequestData req)
        {
            if (!req.Headers.Contains("Content-Type"))
            {
                throw new InvalidOperationException("Content-Type header is missing.");
            }
            var boundary = GetBoundary(req.Headers.GetValues("Content-Type").FirstOrDefault());
            var reader = new MultipartReader(boundary, req.Body);
            var section = await reader.ReadNextSectionAsync();
            
            while (section != null)
            {
                var contentDisposition = ContentDispositionHeaderValue.Parse(section.ContentDisposition);
                if (contentDisposition.DispositionType.Equals("form-data") &&
                    !string.IsNullOrEmpty(contentDisposition.FileName.ToString()))
                {
                    // This is the file we're looking for
                    var memoryStream = new MemoryStream();
                    await section.Body.CopyToAsync(memoryStream);
                    memoryStream.Position = 0;
                    return memoryStream;
                }
                section = await reader.ReadNextSectionAsync();
            }
            
            throw new InvalidOperationException("No file found in the request.");
        }

        private string GetBoundary(string contentType)
        {
            var elements = contentType.Split(';');
            var boundaryElement = elements.FirstOrDefault(e => e.TrimStart().StartsWith("boundary="));
            return boundaryElement?.Substring(boundaryElement.IndexOf('=') + 1).Trim('"');
        }

        private async Task<VOICE.Data.Models.Conversation> MapToDataModelConversationAsync(Conversation conversation)
        {
            // Generate a name for the conversation using ChatGPT
            conversation.AddSystemMessage("Describe our conversation in five words or less.");
            var conversationName = await _openAiService.GetChatGPTResponse(conversation.GetHistory());
            conversation.RemoveLastMessage(); // Remove the system message we just added

            var dataConversation = new VOICE.Data.Models.Conversation
            {
                name = conversationName.Trim(),       
                created = DateTime.Now
            };

            foreach (var message in conversation.GetHistory())
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
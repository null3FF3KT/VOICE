using System.Text;
using Newtonsoft.Json;
using VOICE.Models;

namespace VOICE.Services
{
    public class OpenAIService
    {
        private readonly string _apiKey;

        public OpenAIService(string apiKey)
        {
            _apiKey = apiKey;
        }

        public async Task<string> GetChatGPTResponse(string userInput)
        {
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");
                var requestBody = new
                {
                    model = "gpt-4o",
                    messages = new Message[] { new Message { role = "user", content = userInput } },
                    max_tokens = 1000
                };
                var content = new StringContent(JsonConvert.SerializeObject(requestBody), Encoding.UTF8, "application/json");
                var response = await client.PostAsync("https://api.openai.com/v1/chat/completions", content);
                response.EnsureSuccessStatusCode();

                var responseString = await response.Content.ReadAsStringAsync();
                dynamic responseJson = JsonConvert.DeserializeObject(responseString);
                string answer = responseJson.choices[0].message.content;
                return answer;
            }
        }
    }
}

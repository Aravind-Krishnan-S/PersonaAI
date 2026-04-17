using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace CharacterChat.Services
{
    public class EmbeddingService
    {
        private readonly HttpClient _httpClient;
        private readonly LlmConfigService _config;

        public EmbeddingService(LlmConfigService config)
        {
            _config = config;
            _httpClient = new HttpClient();
        }

        public async Task<float[]> GetEmbeddingAsync(string text)
        {
            string url = _config.BaseUrl.TrimEnd('/') + "/embeddings";
            
            var requestBody = new
            {
                model = _config.EmbeddingsModelName,
                input = text
            };

            var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
            
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _config.ApiKey);

            var response = await _httpClient.PostAsync(url, content);
            response.EnsureSuccessStatusCode();

            var responseString = await response.Content.ReadAsStringAsync();
            using var document = JsonDocument.Parse(responseString);
            
            var dataArray = document.RootElement.GetProperty("data")[0].GetProperty("embedding");
            float[] embedding = new float[dataArray.GetArrayLength()];
            
            for (int i = 0; i < embedding.Length; i++)
            {
                embedding[i] = dataArray[i].GetSingle();
            }

            return embedding;
        }
    }
}

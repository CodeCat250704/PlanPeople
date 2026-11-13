using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;

namespace PlanPeople.AI;

public class OpenAiClient
{
    private readonly HttpClient _httpClient;

    public OpenAiClient()
    {
        _httpClient = new HttpClient();
        // 统一在这里设置超时，避免每次请求时修改导致异常
        _httpClient.Timeout = TimeSpan.FromSeconds(60); 
    }

    public async Task<string> SendMessageAsync(string baseUrl, string apiKey, string model, string systemPrompt, string userMessage, int timeoutSeconds = 60)
    {
        var url = $"{baseUrl.TrimEnd('/')}/chat/completions";
        var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        var payload = new
        {
            model = model,
            messages = new[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = userMessage }
            },
            temperature = 0.2
        };

        request.Content = JsonContent.Create(payload);

        try
        {
            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var content = doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();

            return content ?? "AI 没有返回内容。";
        }
        catch (Exception ex)
        {
            return $"[调用失败] {ex.Message}";
        }
    }
}
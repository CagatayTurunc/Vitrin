using System.Net.Http.Json;
using System.Text.Json;
using Vitrin.Ai.Application.Services;
using Microsoft.Extensions.Configuration;

namespace Vitrin.Ai.Infrastructure.Services;

public class GeminiAiAnalyzerService : IAiAnalyzerService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;

    public GeminiAiAnalyzerService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _apiKey = configuration["Gemini:ApiKey"] ?? string.Empty;
    }

    public async Task<(string Summary, string[] Tags)> AnalyzeProductTextAsync(string name, string description, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            // Fallback if API key is not configured
            var desc = description ?? string.Empty;
            return ($"Bu ürün ({name}), yapay zeka tarafından analiz edildi. Açıklaması: {desc.Substring(0, Math.Min(desc.Length, 50))}...", new[] { "inovasyon", "teknoloji" });
        }

        var prompt = $@"
Aşağıdaki ürünü incele ve JSON formatında şu iki bilgiyi dön:
1. 'summary': Ürünün 1-2 cümlelik vurucu, pazarlama odaklı kısa özeti.
2. 'tags': Ürünü en iyi tanımlayan en fazla 3 adet kategori etiketi (virgülle ayrılmış tek bir string olarak, örnek: 'B2B, SaaS, Pazarlama').

Ürün Adı: {name}
Açıklama: {description}

SADECE geçerli bir JSON dön, markdown blokları (```json) kullanma.";

        var requestBody = new
        {
            contents = new[]
            {
                new
                {
                    parts = new[]
                    {
                        new { text = prompt }
                    }
                }
            }
        };

        var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-flash:generateContent?key={_apiKey}";
        
        var response = await _httpClient.PostAsJsonAsync(url, requestBody, cancellationToken);
        
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            throw new Exception($"Gemini API Error: {response.StatusCode} - {errorContent}");
        }

        var jsonDoc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);
        var textResult = jsonDoc.RootElement
            .GetProperty("candidates")[0]
            .GetProperty("content")
            .GetProperty("parts")[0]
            .GetProperty("text")
            .GetString();

        if (string.IsNullOrWhiteSpace(textResult))
        {
            throw new Exception("Gemini returned empty text.");
        }

        // Parse the generated JSON
        textResult = textResult.Trim();
        if (textResult.StartsWith("```json"))
        {
            textResult = textResult.Substring(7);
            if (textResult.EndsWith("```"))
            {
                textResult = textResult.Substring(0, textResult.Length - 3);
            }
        }
        
        var parsedObj = JsonSerializer.Deserialize<GeminiResponseFormat>(textResult, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        
        var summary = parsedObj?.Summary ?? "Özet oluşturulamadı.";
        var tagsStr = parsedObj?.Tags ?? "Genel";
        
        var tags = tagsStr.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        return (summary, tags);
    }

    private class GeminiResponseFormat
    {
        public string Summary { get; set; }
        public string Tags { get; set; }
    }
}

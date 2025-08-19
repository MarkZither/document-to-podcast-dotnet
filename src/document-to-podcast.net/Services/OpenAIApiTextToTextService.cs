using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DocumentToPodcast.Services;

public class OpenAIApiTextToTextService : ITextToTextService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<OpenAIApiTextToTextService> _logger;

    public OpenAIApiTextToTextService(HttpClient httpClient, ILogger<OpenAIApiTextToTextService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<string> TransformTextAsync(string inputText, string systemPrompt, string modelEndpoint)
    {
        var requestBody = CreateRequestBody(inputText, systemPrompt);
        var jsonContent = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync($"{modelEndpoint}/v1/chat/completions", content);
        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync();
        var responseObject = JsonSerializer.Deserialize<ChatCompletionResponse>(responseJson);
        
        return responseObject?.Choices?[0]?.Message?.Content ?? string.Empty;
    }

    public async IAsyncEnumerable<string> TransformTextStreamAsync(string inputText, string systemPrompt, string modelEndpoint)
    {
        var requestBody = CreateRequestBody(inputText, systemPrompt, stream: true);
        var jsonContent = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

        var request = new HttpRequestMessage(HttpMethod.Post, $"{modelEndpoint}/v1/chat/completions")
        {
            Content = content
        };

        using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();

        using var stream = await response.Content.ReadAsStreamAsync();
        using var reader = new StreamReader(stream);

        string? line;
        while ((line = await reader.ReadLineAsync()) != null)
        {
            if (line.StartsWith("data: "))
            {
                var dataContent = line.Substring(6);
                if (dataContent == "[DONE]")
                    break;

                ChatCompletionStreamResponse? streamResponse = null;
                try
                {
                    streamResponse = JsonSerializer.Deserialize<ChatCompletionStreamResponse>(dataContent);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to parse streaming response: {Line}", line);
                    continue;
                }

                var content_chunk = streamResponse?.Choices?[0]?.Delta?.Content;
                if (!string.IsNullOrEmpty(content_chunk))
                {
                    yield return content_chunk;
                }
            }
        }
    }

    private object CreateRequestBody(string inputText, string systemPrompt, bool stream = false)
    {
        return new
        {
            model = "phi3:medium-128k", // Updated to use available model
            messages = new[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = inputText }
            },
            response_format = new { type = "json_object" },
            stream = stream,
            temperature = 0.7,
            max_tokens = 4000
        };
    }
}

// Response models for deserialization
public class ChatCompletionResponse
{
    [JsonPropertyName("choices")]
    public Choice[]? Choices { get; set; }
}

public class Choice
{
    [JsonPropertyName("message")]
    public Message? Message { get; set; }
}

public class Message
{
    [JsonPropertyName("content")]
    public string? Content { get; set; }
}

public class ChatCompletionStreamResponse
{
    [JsonPropertyName("choices")]
    public StreamChoice[]? Choices { get; set; }
}

public class StreamChoice
{
    [JsonPropertyName("delta")]
    public Delta? Delta { get; set; }
}

public class Delta
{
    [JsonPropertyName("content")]
    public string? Content { get; set; }
}

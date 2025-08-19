using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Services;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text;

namespace DocumentToPodcast.Services;

public class OllamaChatCompletionService : IChatCompletionService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<OllamaChatCompletionService> _logger;
    private readonly string _modelId;
    private readonly string _baseUrl;

    public IReadOnlyDictionary<string, object?> Attributes { get; }

    public OllamaChatCompletionService(
        HttpClient httpClient,
        ILogger<OllamaChatCompletionService> logger,
        string modelId = "phi3:medium-128k",
        string baseUrl = "http://localhost:11434")
    {
        _httpClient = httpClient;
        _logger = logger;
        _modelId = modelId;
        _baseUrl = baseUrl.TrimEnd('/');
        
        Attributes = new Dictionary<string, object?>
        {
            ["ModelId"] = _modelId,
            ["BaseUrl"] = _baseUrl
        };
    }

    public async Task<IReadOnlyList<ChatMessageContent>> GetChatMessageContentsAsync(
        ChatHistory chatHistory,
        PromptExecutionSettings? executionSettings = null,
        Kernel? kernel = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Ollama Chat Completion - Model: {ModelId}, BaseUrl: {BaseUrl}", _modelId, _baseUrl);
            _logger.LogInformation("Starting AI inference - this may take several minutes for large documents...");
            
            // Convert chat history to Ollama format
            var messages = chatHistory.Select(message => new
            {
                role = message.Role.Label.ToLowerInvariant(),
                content = message.Content
            }).ToArray();

            var requestUrl = $"{_baseUrl}/v1/chat/completions";
            _logger.LogInformation("Making request to Ollama Chat API: {RequestUrl}", requestUrl);

            var requestBody = new
            {
                model = _modelId,
                messages = messages,
                stream = false,
                options = new
                {
                    temperature = 0.7,
                    num_predict = 4000,
                    top_p = 0.9
                }
            };

            var jsonRequest = JsonSerializer.Serialize(requestBody);
            _logger.LogDebug("Ollama chat request: {Request}", jsonRequest);

            using var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(requestUrl, content, cancellationToken);

            _logger.LogInformation("Ollama Chat API response status: {StatusCode}", response.StatusCode);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Ollama Chat API failed. Status: {StatusCode}, Error: {Error}", 
                    response.StatusCode, errorContent);
                throw new HttpRequestException($"Ollama API request failed: {response.StatusCode} - {errorContent}");
            }

            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogDebug("Ollama chat response: {Response}", responseContent);

            var ollamaResponse = JsonSerializer.Deserialize<OllamaChatResponse>(responseContent);
            
            if (ollamaResponse?.Message?.Content == null)
            {
                _logger.LogWarning("Ollama returned null or empty response");
                throw new InvalidOperationException("Ollama did not return a valid response");
            }

            _logger.LogInformation("Received response with {Length} characters", ollamaResponse.Message.Content.Length);

            var chatMessage = new ChatMessageContent(
                AuthorRole.Assistant,
                ollamaResponse.Message.Content
            );

            return new List<ChatMessageContent> { chatMessage };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in Ollama chat completion");
            throw;
        }
    }

    public async IAsyncEnumerable<StreamingChatMessageContent> GetStreamingChatMessageContentsAsync(
        ChatHistory chatHistory,
        PromptExecutionSettings? executionSettings = null,
        Kernel? kernel = null,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Ollama Streaming Chat Completion - Model: {ModelId}", _modelId);
        
        var messages = chatHistory.Select(message => new
        {
            role = message.Role.Label.ToLowerInvariant(),
            content = message.Content
        }).ToArray();

        var requestUrl = $"{_baseUrl}/api/chat";
        var requestBody = new
        {
            model = _modelId,
            messages = messages,
            stream = true,
            options = new
            {
                temperature = 0.7,
                num_predict = 4000,
                top_p = 0.9
            }
        };

        var jsonRequest = JsonSerializer.Serialize(requestBody);
        using var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");
        
        using var request = new HttpRequestMessage(HttpMethod.Post, requestUrl) { Content = content };
        var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("Ollama streaming API failed: {StatusCode} - {Error}", response.StatusCode, errorContent);
            throw new HttpRequestException($"Ollama API request failed: {response.StatusCode} - {errorContent}");
        }

        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);

        string? line;
        while ((line = await reader.ReadLineAsync()) != null && !cancellationToken.IsCancellationRequested)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;

            OllamaChatResponse? streamResponse = null;
            try
            {
                streamResponse = JsonSerializer.Deserialize<OllamaChatResponse>(line);
            }
            catch (JsonException ex)
            {
                _logger.LogDebug("Failed to parse streaming response line: {Line}, Error: {Error}", line, ex.Message);
                continue;
            }

            if (streamResponse?.Message?.Content != null)
            {
                yield return new StreamingChatMessageContent(
                    AuthorRole.Assistant,
                    streamResponse.Message.Content
                );
            }

            if (streamResponse?.Done == true)
            {
                break;
            }
        }
    }

    private class OllamaChatResponse
    {
        [JsonPropertyName("model")]
        public string? Model { get; set; }

        [JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("message")]
        public OllamaChatMessage? Message { get; set; }

        [JsonPropertyName("done")]
        public bool Done { get; set; }

        [JsonPropertyName("total_duration")]
        public long TotalDuration { get; set; }

        [JsonPropertyName("load_duration")]
        public long LoadDuration { get; set; }

        [JsonPropertyName("prompt_eval_count")]
        public int PromptEvalCount { get; set; }

        [JsonPropertyName("prompt_eval_duration")]
        public long PromptEvalDuration { get; set; }

        [JsonPropertyName("eval_count")]
        public int EvalCount { get; set; }

        [JsonPropertyName("eval_duration")]
        public long EvalDuration { get; set; }
    }

    private class OllamaChatMessage
    {
        [JsonPropertyName("role")]
        public string? Role { get; set; }

        [JsonPropertyName("content")]
        public string? Content { get; set; }
    }
}

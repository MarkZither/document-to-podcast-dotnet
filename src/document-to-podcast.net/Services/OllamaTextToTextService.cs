using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using DocumentToPodcast.Configuration;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text;

namespace DocumentToPodcast.Services;

public class OllamaTextToTextService : ITextToTextService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<OllamaTextToTextService> _logger;
    private readonly PodcastGeneratorOptions _options;

    public OllamaTextToTextService(
        HttpClient httpClient,
        ILogger<OllamaTextToTextService> logger,
        IOptions<PodcastGeneratorOptions> options)
    {
        _httpClient = httpClient;
        _logger = logger;
        _options = options.Value;
    }

    public async Task<string> TransformTextAsync(string inputText, string systemPrompt, string modelEndpoint)
    {
        var segments = new List<string>();
        await foreach (var segment in TransformTextStreamAsync(inputText, systemPrompt, modelEndpoint))
        {
            segments.Add(segment);
        }
        return string.Join('\n', segments);
    }

    public async IAsyncEnumerable<string> TransformTextStreamAsync(string inputText, string systemPrompt, string modelEndpoint)
    {
        List<string> result;
        
        try
        {
            _logger.LogInformation("Starting text transformation with Ollama API");
            _logger.LogInformation("Model endpoint: {ModelEndpoint}", modelEndpoint);
            _logger.LogInformation("Ollama model: {Model}", _options.SemanticKernel.OllamaModel);
            _logger.LogInformation("Input text length: {Length} characters", inputText.Length);
            
            var prompt = CreatePodcastPrompt(inputText, systemPrompt);
            _logger.LogInformation("Created podcast prompt with length: {Length} characters", prompt.Length);
            
            var requestUrl = $"{modelEndpoint.TrimEnd('/')}/api/generate";
            _logger.LogInformation("Making request to Ollama API: {RequestUrl}", requestUrl);
            
            var requestBody = new
            {
                model = _options.SemanticKernel.OllamaModel,
                prompt = prompt,
                stream = false,
                options = new
                {
                    temperature = _options.SemanticKernel.Temperature,
                    num_predict = _options.SemanticKernel.MaxTokens,
                    top_p = 0.9
                }
            };

            var jsonRequest = JsonSerializer.Serialize(requestBody, new JsonSerializerOptions 
            { 
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower 
            });
            
            _logger.LogDebug("Ollama request body: {RequestBody}", jsonRequest);

            using var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");
            
            _logger.LogInformation("Sending request to Ollama with settings: Model={Model}, Temperature={Temperature}, MaxTokens={MaxTokens}", 
                _options.SemanticKernel.OllamaModel, _options.SemanticKernel.Temperature, _options.SemanticKernel.MaxTokens);
            
            var response = await _httpClient.PostAsync(requestUrl, content);
            
            _logger.LogInformation("Ollama API response status: {StatusCode}", response.StatusCode);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Ollama API request failed. Status: {StatusCode}, Content: {ErrorContent}", 
                    response.StatusCode, errorContent);
                throw new HttpRequestException($"Ollama API request failed with status {response.StatusCode}: {errorContent}");
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            _logger.LogDebug("Ollama API response: {ResponseContent}", responseContent);
            
            var ollamaResponse = JsonSerializer.Deserialize<OllamaResponse>(responseContent);
            
            if (ollamaResponse?.Response == null)
            {
                _logger.LogWarning("Ollama response was null or empty");
                throw new InvalidOperationException("Ollama did not return a valid response");
            }

            var scriptText = ollamaResponse.Response;
            _logger.LogInformation("Generated script with {Length} characters", scriptText.Length);
            _logger.LogDebug("Generated script content: {ScriptText}", scriptText);
            
            // Split the result into lines
            var lines = scriptText.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            result = lines.Where(line => !string.IsNullOrWhiteSpace(line))
                         .Select(line => line.Trim())
                         .ToList();
                         
            _logger.LogInformation("Processed script into {LineCount} lines", result.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate podcast script using Ollama API. Exception type: {ExceptionType}, Message: {Message}", 
                ex.GetType().Name, ex.Message);
                
            if (ex.InnerException != null)
            {
                _logger.LogError("Inner exception: {InnerExceptionType}, Message: {InnerMessage}", 
                    ex.InnerException.GetType().Name, ex.InnerException.Message);
            }
            
            // Fallback: Generate a simple script
            _logger.LogInformation("Using fallback script generation");
            result = new List<string>
            {
                "Welcome to our podcast. Today we'll be discussing the provided document.",
                "This is an interesting topic that deserves our attention.",
                "Thank you for listening to our discussion today."
            };
        }

        // Yield the results
        foreach (var line in result)
        {
            yield return line;
        }
    }

    private string CreatePodcastPrompt(string documentText, string systemPrompt)
    {
        // If systemPrompt contains podcast-specific instructions, use it; otherwise create our own
        if (systemPrompt.Contains("podcast") || systemPrompt.Contains("conversation"))
        {
            return $"{systemPrompt}\n\nDocument content:\n{documentText}";
        }

        return $@"
You are a professional podcast script writer. Create an engaging, conversational podcast script based on the following document content.

Instructions:
1. Create a natural dialogue between two speakers
2. Break down complex topics into digestible segments
3. Include natural transitions and reactions
4. Make it sound conversational, not like reading
5. Keep the tone engaging and informative
6. Format as simple lines of dialogue, alternating between speakers

Document content:
{documentText}

Generate an engaging podcast script now:
";
    }

    private class OllamaResponse
    {
        [JsonPropertyName("model")]
        public string? Model { get; set; }

        [JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("response")]
        public string? Response { get; set; }

        [JsonPropertyName("done")]
        public bool Done { get; set; }

        [JsonPropertyName("context")]
        public int[]? Context { get; set; }

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
}

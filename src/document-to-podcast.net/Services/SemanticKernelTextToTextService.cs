using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using DocumentToPodcast.Configuration;
using System.Text.Json;

namespace DocumentToPodcast.Services;

public class SemanticKernelTextToTextService : ITextToTextService
{
    private readonly Kernel _kernel;
    private readonly IChatCompletionService _chatService;
    private readonly ILogger<SemanticKernelTextToTextService> _logger;
    private readonly PodcastGeneratorOptions _options;

    public SemanticKernelTextToTextService(
        Kernel kernel,
        IChatCompletionService chatService,
        ILogger<SemanticKernelTextToTextService> logger,
        IOptions<PodcastGeneratorOptions> options)
    {
        _kernel = kernel;
        _chatService = chatService;
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
            _logger.LogInformation("Starting text transformation with Semantic Kernel");
            _logger.LogInformation("Model endpoint: {ModelEndpoint}", modelEndpoint);
            _logger.LogInformation("Input text length: {Length} characters", inputText.Length);
            _logger.LogInformation("System prompt: {SystemPrompt}", systemPrompt);
            
            var prompt = CreatePodcastPrompt(inputText, systemPrompt);
            _logger.LogInformation("Created podcast prompt with length: {Length} characters", prompt.Length);
            
            // Create chat history
            var chatHistory = new ChatHistory();
            chatHistory.AddUserMessage(prompt);
            
            _logger.LogInformation("Generating podcast script using Semantic Kernel with settings: MaxTokens={MaxTokens}, Temperature={Temperature}", 
                _options.SemanticKernel.MaxTokens, _options.SemanticKernel.Temperature);
            
            // Get chat completion
            var chatResults = await _chatService.GetChatMessageContentsAsync(chatHistory, kernel: _kernel);
            
            if (!chatResults.Any())
            {
                throw new InvalidOperationException("No response received from chat completion service");
            }
            
            var scriptText = chatResults.First().Content ?? "";

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
            _logger.LogError(ex, "Failed to generate podcast script using Semantic Kernel. Exception type: {ExceptionType}, Message: {Message}", 
                ex.GetType().Name, ex.Message);
                
            if (ex.InnerException != null)
            {
                _logger.LogError("Inner exception: {InnerExceptionType}, Message: {InnerMessage}", 
                    ex.InnerException.GetType().Name, ex.InnerException.Message);
            }
            
            // Fallback: Generate a simple script
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
}

public record PodcastScript
{
    public string Speaker { get; init; } = "";
    public string Text { get; init; } = "";
    public int Sequence { get; init; }
}

using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace DocumentToPodcast.Configuration;

public class PodcastGeneratorOptions
{
    public const string SectionName = "PodcastGenerator";

    [JsonPropertyName("useRealTextToSpeech")]
    public bool UseRealTextToSpeech { get; set; } = false;

    [JsonPropertyName("usePythonTts")]
    public bool UsePythonTts { get; set; } = false;

    [JsonPropertyName("useOnnxTts")]
    public bool UseOnnxTts { get; set; } = true;

    [JsonPropertyName("useSemanticKernel")]
    public bool UseSemanticKernel { get; set; } = true;

    [JsonPropertyName("useDirectOllamaApi")]
    public bool UseDirectOllamaApi { get; set; } = false;

    [Required]
    [JsonPropertyName("defaultModelEndpoint")]
    public string DefaultModelEndpoint { get; set; } = "http://localhost:11434";

    [JsonPropertyName("pythonTtsServiceUrl")]
    public string PythonTtsServiceUrl { get; set; } = "http://localhost:5001";

    [JsonPropertyName("onnxModelPath")]
    public string? OnnxModelPath { get; set; }

    [JsonPropertyName("autoDownloadModels")]
    public bool AutoDownloadModels { get; set; } = true;

    [JsonPropertyName("modelCacheDirectory")]
    public string? ModelCacheDirectory { get; set; }

    [JsonPropertyName("openAI")]
    public OpenAIOptions OpenAI { get; set; } = new();
    
    [JsonPropertyName("ollama")]
    public OllamaOptions Ollama { get; set; } = new();

    [JsonPropertyName("semanticKernel")]
    public SemanticKernelOptions SemanticKernel { get; set; } = new();
}

public class OpenAIOptions
{
    [JsonPropertyName("apiKey")]
    public string ApiKey { get; set; } = string.Empty;
    
    [JsonPropertyName("baseUrl")]
    public string BaseUrl { get; set; } = "https://api.openai.com/v1";
    
    [JsonPropertyName("model")]
    public string Model { get; set; } = "gpt-4";
    
    [JsonPropertyName("voiceModel")]
    public string VoiceModel { get; set; } = "tts-1";
}

public class OllamaOptions
{
    [JsonPropertyName("baseUrl")]
    public string BaseUrl { get; set; } = "http://localhost:11434";
    
    [JsonPropertyName("model")]
    public string Model { get; set; } = "llama3.2";
    
    [JsonPropertyName("voiceModel")]
    public string VoiceModel { get; set; } = "xtts";
}

public class SemanticKernelOptions
{
    [JsonPropertyName("useOpenAI")]
    public bool UseOpenAI { get; set; } = false;

    [JsonPropertyName("useOllama")]
    public bool UseOllama { get; set; } = true;

    [JsonPropertyName("ollamaEndpoint")]
    public string OllamaEndpoint { get; set; } = "http://localhost:11434";

    [JsonPropertyName("ollamaModel")]
    public string OllamaModel { get; set; } = "phi3:medium-128k";

    [JsonPropertyName("temperature")]
    public double Temperature { get; set; } = 0.7;

    [JsonPropertyName("maxTokens")]
    public int MaxTokens { get; set; } = 4000;
}

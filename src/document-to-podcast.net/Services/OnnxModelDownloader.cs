using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using DocumentToPodcast.Configuration;
using System.IO.Compression;
using System.Text.Json;

namespace DocumentToPodcast.Services;

public class OnnxModelDownloader
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<OnnxModelDownloader> _logger;
    private readonly PodcastGeneratorOptions _options;

    public OnnxModelDownloader(
        HttpClient httpClient, 
        ILogger<OnnxModelDownloader> logger,
        IOptions<PodcastGeneratorOptions> options)
    {
        _httpClient = httpClient;
        _logger = logger;
        _options = options.Value;
    }

    public async Task<string> EnsureModelAvailableAsync(string? preferredModelPath = null)
    {
        // Check if a model already exists
        var modelPath = preferredModelPath ?? GetDefaultModelPath();
        
        _logger.LogInformation("Checking ONNX model at path: {ModelPath}", modelPath);
        
        if (File.Exists(modelPath))
        {
            _logger.LogInformation("ONNX model already exists at {ModelPath}", modelPath);
            return modelPath;
        }

        // Create the models directory if it doesn't exist
        var modelDirectory = Path.GetDirectoryName(modelPath);
        string cwd = Directory.GetCurrentDirectory(); // Gets the full path of the current working directory
        var modelDirectory2 = Path.Combine(cwd, modelDirectory); // Appends the directory name

        _logger.LogInformation("Model directory: {ModelDirectory}", modelDirectory);
        
        if (!string.IsNullOrEmpty(modelDirectory))
        {
            Directory.CreateDirectory(modelDirectory);
            _logger.LogInformation("Created model directory: {ModelDirectory}", modelDirectory);
        }

        _logger.LogInformation("Downloading OuteTTS ONNX model to {ModelPath}", modelPath);
        _logger.LogInformation("This will download approximately 500MB of model files from Hugging Face...");

        try
        {
            await DownloadOuteTtsModel(modelPath);
            _logger.LogInformation("Successfully downloaded OuteTTS ONNX model");
            return modelPath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to download ONNX model");
            throw;
        }
    }

    private async Task DownloadOuteTtsModel(string targetPath)
    {
        // OuteTTS-0.2-500M ONNX model from Hugging Face
        const string modelUrl = "https://huggingface.co/onnx-community/OuteTTS-0.2-500M/resolve/main/onnx/model.onnx";
        const string tokenizerUrl = "https://huggingface.co/onnx-community/OuteTTS-0.2-500M/resolve/main/tokenizer.json";
        const string configUrl = "https://huggingface.co/onnx-community/OuteTTS-0.2-500M/resolve/main/config.json";

        var modelDirectory = Path.GetDirectoryName(targetPath);
        if (string.IsNullOrEmpty(modelDirectory))
        {
            throw new ArgumentException($"Invalid target path: {targetPath}");
        }
        
        var tokenizerPath = Path.Combine(modelDirectory, "tokenizer.json");
        var configPath = Path.Combine(modelDirectory, "config.json");

        // Download main model file
        _logger.LogInformation("Downloading main ONNX model file...");
        await DownloadFileWithProgress(modelUrl, targetPath);

        // Download tokenizer (needed for text processing)
        _logger.LogInformation("Downloading tokenizer...");
        await DownloadFileWithProgress(tokenizerUrl, tokenizerPath);

        // Download config (contains model metadata)
        _logger.LogInformation("Downloading model config...");
        await DownloadFileWithProgress(configUrl, configPath);

        // Verify the downloads
        if (!File.Exists(targetPath) || !File.Exists(tokenizerPath) || !File.Exists(configPath))
        {
            throw new InvalidOperationException("Model download incomplete - missing required files");
        }

        var modelSize = new FileInfo(targetPath).Length;
        _logger.LogInformation("Model download complete. Size: {Size:N0} bytes", modelSize);
    }

    private async Task DownloadFileWithProgress(string url, string targetPath)
    {
        using var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();

        var totalBytes = response.Content.Headers.ContentLength;
        using var contentStream = await response.Content.ReadAsStreamAsync();
        using var fileStream = new FileStream(targetPath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);

        var buffer = new byte[8192];
        var totalRead = 0L;
        int bytesRead;
        var lastReported = DateTime.Now;

        while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
        {
            await fileStream.WriteAsync(buffer, 0, bytesRead);
            totalRead += bytesRead;

            // Report progress every 2 seconds
            if (DateTime.Now - lastReported > TimeSpan.FromSeconds(2))
            {
                var percentage = totalBytes.HasValue ? (totalRead * 100.0 / totalBytes.Value) : 0;
                _logger.LogInformation("Download progress: {Percentage:F1}% ({Downloaded:N0} / {Total:N0} bytes)", 
                    percentage, totalRead, totalBytes ?? 0);
                lastReported = DateTime.Now;
            }
        }
    }

    public string GetDefaultModelPath()
    {
        if (!string.IsNullOrEmpty(_options.OnnxModelPath))
        {
            _logger.LogInformation("Using configured ONNX model path: {ModelPath}", _options.OnnxModelPath);
            return _options.OnnxModelPath;
        }

        // Default locations in order of preference
        var possiblePaths = new[]
        {
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "document-to-podcast", "models", "outetts-0.2-500m.onnx"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".cache", "document-to-podcast", "models", "outetts-0.2-500m.onnx"),
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "models", "outetts-0.2-500m.onnx")
        };

        var defaultPath = possiblePaths[0]; // Default to user's AppData folder
        _logger.LogInformation("Using default ONNX model path: {ModelPath}", defaultPath);
        return defaultPath;
    }

    public async Task<ModelInfo> GetModelInfoAsync(string modelPath)
    {
        var configPath = Path.Combine(Path.GetDirectoryName(modelPath)!, "config.json");
        
        if (!File.Exists(configPath))
        {
            return new ModelInfo("OuteTTS-0.2-500M", "Unknown", 24000);
        }

        try
        {
            var configJson = await File.ReadAllTextAsync(configPath);
            var config = JsonSerializer.Deserialize<ModelConfig>(configJson);
            
            return new ModelInfo(
                config?.ModelName ?? "OuteTTS-0.2-500M",
                config?.Version ?? "Unknown",
                config?.SampleRate ?? 24000
            );
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to read model config, using defaults");
            return new ModelInfo("OuteTTS-0.2-500M", "Unknown", 24000);
        }
    }

    public record ModelInfo(string Name, string Version, int SampleRate);

    private class ModelConfig
    {
        public string? ModelName { get; set; }
        public string? Version { get; set; }
        public int SampleRate { get; set; }
    }
}

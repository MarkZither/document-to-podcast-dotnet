namespace DocumentToPodcast.Services;

/// <summary>
/// Factory interface for creating document parsers based on file type
/// </summary>
public interface IDocumentParserFactory
{
    IDocumentParser CreateParser(string fileExtension);
    bool SupportsFormat(string fileExtension);
}

/// <summary>
/// Interface for parsing documents to text
/// </summary>
public interface IDocumentParser
{
    Task<string> ParseAsync(string filePath);
    Task<string> ParseAsync(Stream stream);
    string[] SupportedExtensions { get; }
}

/// <summary>
/// Interface for text-to-text transformation using AI models
/// </summary>
public interface ITextToTextService
{
    Task<string> TransformTextAsync(string inputText, string systemPrompt, string modelEndpoint);
    IAsyncEnumerable<string> TransformTextStreamAsync(string inputText, string systemPrompt, string modelEndpoint);
}

/// <summary>
/// Interface for text-to-speech conversion
/// </summary>
public interface ITextToSpeechService
{
    Task<byte[]> ConvertToSpeechAsync(string text, string voiceProfile, string modelEndpoint);
}

/// <summary>
/// Main service interface for generating podcasts
/// </summary>
public interface IPodcastGeneratorService
{
    Task GeneratePodcastAsync(string inputFile, string outputFolder, string modelEndpoint, string? configFile = null);
}

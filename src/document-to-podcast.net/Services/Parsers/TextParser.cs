namespace DocumentToPodcast.Services.Parsers;

/// <summary>
/// Simple text file parser
/// </summary>
public class TextParser : IDocumentParser
{
    public string[] SupportedExtensions => new[] { ".txt" };

    public async Task<string> ParseAsync(string filePath)
    {
        return await File.ReadAllTextAsync(filePath);
    }

    public async Task<string> ParseAsync(Stream stream)
    {
        using var reader = new StreamReader(stream);
        return await reader.ReadToEndAsync();
    }
}

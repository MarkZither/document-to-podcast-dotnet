using DocumentToPodcast.Services.Parsers;

namespace DocumentToPodcast.Services;

public class DocumentParserFactory : IDocumentParserFactory
{
    private readonly Dictionary<string, Func<IDocumentParser>> _parsers;

    public DocumentParserFactory()
    {
        _parsers = new Dictionary<string, Func<IDocumentParser>>(StringComparer.OrdinalIgnoreCase)
        {
            { ".pdf", () => new MarkItDownParser() },
            { ".docx", () => new MarkItDownParser() },
            { ".html", () => new MarkItDownParser() },
            { ".md", () => new MarkItDownParser() },
            { ".txt", () => new TextParser() },
            // Future parsers can be added here
            // { ".pdf", () => new MarkItDownSharpParser() },
            // { ".docx", () => new MarkItDownNetParser() },
        };
    }

    public IDocumentParser CreateParser(string fileExtension)
    {
        if (_parsers.TryGetValue(fileExtension, out var factory))
        {
            return factory();
        }
        
        throw new NotSupportedException($"File extension '{fileExtension}' is not supported.");
    }

    public bool SupportsFormat(string fileExtension)
    {
        return _parsers.ContainsKey(fileExtension);
    }
}

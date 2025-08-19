using System.Diagnostics;
using Microsoft.Extensions.Logging;
using MarkItDownSharp.Converters;
using MarkItDownSharp.Models;
using MarkItDownSharp;
using MarkItDownSharp.Services;

namespace DocumentToPodcast.Services.Parsers;

/// <summary>
/// Parser that uses MarkItDown Python service via HTTP API call
/// This can be extended to support local MarkItDown.NET implementations
/// </summary>
public class MarkItDownParser : IDocumentParser
{
    private readonly ILogger<MarkItDownParser> _logger;
    private readonly MarkItDownConverter _converter;
    private readonly HttpClient _httpClient;

    public string[] SupportedExtensions => new[] { ".pdf", ".docx", ".html", ".md" };

    public MarkItDownParser()
    {
        _httpClient = new HttpClient();
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        _logger = loggerFactory.CreateLogger<MarkItDownParser>();
        var pdfConverter = new PdfConverter(new NoOpOcrService());
        IEnumerable<DocumentConverter> converters = new List<DocumentConverter>();
        converters = converters.Append(pdfConverter);
        _converter =  new MarkItDownConverter(converters);
    }

    public MarkItDownParser(ILogger<MarkItDownParser> logger, HttpClient httpClient, MarkItDownConverter converter)
    {
        _logger = logger;
        _httpClient = httpClient;
        _converter = converter;
    }

    public async Task<string> ParseAsync(string filePath)
    {
        return await ParseWithMarkItDownNet(filePath);
    }

    public async Task<string> ParseAsync(Stream stream)
    {
        // For stream parsing, we need to save to temp file first
        var tempFile = Path.GetTempFileName();
        try
        {
            using (var fileStream = File.Create(tempFile))
            {
                await stream.CopyToAsync(fileStream);
            }
            return await ParseAsync(tempFile);
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    private async Task<string> ParseWithMarkItDownNet(string filePath)
    {
        // Set up conversion options.
        var options = new ConversionOptions
        {
        };
        if (File.Exists(filePath))
        {
            var docConvertorResult = await _converter.ConvertLocalAsync(filePath, options);
            return docConvertorResult.TextContent;
        }

        // Assume it's a URL
        var docConvertorResultUrl = await _converter.ConvertLocalAsync(filePath, options);
            return docConvertorResultUrl.TextContent;
    }
}

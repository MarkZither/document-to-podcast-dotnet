using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace DocumentToPodcast.Services.Parsers;

/// <summary>
/// Parser that uses MarkItDown Python service via HTTP API call
/// This can be extended to support local MarkItDown.NET implementations
/// </summary>
public class MarkItDownParser : IDocumentParser
{
    private readonly ILogger<MarkItDownParser> _logger;
    private readonly HttpClient _httpClient;

    public string[] SupportedExtensions => new[] { ".pdf", ".docx", ".html", ".md" };

    public MarkItDownParser()
    {
        _httpClient = new HttpClient();
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        _logger = loggerFactory.CreateLogger<MarkItDownParser>();
    }

    public MarkItDownParser(ILogger<MarkItDownParser> logger, HttpClient httpClient)
    {
        _logger = logger;
        _httpClient = httpClient;
    }

    public async Task<string> ParseAsync(string filePath)
    {
        // For now, just read the file as text for testing
        // TODO: Implement proper MarkItDown integration
        
        _logger.LogInformation("Parsing document: {FilePath}", filePath);
        
        try
        {
            var content = await File.ReadAllTextAsync(filePath);
            _logger.LogDebug("Successfully read {Length} characters from file", content.Length);
            return content;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to read file: {FilePath}", filePath);
            throw;
        }
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
        // Placeholder for future MarkItDown.NET implementation
        // This would use a native .NET implementation of MarkItDown
        throw new NotImplementedException("MarkItDown.NET implementation not yet available");
    }

    private async Task<string> ParseWithPythonService(string filePath)
    {
        // Try to call a Python MarkItDown service running as HTTP API
        // This assumes you have a service running on localhost:8000/parse
        var serviceUrl = Environment.GetEnvironmentVariable("MARKITDOWN_SERVICE_URL") ?? "http://localhost:8000";
        
        using var form = new MultipartFormDataContent();
        using var fileContent = new StreamContent(File.OpenRead(filePath));
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
        form.Add(fileContent, "file", Path.GetFileName(filePath));

        var response = await _httpClient.PostAsync($"{serviceUrl}/parse", form);
        response.EnsureSuccessStatusCode();
        
        return await response.Content.ReadAsStringAsync();
    }

    private async Task<string> ParseWithLocalPython(string filePath)
    {
        var tempOutputFile = Path.GetTempFileName();
        try
        {
            var pythonScript = $"""
                import sys
                from markitdown import MarkItDown
                
                md = MarkItDown()
                result = md.convert(r"{filePath}")
                
                with open(r"{tempOutputFile}", "w", encoding="utf-8") as f:
                    f.write(result.text_content)
                """;

            var tempScriptFile = Path.GetTempFileName() + ".py";
            await File.WriteAllTextAsync(tempScriptFile, pythonScript);

            try
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "python",
                        Arguments = tempScriptFile,
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                var error = await process.StandardError.ReadToEndAsync();
                await process.WaitForExitAsync();

                if (process.ExitCode != 0)
                {
                    throw new InvalidOperationException($"Python MarkItDown failed: {error}");
                }

                if (File.Exists(tempOutputFile))
                {
                    return await File.ReadAllTextAsync(tempOutputFile);
                }
                else
                {
                    throw new InvalidOperationException("MarkItDown did not produce output file");
                }
            }
            finally
            {
                if (File.Exists(tempScriptFile))
                {
                    File.Delete(tempScriptFile);
                }
            }
        }
        finally
        {
            if (File.Exists(tempOutputFile))
            {
                File.Delete(tempOutputFile);
            }
        }
    }
}

using DocumentToPodcast.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;
using System.Text;
using System.Text.Json;

namespace DocumentToPodcast.Services;

public class PodcastGeneratorService : IPodcastGeneratorService
{
    private readonly IDocumentParserFactory _parserFactory;
    private readonly ITextToTextService _textToTextService;
    private readonly ITextToSpeechService _textToSpeechService;
    private readonly ILogger<PodcastGeneratorService> _logger;
    private readonly IConfiguration _configuration;

    public PodcastGeneratorService(
        IDocumentParserFactory parserFactory,
        ITextToTextService textToTextService,
        ITextToSpeechService textToSpeechService,
        ILogger<PodcastGeneratorService> logger,
        IConfiguration configuration)
    {
        _parserFactory = parserFactory;
        _textToTextService = textToTextService;
        _textToSpeechService = textToSpeechService;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task GeneratePodcastAsync(string inputFile, string outputFolder, string modelEndpoint, string? configFile = null)
    {
        PodcastConfig config;

        if (!string.IsNullOrEmpty(configFile))
        {
            _logger.LogInformation("Loading configuration from {ConfigFile}", configFile);
            var configJson = await File.ReadAllTextAsync(configFile);
            config = JsonSerializer.Deserialize<PodcastConfig>(configJson) 
                ?? throw new InvalidOperationException("Failed to load configuration");
        }
        else
        {
            config = new PodcastConfig
            {
                InputFile = inputFile,
                OutputFolder = outputFolder,
                TextToTextModel = modelEndpoint
            };
        }

        // Ensure output directory exists
        Directory.CreateDirectory(config.OutputFolder);

        // Parse the document
        var fileExtension = Path.GetExtension(config.InputFile);
        if (!_parserFactory.SupportsFormat(fileExtension))
        {
            throw new NotSupportedException($"File format '{fileExtension}' is not supported");
        }

        var parser = _parserFactory.CreateParser(fileExtension);
        _logger.LogInformation("Parsing document: {InputFile}", config.InputFile);
        var documentText = await parser.ParseAsync(config.InputFile);
        
        if (string.IsNullOrWhiteSpace(documentText))
        {
            throw new InvalidOperationException("Document parsing resulted in empty text");
        }

        _logger.LogDebug("Parsed document length: {Length} characters", documentText.Length);

        // Clean the text (basic cleaning for now)
        var cleanText = CleanText(documentText);
        _logger.LogDebug("Cleaned text length: {Length} characters", cleanText.Length);

        // Prepare system prompt
        var systemPrompt = config.TextToTextPrompt.Replace(
            "{SPEAKERS}", 
            string.Join("\n", config.Speakers.Select(s => s.ToString())));

        // Limit text size (simple truncation for now, could be made smarter)
        const int maxCharacters = 16000; // Rough estimate for token limits
        if (cleanText.Length > maxCharacters)
        {
            _logger.LogWarning("Input text too long ({Length}), truncating to {MaxLength}", 
                cleanText.Length, maxCharacters);
            cleanText = cleanText[..maxCharacters];
        }

        // Generate podcast script
        _logger.LogInformation("Generating podcast script...");
        var podcastScript = new StringBuilder();
        var audioSegments = new List<byte[]>();

        try
        {
            var fullScript = new StringBuilder();
            await foreach (var chunk in _textToTextService.TransformTextStreamAsync(cleanText, systemPrompt, config.TextToTextModel))
            {
                fullScript.Append(chunk);
                podcastScript.Append(chunk);
                _logger.LogDebug("Generated content chunk: {Chunk}", chunk.Trim());
            }

            // Check if we got meaningful content
            if (fullScript.Length > 50) // Minimum threshold for valid script
            {
                // Parse the generated script and convert to audio
                await GenerateAudioFromScript(fullScript.ToString(), config, audioSegments);
            }
            else
            {
                _logger.LogWarning("Generated script too short ({Length} characters), using fallback", fullScript.Length);
                throw new InvalidOperationException("Generated script was too short or empty");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "AI service not available or failed, generating placeholder script and audio");
            
            // Generate a simple placeholder script with audio
            var placeholderLines = new[]
            {
                ("Speaker 1", "Welcome to our podcast! Today we're discussing an interesting document."),
                ("Speaker 2", "That sounds fascinating! Can you tell us more about it?"),
                ("Speaker 1", $"Certainly! The document is about: {cleanText.Substring(0, Math.Min(200, cleanText.Length))}..."),
                ("Speaker 2", "Wow, that's really interesting! Thanks for sharing that with us."),
                ("Speaker 1", "Thank you for listening to our podcast today!"),
                ("Speaker 2", "See you next time!")
            };

            podcastScript.Clear();
            foreach (var (speakerName, line) in placeholderLines)
            {
                podcastScript.AppendLine($"{speakerName}: {line}");
                
                // Generate audio for each speaker line
                var speaker = config.Speakers.FirstOrDefault(s => s.Name.Contains(speakerName.Split(' ')[1])) ?? config.Speakers.First();
                try
                {
                    var audioData = await _textToSpeechService.ConvertToSpeechAsync(line, speaker.VoiceProfile, config.TextToSpeechModel);
                    if (audioData.Length > 0)
                    {
                        audioSegments.Add(audioData);
                        _logger.LogDebug("Generated audio for {Speaker}: {Length} bytes", speakerName, audioData.Length);
                    }
                }
                catch (Exception audioEx)
                {
                    _logger.LogWarning(audioEx, "Failed to generate audio for {Speaker}", speakerName);
                }
            }
        }

        // Save the podcast script
        var scriptPath = Path.Combine(config.OutputFolder, "podcast.txt");
        await File.WriteAllTextAsync(scriptPath, podcastScript.ToString());
        _logger.LogInformation("Podcast script saved to: {ScriptPath}", scriptPath);

        // Generate final audio file
        var audioPath = Path.Combine(config.OutputFolder, "podcast.wav");
        if (audioSegments.Count > 0)
        {
            _logger.LogInformation("Combining {Count} audio segments", audioSegments.Count);
            var combinedAudio = AudioUtilities.CombineAudioSegments(audioSegments, silencePadSeconds: 1.0f);
            await AudioUtilities.SaveAudioToFileAsync(combinedAudio, audioPath);
            _logger.LogInformation("Audio saved to: {AudioPath}", audioPath);
        }
        else
        {
            _logger.LogWarning("No audio segments generated, creating placeholder file");
            await File.WriteAllTextAsync(audioPath.Replace(".wav", "_placeholder.txt"), 
                "Audio generation failed. Please check that the text-to-speech service is available.\n\nScript content:\n\n" + podcastScript.ToString());
        }

        _logger.LogInformation("Podcast generation completed! Output saved to: {OutputFolder}", config.OutputFolder);
    }

    private async Task GenerateAudioFromScript(string scriptContent, PodcastConfig config, List<byte[]> audioSegments)
    {
        // First, validate that the script content looks like valid JSON
        if (string.IsNullOrWhiteSpace(scriptContent) || 
            (!scriptContent.Trim().StartsWith("{") && !scriptContent.Trim().StartsWith("[")))
        {
            _logger.LogWarning("Script content does not appear to be valid JSON format, skipping audio generation");
            return;
        }

        try
        {
            // Parse JSON script
            var scriptDict = JsonSerializer.Deserialize<Dictionary<string, string>>(scriptContent);
            if (scriptDict == null || scriptDict.Count == 0)
            {
                _logger.LogWarning("Script parsed but contains no dialogue entries");
                return;
            }

            _logger.LogInformation("Successfully parsed script with {Count} dialogue entries", scriptDict.Count);

            foreach (var kvp in scriptDict)
            {
                var speakerMatch = Regex.Match(kvp.Key, @"Speaker (\d+)");
                if (speakerMatch.Success)
                {
                    var speakerId = int.Parse(speakerMatch.Groups[1].Value);
                    var speaker = config.Speakers.FirstOrDefault(s => s.Id == speakerId);
                    if (speaker != null)
                    {
                        _logger.LogDebug("Generating audio for {SpeakerName}: {Text}", speaker.Name, kvp.Value);
                        try
                        {
                            var audioData = await _textToSpeechService.ConvertToSpeechAsync(kvp.Value, speaker.VoiceProfile, config.TextToSpeechModel);
                            if (audioData.Length > 0)
                            {
                                audioSegments.Add(audioData);
                                _logger.LogDebug("Generated audio segment: {Length} bytes", audioData.Length);
                            }
                        }
                        catch (Exception audioEx)
                        {
                            _logger.LogWarning(audioEx, "Failed to generate audio for {Speaker}, skipping segment", speaker.Name);
                        }
                    }
                    else
                    {
                        _logger.LogWarning("Speaker {SpeakerId} not found in configuration", speakerId);
                    }
                }
                else
                {
                    _logger.LogWarning("Invalid speaker key format: {Key}", kvp.Key);
                }
            }
        }
        catch (JsonException jsonEx)
        {
            _logger.LogError(jsonEx, "Failed to parse script as JSON. Script content preview: {Preview}", 
                scriptContent.Length > 200 ? scriptContent.Substring(0, 200) + "..." : scriptContent);
            
            // Don't attempt fallback parsing - if the AI service failed to generate valid JSON,
            // we should rely on the placeholder content instead
            _logger.LogInformation("Script generation appears to have failed, placeholder content should be used instead");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during script processing");
        }
    }

    private string CleanText(string text)
    {
        // Basic text cleaning - can be enhanced based on document type
        // Remove excessive whitespace
        text = Regex.Replace(text, @"\s+", " ");
        
        // Remove common unwanted patterns
        text = Regex.Replace(text, @"[\r\n\t]+", " ");
        
        // Trim
        text = text.Trim();
        
        return text;
    }
}

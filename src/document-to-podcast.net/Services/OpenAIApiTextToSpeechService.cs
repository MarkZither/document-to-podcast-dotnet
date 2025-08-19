using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;

namespace DocumentToPodcast.Services;

public class OpenAIApiTextToSpeechService : ITextToSpeechService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<OpenAIApiTextToSpeechService> _logger;

    public OpenAIApiTextToSpeechService(HttpClient httpClient, ILogger<OpenAIApiTextToSpeechService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<byte[]> ConvertToSpeechAsync(string text, string voiceProfile, string modelEndpoint)
    {
        try
        {
            var requestBody = new
            {
                model = "tts-1", // This can be made configurable
                input = text,
                voice = MapVoiceProfile(voiceProfile),
                response_format = "wav"
            };

            var jsonContent = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"{modelEndpoint}/v1/audio/speech", content);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsByteArrayAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to convert text to speech");
            // Return empty byte array as fallback
            return Array.Empty<byte>();
        }
    }

    private string MapVoiceProfile(string voiceProfile)
    {
        // Map internal voice profiles to API voice names
        return voiceProfile switch
        {
            "female_1" => "nova",
            "male_1" => "onyx",
            "female_2" => "alloy",
            "male_2" => "echo",
            _ => "alloy"
        };
    }
}

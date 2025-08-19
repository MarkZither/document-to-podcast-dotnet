using Microsoft.Extensions.Logging;

namespace DocumentToPodcast.Services;

/// <summary>
/// Mock text-to-speech service that generates placeholder WAV files for testing
/// </summary>
public class MockTextToSpeechService : ITextToSpeechService
{
    private readonly ILogger<MockTextToSpeechService> _logger;

    public MockTextToSpeechService(ILogger<MockTextToSpeechService> logger)
    {
        _logger = logger;
    }

    public async Task<byte[]> ConvertToSpeechAsync(string text, string voiceProfile, string modelEndpoint)
    {
        _logger.LogInformation("Generating mock audio for voice {VoiceProfile}: {Text}", voiceProfile, text.Substring(0, Math.Min(50, text.Length)) + "...");
        
        // Simulate some processing time
        await Task.Delay(100);
        
        // Generate a simple WAV file with silence
        // This creates a valid WAV header followed by silence
        var sampleRate = 44100;
        var channels = 2;
        var bitsPerSample = 16;
        var duration = Math.Max(1, text.Length / 20); // Rough estimate: 20 chars per second
        var dataLength = sampleRate * channels * (bitsPerSample / 8) * duration;
        
        var wavData = new List<byte>();
        
        // WAV header
        wavData.AddRange(System.Text.Encoding.ASCII.GetBytes("RIFF"));
        wavData.AddRange(BitConverter.GetBytes(36 + dataLength)); // File size - 8
        wavData.AddRange(System.Text.Encoding.ASCII.GetBytes("WAVE"));
        
        // Format chunk
        wavData.AddRange(System.Text.Encoding.ASCII.GetBytes("fmt "));
        wavData.AddRange(BitConverter.GetBytes(16)); // Format chunk size
        wavData.AddRange(BitConverter.GetBytes((short)1)); // PCM format
        wavData.AddRange(BitConverter.GetBytes((short)channels));
        wavData.AddRange(BitConverter.GetBytes(sampleRate));
        wavData.AddRange(BitConverter.GetBytes(sampleRate * channels * (bitsPerSample / 8))); // Byte rate
        wavData.AddRange(BitConverter.GetBytes((short)(channels * (bitsPerSample / 8)))); // Block align
        wavData.AddRange(BitConverter.GetBytes((short)bitsPerSample));
        
        // Data chunk
        wavData.AddRange(System.Text.Encoding.ASCII.GetBytes("data"));
        wavData.AddRange(BitConverter.GetBytes(dataLength));
        
        // Audio data (silence)
        for (int i = 0; i < dataLength; i++)
        {
            wavData.Add(0);
        }
        
        _logger.LogDebug("Generated {Length} bytes of mock audio data", wavData.Count);
        return wavData.ToArray();
    }
}

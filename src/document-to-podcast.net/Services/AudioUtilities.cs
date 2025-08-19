using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace DocumentToPodcast.Services;

public static class AudioUtilities
{
    public static byte[] CombineAudioSegments(List<byte[]> audioSegments, float silencePadSeconds = 1.0f)
    {
        if (audioSegments.Count == 0)
            return Array.Empty<byte>();

        if (audioSegments.Count == 1)
            return audioSegments[0];

        try
        {
            // Create a list to hold sample providers
            var sampleProviders = new List<ISampleProvider>();

            foreach (var audioSegment in audioSegments)
            {
                if (audioSegment.Length == 0) continue;

                // Convert byte array to sample provider
                using var memoryStream = new MemoryStream(audioSegment);
                try
                {
                    using var waveFileReader = new WaveFileReader(memoryStream);
                    var sampleProvider = waveFileReader.ToSampleProvider();
                    
                    // Store samples in memory since we can't keep the reader open
                    var samples = new List<float>();
                    var buffer = new float[1024];
                    int read;
                    while ((read = sampleProvider.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        for (int i = 0; i < read; i++)
                        {
                            samples.Add(buffer[i]);
                        }
                    }

                    // Create a sample provider from the stored samples
                    var cachedProvider = new CachedSampleProvider(samples.ToArray(), waveFileReader.WaveFormat);
                    sampleProviders.Add(cachedProvider);

                    // Add silence between segments
                    if (audioSegment != audioSegments.Last())
                    {
                        var silenceSamples = (int)(waveFileReader.WaveFormat.SampleRate * waveFileReader.WaveFormat.Channels * silencePadSeconds);
                        var silenceFormat = WaveFormat.CreateIeeeFloatWaveFormat(waveFileReader.WaveFormat.SampleRate, waveFileReader.WaveFormat.Channels);
                        var silenceProvider = new SilenceSampleProvider(silenceFormat, silenceSamples);
                        sampleProviders.Add(silenceProvider);
                    }
                }
                catch (Exception ex)
                {
                    // If we can't read as WAV, skip this segment
                    Console.WriteLine($"Skipping invalid audio segment: {ex.Message}");
                    continue;
                }
            }

            if (sampleProviders.Count == 0)
                return Array.Empty<byte>();

            // Concatenate all sample providers
            var concatenated = new ConcatenatingSampleProvider(sampleProviders);

            // Convert back to byte array
            using var outputStream = new MemoryStream();
            // Use a standard format: 44.1kHz, 16-bit, stereo
            var waveFormat = WaveFormat.CreateIeeeFloatWaveFormat(44100, 2);
            using var writer = new WaveFileWriter(outputStream, waveFormat);

            var outputBuffer = new float[4096];
            int bytesRead;
            while ((bytesRead = concatenated.Read(outputBuffer, 0, outputBuffer.Length)) > 0)
            {
                writer.WriteSamples(outputBuffer, 0, bytesRead);
            }

            return outputStream.ToArray();
        }
        catch (Exception ex)
        {
            // If audio combination fails, just return the first segment
            Console.WriteLine($"Audio combination failed: {ex.Message}");
            return audioSegments.First();
        }
    }

    public static async Task SaveAudioToFileAsync(byte[] audioData, string filePath)
    {
        await File.WriteAllBytesAsync(filePath, audioData);
    }
}

// Helper class to cache samples in memory
public class CachedSampleProvider : ISampleProvider
{
    private readonly float[] _samples;
    private int _position;
    
    public WaveFormat WaveFormat { get; }

    public CachedSampleProvider(float[] samples, WaveFormat sourceFormat)
    {
        _samples = samples;
        WaveFormat = WaveFormat.CreateIeeeFloatWaveFormat(sourceFormat.SampleRate, sourceFormat.Channels);
        _position = 0;
    }

    public int Read(float[] buffer, int offset, int count)
    {
        int samplesRemaining = _samples.Length - _position;
        int samplesToRead = Math.Min(count, samplesRemaining);
        
        Array.Copy(_samples, _position, buffer, offset, samplesToRead);
        _position += samplesToRead;
        
        return samplesToRead;
    }
}

// Helper class to generate silence
public class SilenceSampleProvider : ISampleProvider
{
    private int _remainingSamples;
    
    public WaveFormat WaveFormat { get; }

    public SilenceSampleProvider(WaveFormat waveFormat, int sampleCount)
    {
        WaveFormat = waveFormat;
        _remainingSamples = sampleCount;
    }

    public int Read(float[] buffer, int offset, int count)
    {
        int samplesToGenerate = Math.Min(count, _remainingSamples);
        
        // Fill with silence (zeros)
        for (int i = 0; i < samplesToGenerate; i++)
        {
            buffer[offset + i] = 0.0f;
        }
        
        _remainingSamples -= samplesToGenerate;
        return samplesToGenerate;
    }
}

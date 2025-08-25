using Xunit;
using DocumentToPodcast.Services;
using DocumentToPodcast.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Threading.Tasks;
using VerifyXunit;
using VerifyTests;
using System.IO;

namespace DocumentToPodcast.Net.Tests.Integration
{
    public class KokoroSharpTextToSpeechServiceIntegrationTest
    {
        [Fact]
        [Trait("Category", "Integration")]
        public async Task ConvertToSpeechAsync_GeneratesAudioSnapshot()
        {
            var loggerFactory = new LoggerFactory();
            var logger = loggerFactory.CreateLogger<KokoroSharpTextToSpeechService>();
            var options = Options.Create(new PodcastGeneratorOptions { OnnxModelPath = "dummy" }); // Not used by KokoroSharp
            var service = new KokoroSharpTextToSpeechService(logger, options);
            var audio = await service.ConvertToSpeechAsync("Testing KokoroSharp speech synthesis.", "female_1", "");
            await Verifier.Verify(audio);
        }

        [Fact]
        [Trait("Category", "Integration")]
        public async Task GeneratePodcastFromLmStudioScriptJson()
        {
            var loggerFactory = new LoggerFactory();
            var logger = loggerFactory.CreateLogger<KokoroSharpTextToSpeechService>();
            var options = Options.Create(new PodcastGeneratorOptions { OnnxModelPath = "dummy" });
            var service = new KokoroSharpTextToSpeechService(logger, options);

            // Read JSON from file (replicate path logic from ONNX test)
            var parentDir = Directory.GetParent(Directory.GetCurrentDirectory());
            var jsonPath = Path.Combine(parentDir.FullName, "net10.0", "Integration", "LmStudioPodcastScript.json");
            var lmStudioJson = await File.ReadAllTextAsync(jsonPath);

            var content = System.Text.Json.JsonDocument.Parse(lmStudioJson)
                    .RootElement.GetProperty("choices")[0]
                    .GetProperty("message")
                    .GetProperty("content")
                    .GetString();

            Assert.False(string.IsNullOrWhiteSpace(content), "Podcast script content is null or empty.");
            var lines = content.Split('\n');
            var audioSegments = new System.Collections.Generic.List<byte[]>();
            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (trimmed.StartsWith("\"Speaker 1\":"))
                {
                    var text = trimmed.Substring("\"Speaker 1\":".Length).Trim();
                    var audio = await service.ConvertToSpeechAsync(text, "female_1", "");
                    audioSegments.Add(audio);
                }
                else if (trimmed.StartsWith("\"Speaker 2\":"))
                {
                    var text = trimmed.Substring("\"Speaker 2\":".Length).Trim();
                    var audio = await service.ConvertToSpeechAsync(text, "male_1", "");
                    audioSegments.Add(audio);
                }
            }

            // Debug: Convert float PCM to Int16, write WAV header, check endianness, normalization, and channel info
            // Assume audioSegments[0] is float PCM, 1 channel, 16kHz, little-endian
            float[]? floatPcm = null;
            if (audioSegments.Count > 0 && audioSegments[0] != null && audioSegments[0].Length > 44)
            {
                int sampleCount = (audioSegments[0].Length - 44) / 4;
                floatPcm = new float[sampleCount];
                Buffer.BlockCopy(audioSegments[0], 44, floatPcm, 0, sampleCount * 4);
            }
            // Normalize and clamp
            short[]? int16Pcm = null;
            if (floatPcm != null)
            {
                int16Pcm = new short[floatPcm.Length];
                for (int i = 0; i < floatPcm.Length; i++)
                {
                    float sample = floatPcm[i];
                    if (float.IsNaN(sample) || float.IsInfinity(sample)) sample = 0f;
                    sample = Math.Max(-1.0f, Math.Min(1.0f, sample));
                    int16Pcm[i] = (short)(sample * 32767);
                }
            }
            // Write WAV header for PCM 16-bit mono
            byte[]? wavBytes = null;
            if (int16Pcm != null)
            {
                int sampleRate = 16000;
                int numChannels = 1;
                int bitsPerSample = 16;
                int blockAlign = numChannels * bitsPerSample / 8;
                int byteRate = sampleRate * blockAlign;
                int dataSize = int16Pcm.Length * 2;
                int fileSize = 44 + dataSize;
                wavBytes = new byte[fileSize];
                // RIFF header
                System.Text.Encoding.ASCII.GetBytes("RIFF").CopyTo(wavBytes, 0);
                BitConverter.GetBytes(fileSize - 8).CopyTo(wavBytes, 4);
                System.Text.Encoding.ASCII.GetBytes("WAVE").CopyTo(wavBytes, 8);
                System.Text.Encoding.ASCII.GetBytes("fmt ").CopyTo(wavBytes, 12);
                BitConverter.GetBytes(16).CopyTo(wavBytes, 16); // PCM header size
                BitConverter.GetBytes((short)1).CopyTo(wavBytes, 20); // AudioFormat = 1 (PCM)
                BitConverter.GetBytes((short)numChannels).CopyTo(wavBytes, 22);
                BitConverter.GetBytes(sampleRate).CopyTo(wavBytes, 24);
                BitConverter.GetBytes(byteRate).CopyTo(wavBytes, 28);
                BitConverter.GetBytes((short)blockAlign).CopyTo(wavBytes, 32);
                BitConverter.GetBytes((short)bitsPerSample).CopyTo(wavBytes, 34);
                System.Text.Encoding.ASCII.GetBytes("data").CopyTo(wavBytes, 36);
                BitConverter.GetBytes(dataSize).CopyTo(wavBytes, 40);
                // PCM data (little-endian)
                for (int i = 0; i < int16Pcm.Length; i++)
                {
                    byte[] bytes = BitConverter.GetBytes(int16Pcm[i]);
                    wavBytes[44 + i * 2] = bytes[0];
                    wavBytes[44 + i * 2 + 1] = bytes[1];
                }
            }
            // Output for manual inspection
            var outputPath = Path.Combine(parentDir.FullName, "GeneratedAudio", "KokoroSharpPodcast_Debug.wav");
            var outputDir = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(outputDir))
            {
                Directory.CreateDirectory(outputDir);
            }
            if (wavBytes != null)
            {
                File.WriteAllBytes(outputPath, wavBytes);
            }
            for(int i = 0; i < audioSegments.Count; i++)
            {
                var segmentPath = Path.Combine(parentDir.FullName, "GeneratedAudio", $"KokoroSharpPodcast_Segment_{i + 1}.wav");
                var segmentDir = Path.GetDirectoryName(segmentPath);
                if (!string.IsNullOrEmpty(segmentDir))
                {
                    Directory.CreateDirectory(segmentDir);
                }
                if (audioSegments[i] != null)
                {
                    File.WriteAllBytes(segmentPath, audioSegments[i]);
                }
            }
            // Log debug info
            if (floatPcm != null)
            {
                System.Diagnostics.Debug.WriteLine($"First 10 float samples: {string.Join(", ", floatPcm.Take(10))}");
            }
            if (int16Pcm != null)
            {
                System.Diagnostics.Debug.WriteLine($"First 10 int16 samples: {string.Join(", ", int16Pcm.Take(10))}");
            }
        }
    }
}

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

            // Write to WAV file for inspection
            for (int i = 0; i < audioSegments.Count; i++)
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

            // using naudiodotnet to concatenate WAV files
            var concatenatedPath = Path.Combine(parentDir.FullName, "GeneratedAudio", "KokoroSharpPodcast_Concatenated.wav");
            var segmentFiles = new System.Collections.Generic.List<string>();
            for (int i = 0; i < audioSegments.Count; i++)
            {
                var segmentPath = Path.Combine(parentDir.FullName, "GeneratedAudio", $"KokoroSharpPodcast_Segment_{i + 1}.wav");
                if (File.Exists(segmentPath))
                {
                    segmentFiles.Add(segmentPath);
                }
            }
            if (segmentFiles.Count > 0)
            {
                using (var waveFile = new NAudio.Wave.WaveFileWriter(concatenatedPath, new NAudio.Wave.WaveFormat(22050, 16, 1)))
                {
                    foreach (var segmentFile in segmentFiles)
                    {
                        using (var reader = new NAudio.Wave.WaveFileReader(segmentFile))
                        {
                            reader.CopyTo(waveFile);
                        }
                    }
                }
            }
            Assert.True(File.Exists(concatenatedPath), "Concatenated WAV file was not created.");
        }
    }
}

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
    public class OnnxTextToSpeechServiceIntegrationTest
    {
        [Fact]
        [Trait("Category", "Integration")]
        public async Task ConvertToSpeechAsync_GeneratesAudioSnapshot()
        {
            var loggerFactory = new LoggerFactory();
            var logger = loggerFactory.CreateLogger<OnnxTextToSpeechService>();
            var downloaderLogger = loggerFactory.CreateLogger<OnnxModelDownloader>();

            // Use the same ONNX model path as the running application, with null check
            var parentDir = Directory.GetParent(Directory.GetCurrentDirectory());
            if (parentDir == null)
            {
                throw new DirectoryNotFoundException("Could not determine parent directory for ONNX model path.");
            }
            var appModelPath = Path.Combine(parentDir.FullName, "models", "onnx", "model.onnx");

            var options = Options.Create(new PodcastGeneratorOptions { OnnxModelPath = appModelPath });

            // Proper DI for OnnxModelDownloader
            var httpClient = new System.Net.Http.HttpClient();
            var modelDownloader = new OnnxModelDownloader(httpClient, downloaderLogger, options);

            var service = new OnnxTextToSpeechService(logger, options, modelDownloader);
            var audio = await service.ConvertToSpeechAsync("Testing ONNX speech synthesis.", "female_1", "");
            await Verifier.Verify(audio);
        }

        [Fact]
        [Trait("Category", "Integration")]
        public async Task GeneratePodcastFromLmStudioScriptJson()
        {
            var loggerFactory = new LoggerFactory();
            var logger = loggerFactory.CreateLogger<OnnxTextToSpeechService>();
            var downloaderLogger = loggerFactory.CreateLogger<OnnxModelDownloader>();

            var parentDir = Directory.GetParent(Directory.GetCurrentDirectory());
            if (parentDir == null)
            {
                throw new DirectoryNotFoundException("Could not determine parent directory for ONNX model path.");
            }
            var appModelPath = Path.Combine(parentDir.FullName, "src", "document-to-podcast.net", "OuteTTS.onnx");
            var options = Options.Create(new PodcastGeneratorOptions { OnnxModelPath = appModelPath });
            var httpClient = new System.Net.Http.HttpClient();
            var modelDownloader = new OnnxModelDownloader(httpClient, downloaderLogger, options);
            var service = new OnnxTextToSpeechService(logger, options, modelDownloader);

            // Read JSON from file
            var jsonPath = Path.Combine(parentDir.FullName, "net10.0", "Integration", "LmStudioPodcastScript.json");
            var lmStudioJson = await File.ReadAllTextAsync(jsonPath);

            // Extract the podcast script from the JSON
            var content = System.Text.Json.JsonDocument.Parse(lmStudioJson)
                    .RootElement.GetProperty("choices")[0]
                    .GetProperty("message")
                    .GetProperty("content")
                    .GetString();

            // Split by speakers and generate audio for each part
            Assert.False(string.IsNullOrWhiteSpace(content), "Podcast script content is null or empty.");
            var lines = content.Split('\n');
            var audioSegments = new System.Collections.Generic.List<byte[]>();
            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (trimmed.StartsWith("\"Speaker 1\":"))
                {
                    var text = trimmed.Substring(trimmed.IndexOf(':') + 1).Trim(' ', '"');
                    var audio = await service.ConvertToSpeechAsync(text, "Speaker1", "");
                    Assert.NotNull(audio);
                    Assert.True(audio.Length > 44, "WAV file too small");
                    Assert.Equal((byte)'R', audio[0]);
                    Assert.Equal((byte)'I', audio[1]);
                    Assert.Equal((byte)'F', audio[2]);
                    Assert.Equal((byte)'F', audio[3]);
                    Assert.Equal((byte)'W', audio[8]);
                    Assert.Equal((byte)'A', audio[9]);
                    Assert.Equal((byte)'V', audio[10]);
                    Assert.Equal((byte)'E', audio[11]);
                    audioSegments.Add(audio);
                }
                else if (trimmed.StartsWith("\"Speaker 2\":"))
                {
                    var text = trimmed.Substring(trimmed.IndexOf(':') + 1).Trim(' ', '"');
                    var audio = await service.ConvertToSpeechAsync(text, "Speaker2", "");
                    Assert.NotNull(audio);
                    Assert.True(audio.Length > 44, "WAV file too small");
                    Assert.Equal((byte)'R', audio[0]);
                    Assert.Equal((byte)'I', audio[1]);
                    Assert.Equal((byte)'F', audio[2]);
                    Assert.Equal((byte)'F', audio[3]);
                    Assert.Equal((byte)'W', audio[8]);
                    Assert.Equal((byte)'A', audio[9]);
                    Assert.Equal((byte)'V', audio[10]);
                    Assert.Equal((byte)'E', audio[11]);
                    audioSegments.Add(audio);
                }
            }

            // Output each audio segment as a separate WAV file
            Assert.True(audioSegments.Count > 0);
            var outputDir = Path.Combine(parentDir.FullName, "tests", "document-to-podcast.net.tests", "Integration", "GeneratedAudio");
            Directory.CreateDirectory(outputDir);
            for (int i = 0; i < audioSegments.Count; i++)
            {
                var filePath = Path.Combine(outputDir, $"PodcastSegment_{i + 1}.wav");
                await File.WriteAllBytesAsync(filePath, audioSegments[i]);
            }

            // Properly join all segments into one WAV file using NAudio
            var joinedPath = Path.Combine(outputDir, "PodcastJoined.wav");
            using (var writer = new NAudio.Wave.WaveFileWriter(joinedPath, new NAudio.Wave.WaveFileReader(new MemoryStream(audioSegments[0])).WaveFormat))
            {
                for (int i = 0; i < audioSegments.Count; i++)
                {
                    using (var reader = new NAudio.Wave.WaveFileReader(new MemoryStream(audioSegments[i])))
                    {
                        var buffer = new byte[reader.Length];
                        int read = reader.Read(buffer, 0, buffer.Length);
                        writer.Write(buffer, 0, read);
                    }
                }
            }

            await Verifier.Verify(audioSegments);
        }
    }
}
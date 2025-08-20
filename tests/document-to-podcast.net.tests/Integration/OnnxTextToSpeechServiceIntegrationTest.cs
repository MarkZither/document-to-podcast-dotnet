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
            var appModelPath = Path.Combine(parentDir.FullName, "src", "document-to-podcast.net", "OuteTTS.onnx");

            var options = Options.Create(new PodcastGeneratorOptions { OnnxModelPath = appModelPath });

            // Proper DI for OnnxModelDownloader
            var httpClient = new System.Net.Http.HttpClient();
            var modelDownloader = new OnnxModelDownloader(httpClient, downloaderLogger, options);

            var service = new OnnxTextToSpeechService(logger, options, modelDownloader);
            var audio = await service.ConvertToSpeechAsync("Testing ONNX speech synthesis.", "female_1", "");
            await Verifier.Verify(audio);
        }
    }
}


using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using DocumentToPodcast.Services;
using DocumentToPodcast.Configuration;
using System.Threading.Tasks;

namespace DocumentToPodcast.Net.Tests.Unit
{
    public class OnnxTextToSpeechServiceTest
    {
        [Fact]
        [Trait("Category", "Unit")]
        public async Task ConvertToSpeechAsync_UsesFallback_WhenSessionIsNull()
        {
            var loggerMock = new Mock<ILogger<OnnxTextToSpeechService>>();
            var optionsMock = new Mock<IOptions<PodcastGeneratorOptions>>();
            optionsMock.Setup(o => o.Value).Returns(new PodcastGeneratorOptions());
            var modelDownloaderMock = new Mock<OnnxModelDownloader>();
            modelDownloaderMock.Setup(m => m.EnsureModelAvailableAsync(It.IsAny<string>())).ReturnsAsync("dummy.onnx");
            modelDownloaderMock.Setup(m => m.GetModelInfoAsync(It.IsAny<string>())).ReturnsAsync(new OnnxModelDownloader.ModelInfo("Fallback", "1.0", 24000));
            var service = new OnnxTextToSpeechService(loggerMock.Object, optionsMock.Object, modelDownloaderMock.Object);
            var result = await service.ConvertToSpeechAsync("Hello world", "male_1", "");
            Assert.NotNull(result);
            Assert.True(result.Length > 0);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public void Constructor_LogsWarning_WhenModelFileMissing()
        {
            var loggerMock = new Mock<ILogger<OnnxTextToSpeechService>>();
            var optionsMock = new Mock<IOptions<PodcastGeneratorOptions>>();
            optionsMock.Setup(o => o.Value).Returns(new PodcastGeneratorOptions());
            var modelDownloaderMock = new Mock<OnnxModelDownloader>();
            modelDownloaderMock.Setup(m => m.EnsureModelAvailableAsync(It.IsAny<string>())).ReturnsAsync("missing.onnx");
            modelDownloaderMock.Setup(m => m.GetModelInfoAsync(It.IsAny<string>())).ReturnsAsync(new OnnxModelDownloader.ModelInfo("Fallback", "1.0", 24000));
            var service = new OnnxTextToSpeechService(loggerMock.Object, optionsMock.Object, modelDownloaderMock.Object);
            loggerMock.Verify(l => l.Log(
                It.Is<LogLevel>(lvl => lvl == LogLevel.Warning),
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                (Func<It.IsAnyType, Exception?, string>)It.IsAny<object>()), Times.AtLeastOnce());
        }

        [Fact]
        [Trait("Category", "Unit")]
        public async Task ConvertToSpeechAsync_ReturnsValidWavFile()
        {
            // Arrange
            var logger = new LoggerFactory().CreateLogger<OnnxTextToSpeechService>();
            var options = Options.Create(new PodcastGeneratorOptions { OnnxModelPath = "dummy.onnx" });
            var modelDownloader = new OnnxModelDownloader(new System.Net.Http.HttpClient(), new LoggerFactory().CreateLogger<OnnxModelDownloader>(), options);
            var service = new OnnxTextToSpeechService(logger, options, modelDownloader);

            // Act
            var audio = await service.ConvertToSpeechAsync("Test audio", "test", "");

            // Assert: Check RIFF/WAVE header
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

            // Further validation: check for all-zero or NaN sample data
            // WAV PCM data starts at offset 44 for standard header
            var pcmData = new float[(audio.Length - 44) / 4];
            Buffer.BlockCopy(audio, 44, pcmData, 0, pcmData.Length * 4);
            bool allZero = true;
            bool anyNaN = false;
            for (int i = 0; i < Math.Min(pcmData.Length, 100); i++)
            {
                if (pcmData[i] != 0f) allZero = false;
                if (float.IsNaN(pcmData[i])) anyNaN = true;
            }
            // Log first 10 sample values
            System.Diagnostics.Debug.WriteLine("First 10 PCM samples:");
            for (int i = 0; i < Math.Min(10, pcmData.Length); i++)
            {
                System.Diagnostics.Debug.WriteLine($"Sample[{i}] = {pcmData[i]}");
            }
            Assert.False(allZero, "PCM data is all zeros (silent or invalid)");
            Assert.False(anyNaN, "PCM data contains NaN values (invalid)");
        }
    }
}

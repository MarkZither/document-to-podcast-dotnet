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
                (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()), Times.AtLeastOnce());
        }
    }
}

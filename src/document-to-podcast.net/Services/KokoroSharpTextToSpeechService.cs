using DocumentToPodcast.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Threading.Tasks;
using KokoroSharp.Core;
using KokoroSharp.Processing;
using KokoroSharp.Utilities;
using KokoroSharp;
using Microsoft.ML.OnnxRuntime;
using System.Text;

namespace DocumentToPodcast.Services
{
    public class KokoroSharpTextToSpeechService : ITextToSpeechService
    {
        private readonly ILogger<KokoroSharpTextToSpeechService> _logger;
        private readonly IOptions<PodcastGeneratorOptions> _options;
        private readonly KokoroWavSynthesizer _ttsWav;
        private readonly KokoroTTS _tts;

        public KokoroSharpTextToSpeechService(ILogger<KokoroSharpTextToSpeechService> logger, IOptions<PodcastGeneratorOptions> options)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _options = options ?? throw new ArgumentNullException(nameof(options));
            var v = new SessionOptions();
            _tts = KokoroTTS.LoadModel(sessionOptions: v); // Configure as needed
            _ttsWav = new KokoroWavSynthesizer("kokoro.onnx");
        }

        public async Task<byte[]> ConvertToSpeechAsync(string text, string voice, string language)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                throw new ArgumentException("Text cannot be null or empty.", nameof(text));
            }
            if(voice == "female_1"){
                voice = "bf_isabella";
            }
            if(voice == "male_1"){
                voice = "bm_george";
            }
            var voiceObj = KokoroVoiceManager.GetVoice(voice);
            if (voiceObj == null)
            {
                throw new ArgumentException($"Voice '{voice}' not found.", nameof(voice));
            }
            // Synthesize audio bytes
            var audioBytes = await _ttsWav.SynthesizeAsync(text, voiceObj);
            // Save to file using SaveAudioToFile
            var tempPath = Path.Combine(Path.GetTempPath(), $"kokoro_{Guid.NewGuid()}.wav");
            _ttsWav.SaveAudioToFile(audioBytes, tempPath);
            var wavBytes = await File.ReadAllBytesAsync(tempPath);
            try { File.Delete(tempPath); } catch { }
            return wavBytes;
        }

        public static byte[] CreateWavFromFloatPCM(float[] pcmSamples, int sampleRate = 22050)
        {
            using (var ms = new MemoryStream())
            using (var writer = new BinaryWriter(ms))
            {
                int byteRate = sampleRate * 4; // 4 bytes per float sample
                int dataLength = pcmSamples.Length * 4;

                // Write WAV header
                writer.Write(Encoding.ASCII.GetBytes("RIFF"));
                writer.Write(36 + dataLength); // File size - 8
                writer.Write(Encoding.ASCII.GetBytes("WAVE"));

                // fmt chunk
                writer.Write(Encoding.ASCII.GetBytes("fmt "));
                writer.Write(16); // Subchunk1Size (16 for PCM)
                writer.Write((short)3); // AudioFormat = 3 (IEEE float)
                writer.Write((short)1); // NumChannels
                writer.Write(sampleRate);
                writer.Write(byteRate);
                writer.Write((short)4); // BlockAlign (4 bytes per sample)
                writer.Write((short)32); // BitsPerSample

                // data chunk
                writer.Write(Encoding.ASCII.GetBytes("data"));
                writer.Write(dataLength);
                foreach (var sample in pcmSamples)
                    writer.Write(sample);

                writer.Flush();
                return ms.ToArray();
            }
        }
    }
}

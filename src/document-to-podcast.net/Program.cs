using DocumentToPodcast.Services;
using DocumentToPodcast.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Serilog;
using MarkItDownSharp;

namespace DocumentToPodcast;

public class Program
{
    public static async Task<int> Main(string[] args)
    {
        // Configure Serilog
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .CreateLogger();

        try
        {
            var host = CreateHostBuilder(args).Build();
            var options = host.Services.GetRequiredService<IOptions<PodcastGeneratorOptions>>().Value;
            
            // Simple argument parsing for now
            string? inputFile = null;
            string? outputFolder = null;
            string modelEndpoint = options.DefaultModelEndpoint;
            string? configFile = null;

            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "--input-file" or "-i" when i + 1 < args.Length:
                        inputFile = args[++i];
                        break;
                    case "--output-folder" or "-o" when i + 1 < args.Length:
                        outputFolder = args[++i];
                        break;
                    case "--model-endpoint" or "-m" when i + 1 < args.Length:
                        modelEndpoint = args[++i];
                        break;
                    case "--config-file" or "-c" when i + 1 < args.Length:
                        configFile = args[++i];
                        break;
                    case "--help" or "-h":
                        ShowHelp();
                        return 0;
                }
            }

            if (configFile == null && inputFile == null)
            {
                Console.WriteLine("Error: --input-file is required unless --config-file is provided.");
                ShowHelp();
                return 1;
            }

            // If no output folder specified, use temp directory
            if (outputFolder == null)
            {
                var tempDir = Path.GetTempPath();
                var appTempDir = Path.Combine(tempDir, "document-to-podcast");
                Directory.CreateDirectory(appTempDir);
                outputFolder = appTempDir;
                Console.WriteLine($"No output folder specified, using temporary directory: {outputFolder}");
            }

            var podcastService = host.Services.GetRequiredService<IPodcastGeneratorService>();
            await podcastService.GeneratePodcastAsync(inputFile!, outputFolder!, modelEndpoint, configFile);
            
            return 0;
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Application terminated unexpectedly");
            return 1;
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    private static void ShowHelp()
    {
        Console.WriteLine("Document to Podcast Generator (.NET with Semantic Kernel & ONNX)");
        Console.WriteLine();
        Console.WriteLine("Usage:");
        Console.WriteLine("  document-to-podcast.net --input-file <path> [--output-folder <path>] [options]");
        Console.WriteLine("  document-to-podcast.net --config-file <path>");
        Console.WriteLine();
        Console.WriteLine("Options:");
        Console.WriteLine("  -i, --input-file <path>     The path to the input file");
        Console.WriteLine("  -o, --output-folder <path>  The path to the output folder (default: temp directory)");
        Console.WriteLine("  -m, --model-endpoint <url>  The model API endpoint (default: from appsettings.json)");
        Console.WriteLine("  -c, --config-file <path>    Path to configuration file");
        Console.WriteLine("  -h, --help                  Show this help message");
        Console.WriteLine();
        Console.WriteLine("Configuration Options:");
        Console.WriteLine("  UseSemanticKernel: true/false (default: true) - Use Semantic Kernel for AI orchestration");
        Console.WriteLine("  UseOnnxTts: true/false (default: true) - Use ONNX Runtime for local TTS");
        Console.WriteLine("  UseRealTextToSpeech: true/false (default: false) - Use OpenAI TTS API");
        Console.WriteLine();
        Console.WriteLine("Examples:");
        Console.WriteLine("  # Use ONNX TTS + Semantic Kernel with Ollama (default, output to temp)");
        Console.WriteLine("  dotnet run -- --input-file \"document.pdf\"");
        Console.WriteLine();
        Console.WriteLine("  # Specify custom output folder");
        Console.WriteLine("  dotnet run -- --input-file \"document.pdf\" --output-folder \"C:\\MyPodcasts\"");
        Console.WriteLine();
        Console.WriteLine("  # Configure for OpenAI (both text and TTS)");
        Console.WriteLine("  dotnet user-secrets set \"PodcastGenerator:SemanticKernel:UseOpenAI\" \"true\"");
        Console.WriteLine("  dotnet user-secrets set \"PodcastGenerator:OpenAI:ApiKey\" \"sk-...\"");
        Console.WriteLine("  dotnet user-secrets set \"PodcastGenerator:UseRealTextToSpeech\" \"true\"");
        Console.WriteLine();
        Console.WriteLine("  # Use Ollama for text + ONNX for TTS (recommended)");
        Console.WriteLine("  dotnet user-secrets set \"PodcastGenerator:UseOnnxTts\" \"true\"");
        Console.WriteLine("  dotnet user-secrets set \"PodcastGenerator:SemanticKernel:UseOllama\" \"true\"");
    }

    private static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((context, config) =>
            {
                config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
                config.AddJsonFile($"appsettings.{context.HostingEnvironment.EnvironmentName}.json", optional: true);
                config.AddUserSecrets<Program>();
                config.AddEnvironmentVariables();
                config.AddCommandLine(args);
            })
            .UseSerilog()
            .ConfigureServices((context, services) =>
            {
                // Configuration
                services.Configure<PodcastGeneratorOptions>(
                    context.Configuration.GetSection(PodcastGeneratorOptions.SectionName));

                // HTTP Client
                services.AddHttpClient();

                // Model downloader service
                services.AddSingleton<OnnxModelDownloader>();

                // Get options for conditional registration
                var options = new PodcastGeneratorOptions();
                context.Configuration.GetSection(PodcastGeneratorOptions.SectionName).Bind(options);

                // Register Semantic Kernel
                if (options.UseSemanticKernel)
                {
                    var kernelBuilder = services.AddKernel();

                    if (options.SemanticKernel.UseOpenAI && !string.IsNullOrEmpty(options.OpenAI.ApiKey))
                    {
                        kernelBuilder.AddOpenAIChatCompletion(
                            options.OpenAI.Model,
                            options.OpenAI.ApiKey);
                    }
                    else if (options.SemanticKernel.UseOllama)
                    {
                        // Use our custom Ollama service instead of the OpenAI connector
                        services.AddSingleton<IChatCompletionService>(provider =>
                        {
                            var httpClientFactory = provider.GetRequiredService<IHttpClientFactory>();
                            var httpClient = httpClientFactory.CreateClient();

                            // Set extended timeout for AI inference
                            httpClient.Timeout = TimeSpan.FromMinutes(30);

                            var logger = provider.GetRequiredService<ILogger<OllamaChatCompletionService>>();
                            logger.LogInformation("Configured HttpClient timeout to 10 minutes for AI inference");

                            return new OllamaChatCompletionService(
                                httpClient,
                                logger,
                                options.SemanticKernel.OllamaModel,
                                options.SemanticKernel.OllamaEndpoint);
                        });
                    }

                    services.AddScoped<ITextToTextService, SemanticKernelTextToTextService>();

                    // Add MarkItDown to parse the documents
                    services.AddMarkItDown();
                }
                else
                {
                    services.AddScoped<ITextToTextService, OpenAIApiTextToTextService>();
                }

                // Register TTS Service
                if (options.UseOnnxTts)
                {
                    services.AddSingleton<ITextToSpeechService, OnnxTextToSpeechService>();
                }
                else if (options.UseRealTextToSpeech)
                {
                    services.AddSingleton<ITextToSpeechService>(provider =>
                    {
                        var httpClientFactory = provider.GetRequiredService<IHttpClientFactory>();
                        var httpClient = httpClientFactory.CreateClient();
                        return new OpenAIApiTextToSpeechService(
                            httpClient,
                            provider.GetRequiredService<ILogger<OpenAIApiTextToSpeechService>>());
                    });
                }
                else
                {
                    services.AddSingleton<ITextToSpeechService, MockTextToSpeechService>();
                }

                // Core services
                services.AddSingleton<IDocumentParserFactory, DocumentParserFactory>();
                services.AddSingleton<IPodcastGeneratorService, PodcastGeneratorService>();
            });
}

# Document to Podcast .NET

A .NET reimplementation of the document-to-podcast Python project. This version uses OpenAI-compatible APIs (like Ollama or LM Studio) for text generation and text-to-speech conversion.

## Features

- **Flexible Document Parsing**: Uses a factory pattern to support multiple document parsers
  - MarkItDown integration (Python service, local execution, or future .NET implementation)
  - Basic text file parsing
  - Extensible for additional formats

- **OpenAI-Compatible API Integration**: Works with any OpenAI-compatible API
  - Ollama (recommended for local deployment)
  - LM Studio
  - OpenAI API
  - Other compatible services

- **Audio Generation**: Full text-to-speech pipeline
  - OpenAI-compatible TTS API integration
  - Mock TTS service for testing and development
  - Audio segment combination and mixing
  - WAV file output generation

- **Modular Architecture**: Built with dependency injection and SOLID principles
  - Configurable services
  - Easy to extend and test
  - Clean separation of concerns
  - Uses System.Text.Json for high-performance JSON serialization

## Prerequisites

- .NET 9.0 SDK
- An OpenAI-compatible API server (Ollama, LM Studio, etc.)
- Optional: Python with MarkItDown package for document parsing

## Installation

1. Clone the repository
2. Navigate to the .NET project directory:
   ```bash
   cd src/document-to-podcast.net
   ```
3. Restore dependencies:
   ```bash
   dotnet restore
   ```
4. Build the project:
   ```bash
   dotnet build
   ```

## Usage

### Command Line Interface

```bash
# Using individual parameters
dotnet run -- --input-file "path/to/document.pdf" --output-folder "output" --model-endpoint "http://localhost:11434"

# Using configuration file
dotnet run -- --config-file "config.json"

# Show help
dotnet run -- --help
```

### Configuration Management

The application uses the .NET configuration system with multiple sources:

1. **appsettings.json** (default settings)
2. **appsettings.Development.json** (development overrides)
3. **User Secrets** (secure API keys and personal settings)
4. **Environment Variables** (deployment configuration)
5. **Command Line Arguments** (runtime parameters)

#### Quick Configuration Examples

**Enable Real TTS with User Secrets:**
```bash
dotnet user-secrets set "PodcastGenerator:UseRealTextToSpeech" "true"
dotnet user-secrets set "PodcastGenerator:OpenAI:ApiKey" "sk-your-api-key"
```

**Check Current Secrets:**
```bash
dotnet user-secrets list
```

**Override via Environment Variables:**
```bash
$env:PodcastGenerator__UseRealTextToSpeech = "true"
$env:PodcastGenerator__OpenAI__ApiKey = "sk-your-key"
dotnet run -- --input-file "document.pdf" --output-folder "output"
```

### Configuration File

Create a JSON configuration file (see `sample-config.json` for example):

```json
{
  "InputFile": "sample.txt",
  "OutputFolder": "output",
  "TextToTextModel": "http://localhost:11434",
  "TextToSpeechModel": "http://localhost:11434",
  "Speakers": [
    {
      "Id": 1,
      "Name": "Laura",
      "Description": "The main host...",
      "VoiceProfile": "female_1"
    }
  ]
}
```

## Architecture

### Document Parsing Strategy

The application uses a factory pattern to handle different document types:

1. **MarkItDown.NET** (future): Native .NET implementation
2. **MarkItDown Service**: HTTP API call to Python service
3. **Local Python**: Execute MarkItDown via Python subprocess
4. **Basic Parsers**: Simple text file parsing

### API Integration

- **Text-to-Text**: Uses OpenAI chat completions API format
- **Text-to-Speech**: Uses OpenAI audio/speech API format
- **Streaming Support**: Supports streaming responses for real-time processing

### Services

- `IDocumentParserFactory`: Creates appropriate parsers for file types
- `ITextToTextService`: Handles LLM text generation
- `ITextToSpeechService`: Converts text to audio
- `IPodcastGeneratorService`: Orchestrates the entire pipeline

## Supported Document Formats

- `.txt` - Plain text files
- `.pdf` - PDF documents (via MarkItDown)
- `.docx` - Word documents (via MarkItDown)
- `.html` - HTML files (via MarkItDown)
- `.md` - Markdown files (via MarkItDown)

## Setting up Ollama

1. Install Ollama from <https://ollama.ai>
2. Pull a compatible model:

   ```bash
   ollama pull llama3.2
   ```

3. Start Ollama server (usually runs on <http://localhost:11434>)

## Future Enhancements

- [ ] Native MarkItDown.NET implementation
- [ ] Audio concatenation and mixing
- [ ] Voice cloning integration
- [ ] Podcast metadata generation
- [ ] Web API interface
- [ ] Docker containerization
- [ ] Advanced text processing and chunking
- [ ] Multiple audio format support

## Contributing

This is a reimplementation designed to be more modular and extensible than the original Python version. Contributions are welcome, especially for:

- Additional document parsers
- Audio processing improvements
- API integrations
- Performance optimizations

## License

Same as the original document-to-podcast project.

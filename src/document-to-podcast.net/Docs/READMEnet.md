# Document-to-Podcast (.NET)

<p align="center"><img src="https://raw.githubusercontent.com/mozilla-ai/document-to-podcast/main/images/Blueprints-logo.png" width="35%" alt="Mozilla.ai Blueprints logo"/></p>

**A modern .NET implementation of the Document-to-Podcast Blueprint by Mozilla.ai**

[![Build Status](https://img.shields.io/badge/build-passing-brightgreen)](#)
[![.NET](https://img.shields.io/badge/.NET-9.0-purple)](https://dotnet.microsoft.com/download/dotnet/9.0)
[![Semantic Kernel](https://img.shields.io/badge/Semantic%20Kernel-1.26.0-blue)](https://github.com/microsoft/semantic-kernel)
[![ONNX Runtime](https://img.shields.io/badge/ONNX%20Runtime-1.19.2-orange)](https://onnxruntime.ai/)

## 🎯 Overview

This project is a complete .NET 9.0 reimplementation of the original [Mozilla.ai Document-to-Podcast Blueprint](https://github.com/mozilla-ai/document-to-podcast), featuring modern C# architecture with **Microsoft Semantic Kernel** for AI orchestration and **ONNX Runtime** for local text-to-speech inference.

**Transform any document into an engaging podcast conversation between two AI speakers - completely locally, with no external API dependencies required.**

<div align="center">
<img src="https://raw.githubusercontent.com/mozilla-ai/document-to-podcast/main/images/document-to-podcast-diagram.png" width="100%" alt="Document-to-Podcast Architecture"/>
</div>

## ✨ Key Features

- 🚀 **Modern .NET 9.0** architecture with dependency injection and configuration patterns
- 🧠 **Microsoft Semantic Kernel** integration for flexible AI provider support
- 🏠 **100% Local Processing** - works completely offline with local models
- 🎙️ **ONNX Runtime TTS** - High-quality text-to-speech with automatic model downloading
- 📱 **Multiple AI Providers** - Supports OpenAI, Ollama, and custom endpoints
- 📄 **Multi-format Support** - PDF, Word, Markdown, HTML, and plain text
- ⚡ **Streaming Generation** - Real-time script generation with progress feedback
- 🔧 **Flexible Configuration** - JSON config files, user secrets, and environment variables

## 🎭 Inspiration & Credits

This .NET implementation is inspired by and builds upon the excellent work from:

- **[Mozilla.ai Document-to-Podcast Blueprint](https://github.com/mozilla-ai/document-to-podcast)** - The original Python implementation
- **[Mozilla.ai Blog: Introducing Blueprints](https://blog.mozilla.ai/introducing-blueprints-customizable-ai-workflows-for-developers/)** - Announcement and vision
- **[Mozilla.ai Blueprints Documentation](https://mozilla-ai.github.io/document-to-podcast/)** - Comprehensive guides and examples

## 🏗️ Architecture

### Built With
- **.NET 9.0** - Modern cross-platform framework
- **Microsoft Semantic Kernel 1.26.0** - AI orchestration and prompt management
- **ONNX Runtime 1.19.2** - Local machine learning inference
- **Microsoft.Extensions.*** - Dependency injection, configuration, logging, and hosting
- **NAudio** - Audio processing and WAV file generation
- **Serilog** - Structured logging
- **System.Text.Json** - High-performance JSON serialization

### Supported AI Providers
- **Ollama** (Recommended) - Local LLM inference
- **OpenAI API** - Cloud-based models
- **Any OpenAI-compatible API** - LM Studio, LocalAI, etc.

### Supported TTS Options
- **ONNX Runtime** (Default) - Local OuteTTS model with automatic downloading
- **OpenAI TTS API** - Cloud-based text-to-speech
- **Mock TTS** - Placeholder audio for testing

## 🚀 Quick Start

### Prerequisites
- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [Ollama](https://ollama.ai/) (for local AI inference)

### Installation

1. **Clone the repository:**
   ```bash
   git clone https://github.com/your-username/document-to-podcast-dotnet.git
   cd document-to-podcast-dotnet
   ```

2. **Install a local model with Ollama:**
   ```bash
   ollama pull phi3:medium-128k
   # or
   ollama pull llama3.2
   ```

3. **Build the project:**
   ```bash
   dotnet build
   ```

### Basic Usage

**Convert a document with default settings:**
```bash
dotnet run -- --input-file "path/to/your/document.pdf"
```

**Specify output location:**
```bash
dotnet run -- --input-file "document.pdf" --output-folder "C:\MyPodcasts"
```

**Use configuration file:**
```bash
dotnet run -- --config-file "my-config.json"
```

## ⚙️ Configuration

### Quick Configuration with User Secrets

```bash
# Use OpenAI for text generation
dotnet user-secrets set "PodcastGenerator:SemanticKernel:UseOpenAI" "true"
dotnet user-secrets set "PodcastGenerator:OpenAI:ApiKey" "your-api-key"

# Use OpenAI TTS (requires API key)
dotnet user-secrets set "PodcastGenerator:UseRealTextToSpeech" "true"

# Use local Ollama (recommended)
dotnet user-secrets set "PodcastGenerator:SemanticKernel:UseOllama" "true"
dotnet user-secrets set "PodcastGenerator:SemanticKernel:OllamaModel" "phi3:medium-128k"
```

### Configuration File Example

Create a `config.json` file:

```json
{
  "PodcastGenerator": {
    "UseSemanticKernel": true,
    "UseOnnxTts": true,
    "AutoDownloadModels": true,
    "SemanticKernel": {
      "UseOllama": true,
      "OllamaEndpoint": "http://localhost:11434",
      "OllamaModel": "phi3:medium-128k",
      "Temperature": 0.7,
      "MaxTokens": 4000
    }
  }
}
```

## 📖 Usage Examples

### Example 1: Local AI Pipeline (Recommended)
```bash
# Uses Ollama for text generation + ONNX for TTS
dotnet run -- --input-file "research-paper.pdf"
```

### Example 2: Hybrid Setup
```bash
# OpenAI for text + local ONNX for TTS
dotnet user-secrets set "PodcastGenerator:SemanticKernel:UseOpenAI" "true"
dotnet user-secrets set "PodcastGenerator:OpenAI:ApiKey" "sk-..."
dotnet run -- --input-file "document.pdf"
```

### Example 3: Full Cloud Setup
```bash
# OpenAI for both text and speech
dotnet user-secrets set "PodcastGenerator:UseRealTextToSpeech" "true"
dotnet user-secrets set "PodcastGenerator:SemanticKernel:UseOpenAI" "true"
dotnet run -- --input-file "document.pdf"
```

## 🎛️ Advanced Features

### Automatic Model Management
The application automatically downloads and manages ONNX models:
- **OuteTTS-0.2-500M** for high-quality text-to-speech
- Models cached in user profile directory
- Automatic verification and retry logic

### Streaming Generation
Real-time podcast script generation with progress feedback:
```bash
[19:25:43 INF] Starting AI inference - this may take several minutes...
[19:25:45 INF] Generated script with 2847 characters
[19:25:45 INF] Processing script into 23 dialogue segments
```

### Flexible Output Management
- **Default**: Temporary directory with automatic cleanup
- **Custom**: User-specified output folder
- **Gitignore-friendly**: All outputs excluded from source control

## 🔧 Development

### Project Structure
```
src/document-to-podcast.net/
├── Program.cs                     # Main entry point
├── Configuration/                 # Configuration models
├── Services/                      # Core business logic
│   ├── Interfaces.cs             # Service contracts
│   ├── PodcastGeneratorService.cs # Main orchestration
│   ├── SemanticKernelTextToTextService.cs
│   ├── OllamaChatCompletionService.cs
│   ├── OnnxTextToSpeechService.cs
│   ├── OnnxModelDownloader.cs
│   └── Parsers/                  # Document parsing
└── appsettings.json              # Default configuration
```

### Building from Source
```bash
git clone https://github.com/your-username/document-to-podcast-dotnet.git
cd document-to-podcast-dotnet
dotnet restore
dotnet build
dotnet test
```

## 🤝 Contributing

Contributions are welcome! This project follows the same community-driven approach as the original Mozilla.ai Blueprint.

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Add tests if applicable
5. Submit a pull request

## 📚 Related Projects

- **[Original Python Blueprint](https://github.com/mozilla-ai/document-to-podcast)** - The foundational implementation
- **[Mozilla.ai Blueprints](https://blog.mozilla.ai/introducing-blueprints-customizable-ai-workflows-for-developers/)** - The broader ecosystem
- **[Microsoft Semantic Kernel](https://github.com/microsoft/semantic-kernel)** - AI orchestration framework
- **[ONNX Runtime](https://onnxruntime.ai/)** - Cross-platform ML inference

## 📄 License

This project is licensed under the Apache 2.0 License - see the [LICENSE](LICENSE) file for details.

This is the same license as the original Mozilla.ai Blueprint, ensuring compatibility and alignment with the open-source ecosystem.

## 🙏 Acknowledgments

Special thanks to:
- **Mozilla.ai team** for creating the original Blueprint and vision
- **Microsoft Semantic Kernel team** for the excellent AI orchestration framework
- **ONNX Runtime team** for enabling local ML inference
- **Ollama community** for making local LLMs accessible

---

<div align="center">

**[🔗 Original Python Project](https://github.com/mozilla-ai/document-to-podcast)** | **[📖 Mozilla.ai Documentation](https://mozilla-ai.github.io/document-to-podcast/)** | **[🎯 Mozilla.ai Blog](https://blog.mozilla.ai/introducing-blueprints-customizable-ai-workflows-for-developers/)**

</div>


# Copilot Instructions for Document-to-Podcast .NET

## Architecture & Major Components

- **Modular, Service-Oriented**: Core logic is in `src/document-to-podcast.net/Services/`.
	- `PodcastGeneratorService`: Orchestrates document parsing, text generation (LLM), and text-to-speech.
	- `OnnxTextToSpeechService`: Converts text to speech using ONNX models (see also `MockTextToSpeechService` for testing).
	- `OpenAIApiTextToTextService`: Integrates with OpenAI-compatible APIs (Ollama, LM Studio, etc.) for LLM text generation.
	- `DocumentParserFactory`: Selects appropriate parser (MarkItDown, TextParser) based on file extension.
	- Interfaces in `Services/Interfaces.cs` define boundaries for extensibility and testing.

- **Configuration**: Centralized in `Configuration/` (see `PodcastConfig.cs`, `PodcastGeneratorOptions.cs`). Supports layered config via `appsettings.json`, environment variables, and CLI args.

- **Testing**: Unit and integration tests in `tests/document-to-podcast.net.tests/`.
	- Use Moq for mocking dependencies.
	- Use Verify (Simon Cropp) for snapshot testing of audio output.
	- Integration tests generate actual WAV files for manual inspection.

## Developer Workflows

- **Build**: Run `dotnet build` in the solution or project directory.
- **Test**: Run `dotnet test` in the `tests/document-to-podcast.net.tests/` directory.
- **Run**: Use `dotnet run -- --input-file ...` or `--config-file ...` (see README for CLI details).
- **Debugging**: Inspect generated WAV files in `tests/.../GeneratedAudio/` after integration tests.

## Patterns & Conventions

- **Dependency Injection**: All services are constructed via DI, with explicit constructor parameters for logging, config, and dependencies.
- **Factory Pattern**: Document parsing uses a factory to select parser by file extension.
- **Error Handling**: Always use braces for control flow. Use guard clauses for null checks and error conditions.
- **Audio Output**: All TTS services must return valid WAV files (check RIFF/WAVE header in tests).
- **Extensibility**: Add new document parsers or TTS/LLM services by implementing interfaces in `Services/Interfaces.cs` and registering in factories.

## Integration Points

- **LLM APIs**: OpenAI-compatible endpoints (Ollama, LM Studio, OpenAI API) for text generation.
- **MarkItDown**: Python-based document parsing (can be run locally or replaced with .NET parser).
- **NAudio**: Used for WAV file generation and manipulation.

## Examples

- To add a new document parser:
	- Implement `IDocumentParser`.
	- Register in `DocumentParserFactory`.
- To add a new TTS service:
	- Implement `ITextToSpeechService`.
	- Register in DI and update orchestration in `PodcastGeneratorService`.

## Code Style

- Always use braces `{}` for all control flow blocks, even single-line.
- Use guard clauses for error handling.
- Prefer explicit, readable error handling and logging.

## Documentation

- All docs go in `.github/docs/`.
- Status updates in `.github/status/`.

---
If any section is unclear or missing, please provide feedback for iterative improvement.
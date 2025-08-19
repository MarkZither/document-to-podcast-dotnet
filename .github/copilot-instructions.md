# Copilot Instructions for Document-to-Podcast .NET

## Project Overview

The document-to-podcast .NET project is a reimplementation of the Python document-to-podcast project using OpenAI-compatible APIs. The project uses a modular architecture with dependency injection and SOLID principles.

## Features

*   Flexible Document Parsing: Uses a factory pattern to support multiple document parsers (MarkItDown integration, basic text file parsing)
*   OpenAI-Compatible API Integration: Works with Ollama, LM Studio, OpenAI API
*   Audio Generation: Full text-to-speech pipeline (OpenAI-compatible TTS API integration, Mock TTS service for testing and development)

## Development

The project structure is divided into the following folders:

*   `src/document-to-podcast.net/`: The main .NET project directory
*   `Configuration/`: Configuration models and default settings
*   `Services/`: Core business logic (interfaces, factory pattern, text generation services)
*   `Parsers/`: Document parsing (MarkItDown integration, basic text file parsing)

## Documentation

All documentation-related tasks are to be written in the `.github/docs` folder. This includes:

*   README files
*   Documentation summaries
*   API documentation

## Status Updates

Any status update files should be kept separate in a subdirectory within the `.github/` directory.

## Notes

*   The project uses OpenAI-compatible APIs for text generation and text-to-speech conversion.
*   The project structure is designed to follow SOLID principles and dependency injection.

Please ensure that any documentation or updates are written according to these instructions.
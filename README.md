# Overview
Optical Character Recognition (OCR) Translator is an entirely offline local JP to EN Translation tool enabled by OCR using self hosted Vision capable LLM Models. The default model used is [Qwen3-VL-8B](https://huggingface.co/lmstudio-community/Qwen3-VL-8B-Instruct-GGUF), and uses LM Studio to manage LLM server. The current application is integrated to use LM Studio CLI commands. For more advanced CLI users can use llmster instead of LM Studio.

There are no current plans for the application to run using ollama as of yet.

OCR Translator supports selectable region to be translated and supports customizable auto translation.
![Sample](/Images/sample.JPG)

This tool is currently only available on Windows and was created with the use of AI tools.

## Requirements
- [LM Studio](https://lmstudio.ai/)
- [Vision capable LLM model](https://huggingface.co/lmstudio-community/Qwen3-VL-8B-Instruct-GGUF)
- [.Net 10 Runtime](https://dotnet.microsoft.com/en-us/download)

## Getting Started
1. Install [LM Studio](https://lmstudio.ai/)
2. Download a vision capable LLM model using LM Studio
    1. By default a .config file is generated with the model name "qwen3-vl-8b".
    2. Replace the model name in the config with a new model name if not using qwen3-vl-8b.
3. Open the solution, compile (Visual Studio or dotnet) and run **or** run "[OCR Translator.exe](https://github.com/BryanWongCK/OCR-Translator/releases)" directly.

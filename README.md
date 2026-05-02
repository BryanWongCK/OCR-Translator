# Overview
OCR Translator is an entirely offline local JP to EN Translation tool enabled by OCR using self hosted Vision capable LLM Models. The default model used  is [Qwen3-VL-8B](https://huggingface.co/lmstudio-community/Qwen3-VL-8B-Instruct-GGUF).

OCR Translator supports selectable region to be translated and supports customizable auto translation.
![Sample](/Images/sample.JPG)

This tool is currently only available on Windows and was created with the use of AI tools.

## Requirements
- [LM Studio](https://lmstudio.ai/)
- [Vision capable LLM model](https://huggingface.co/lmstudio-community/Qwen3-VL-8B-Instruct-GGUF)
- [.Net 10 Runtime](https://dotnet.microsoft.com/en-us/download)

## Getting Started
1. Install [LM Studio](https://lmstudio.ai/)
2. Download a vision capable LLM model
    1. By default a .config file is generated with the model name "qwen3-vl-8b".
    2. Replace the model name in the config with a new model name if not using qwen3-vl-8b.
3. Open the solution, compile and run **or** run "[OCR Translator.exe](https://github.com/BryanWongCK/OCR-Translator/releases/tag/1.0)" directly.
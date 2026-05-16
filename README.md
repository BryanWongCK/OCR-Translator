# Overview
OCR Translator is an entirely offline local JP to EN Translation tool enabled by OCR using self hosted Vision capable LLM Models. The default model used  is [Qwen3-VL-8B](https://huggingface.co/lmstudio-community/Qwen3-VL-8B-Instruct-GGUF).

OCR Translator supports selectable region to be translated and supports customizable auto translation.
![Sample](/Images/sample.JPG)

This tool is currently only available on Windows and was created with the use of AI tools.

## Getting Started
### Requirements
  - [LM Studio](https://lmstudio.ai/)
  - [Vision capable LLM model](https://huggingface.co/lmstudio-community/Qwen3-VL-8B-Instruct-GGUF)
  - [.Net 10 Runtime](https://dotnet.microsoft.com/en-us/download)
### Installation
1. Install [LM Studio](https://lmstudio.ai/)
2. Download a vision capable LLM model with LM Studio
    1. Make sure **CORS is enabled** in LM Studio's server settings
    2. By default a .config file is generated with the model name "qwen3-vl-8b".
    3. Replace the model name in the config with a new model name if not using qwen3-vl-8b.
3. Download and run the latest **[OCR Translator.exe](https://github.com/BryanWongCK/OCR-Translator/releases/latest)** directly.

## How to Use

### Loading a Name/Context Mapping (Optional)
Click **Load Mapping** to load a `json` file containing custom Japanese to English text mappings. This is useful for ensuring character names and other terms are translated consistently.

Example format:
```json
[
  { "og": "初音ミク", "en": "Hatsune Miku" },
  { "og": "鏡音リン", "en": "Kagamine Rin" },  
]
```

### Selecting a Capture Region
Click **Draw Region** and drag over the area of the screen you want to capture and translate.

### Modes
**Manual** — Click **Capture & Translate** whenever you want to translate the selected region.

**Auto** — Set a recapture interval (default: 5 seconds), then click **Capture & Translate** to start. The app will automatically retranslate at that interval. Use the **threshold slider** to control sensitivity to image changes — 0 means the image must be identical to skip, 50 is the least sensitive. Click **Stop** next to the **Capture & Translate** button to stop.

## Building from Source
### Requirements
- [.NET 10 SDK](https://dotnet.microsoft.com/en-us/download)

### Build
Either run the provided **build.bat** file, or run the following command manually:
```bash
dotnet publish "OCR Translator/OCR Translator/OCR Translator.csproj" -c Release -o ./dist
```
The output will be in the `dist/` folder.
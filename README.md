# Overview
OCR Translator is an entirely offline local JP to EN translation tool that uses OCR + self-hosted LLMs.

There are currently 2 versions:
- **Vision-based LLM OCR** using a vision-capable LLM (default: [Qwen3-VL-8B](https://huggingface.co/lmstudio-community/Qwen3-VL-8B-Instruct-GGUF))
- **RapidOCR with LLM** using RapidOCR to extract text before translated by an LLM (default: [Qwen2.5-7B](https://huggingface.co/lmstudio-community/Qwen2.5-7B-Instruct-GGUF))

The tool supports selectable screen region translation and customizable mapped translation.

![Sample](/Images/sample.JPG)

> Windows only. Built with AI-assisted development.

---

## Getting Started

### Requirements
- [LM Studio](https://lmstudio.ai/)
- [.NET 10 Runtime](https://dotnet.microsoft.com/en-us/download)

---

## Installation

### 1. Install LM Studio
[Download and install](https://lmstudio.ai/)

### 2. Configure LM Studio
- Download a supported model in LM Studio
- Ensure **CORS is enabled** in LM Studio server settings

### 3. Choose Your Mode

#### Vision Mode
- Requires a **vision-capable model**
- Default model:  
  - [`qwen3-vl-8b`](https://huggingface.co/lmstudio-community/Qwen3-VL-8B-Instruct-GGUF)

#### RapidOCR Mode
- Does **not require a vision model**
- Uses **RapidOCR for text extraction**
- Sends extracted text to an LLM for translation
- Can use lighter weight models due to not requiring vision capabilities
- Default model:
  - [`qwen2.5-7b`](https://huggingface.co/lmstudio-community/Qwen2.5-7B-Instruct-GGUF)


### 4. Run the Application
Download and run the latest **[release](https://github.com/BryanWongCK/OCR-Translator/releases/latest)** version of your choice directly.


---


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

### Cloning the Repository

This project uses Git submodules. Make sure to clone with submodules enabled:

```bash
git clone --recursive https://github.com/BryanWongCK/OCR-Translator.git
```

### Build
Either run the provided **build.bat** file, or run the following command manually:
#### Vision Model
```bash
dotnet publish "OCR Translator/OCR Translator.csproj" -c Release -o .\dist\Release
```
#### RapidOCR
```bash
dotnet publish "OCR Translator/OCR Translator.csproj" -c "Release Rapid" -o ".\dist\Release Rapid"
```
The output will be in the `dist\` folder.

## ⚠️ Known Issues
- Windows screen scale causes inaccurate region drawning

## Third-Party Components

- RapidOCRCSharp (submodule)  
  Repository: https://github.com/<repo>  
  Purpose: OCR text detection and recognition pipeline
  
No explicit license file is present in the upstream repository at time of integration. The component is used as-is with unknown licensing status pending author approval.

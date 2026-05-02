# OCR Translator — Build Instructions

## Prerequisites
Install ONE of these (both are free):

**Option A — .NET 8 SDK only (lighter, ~200MB)**
https://dotnet.microsoft.com/download/dotnet/8.0
→ Download the "SDK" installer for Windows x64

**Option B — Visual Studio Community 2022 (full IDE)**
https://visualstudio.microsoft.com/vs/community/
→ During install, select the ".NET desktop development" workload

---

## Build & Run

### With .NET SDK (command line):

1. Open a terminal (cmd or PowerShell) in the `ocr-translator` folder
2. Run the app directly:
   ```
   dotnet run
   ```
3. Or build a single standalone .exe (no runtime needed on other machines):
   ```
   dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
   ```
   → Your `.exe` will be in: `bin\Release\net8.0-windows\win-x64\publish\OCRTranslator.exe`

### With Visual Studio:

1. Open the `ocr-translator` folder → File → Open → Folder
2. Press F5 to run, or Ctrl+Shift+B to build
3. To publish: right-click project → Publish → Folder → self-contained, single file

---

## Usage

1. Start **LM Studio**, load `gemma-3-4b-it`, and start the Local Server
2. Make sure **CORS is enabled** in LM Studio's server settings
3. Run `OCRTranslator.exe`
4. Click **Draw Region** and drag a rectangle over the text area on screen
5. Choose **Manual** (click to capture) or **Auto** (captures every N seconds)
6. The Output window shows: Original text · Romanized · English translation

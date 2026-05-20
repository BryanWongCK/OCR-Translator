#if RapidOCR
using System;
using System.Drawing;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using RapidOCRLib;

public class OCR
{
    private OcrLite? ocr;

    public async Task Init(string detPath, string clsPath, string recPath, string dictPath)
    {
        ocr = new OcrLite(detPath, clsPath, recPath, dictPath);

        await ocr.InitModels();
    }

    public string DetectText(byte[] imageBytes)
    {
        if (ocr == null)
            throw new InvalidOperationException("OCR not initialized.");

        using var ms = new MemoryStream(imageBytes);
        using var img = Image.FromStream(ms);

        var result = ocr.Detect(img);

        return result?.StrRes?.Trim() ?? string.Empty;
    }
}
#endif
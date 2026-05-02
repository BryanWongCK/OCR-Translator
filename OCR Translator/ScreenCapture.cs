using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;

namespace OCRTranslator;

public static class ScreenCapture
{
    // We use GDI directly to avoid needing System.Drawing.Common on .NET 8 Windows
    [DllImport("gdi32.dll")] private static extern bool BitBlt(
        IntPtr hdc, int x, int y, int cx, int cy,
        IntPtr hdcSrc, int x1, int y1, uint rop);

    [DllImport("user32.dll")] private static extern IntPtr GetDC(IntPtr hWnd);
    [DllImport("user32.dll")] private static extern int   ReleaseDC(IntPtr hWnd, IntPtr hDC);
    [DllImport("gdi32.dll")]  private static extern IntPtr CreateCompatibleDC(IntPtr hdc);
    [DllImport("gdi32.dll")]  private static extern IntPtr CreateCompatibleBitmap(IntPtr hdc, int cx, int cy);
    [DllImport("gdi32.dll")]  private static extern IntPtr SelectObject(IntPtr hdc, IntPtr h);
    [DllImport("gdi32.dll")]  private static extern bool  DeleteDC(IntPtr hdc);
    [DllImport("gdi32.dll")]  private static extern bool  DeleteObject(IntPtr ho);

    private const uint SRCCOPY = 0x00CC0020;

    public static string CaptureBase64(CaptureRegion r)
    {
        if (r.W <= 0 || r.H <= 0)
            throw new ArgumentException("Region dimensions must be positive.");

        IntPtr screenDC  = GetDC(IntPtr.Zero);
        IntPtr memDC     = CreateCompatibleDC(screenDC);
        IntPtr hBitmap   = CreateCompatibleBitmap(screenDC, r.W, r.H);
        IntPtr oldBitmap = SelectObject(memDC, hBitmap);

        try
        {
            BitBlt(memDC, 0, 0, r.W, r.H, screenDC, r.X, r.Y, SRCCOPY);
            using var bmp = System.Drawing.Image.FromHbitmap(hBitmap);
            using var ms  = new MemoryStream();
            bmp.Save(ms, ImageFormat.Jpeg);
            return Convert.ToBase64String(ms.ToArray());
        }
        finally
        {
            SelectObject(memDC, oldBitmap);
            DeleteObject(hBitmap);
            DeleteDC(memDC);
            ReleaseDC(IntPtr.Zero, screenDC);
        }
    }
}

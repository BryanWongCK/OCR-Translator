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

    public static byte[] CaptureBytes(CaptureRegion r)
    {
        if (r.W <= 0 || r.H <= 0)
            throw new ArgumentException("Region dimensions must be positive.");

        int x = (int)((r.X - r.HotX) * r.DpiScale);
        int y = (int)((r.Y - r.HotY) * r.DpiScale);
        int w = (int)(r.W * r.DpiScale);
        int h = (int)(r.H * r.DpiScale);

        IntPtr screenDC = GetDC(IntPtr.Zero);
        IntPtr memDC = CreateCompatibleDC(screenDC);
        IntPtr hBitmap = CreateCompatibleBitmap(screenDC, w, h);
        IntPtr oldBitmap = SelectObject(memDC, hBitmap);

        try
        {
            BitBlt(memDC, 0, 0, w, h, screenDC, x, y, SRCCOPY);

            using var bmp = Image.FromHbitmap(hBitmap);
            using var ms = new MemoryStream();

            bmp.Save(ms, ImageFormat.Jpeg);
            return ms.ToArray();
        }
        finally
        {
            SelectObject(memDC, oldBitmap);
            DeleteObject(hBitmap);
            DeleteDC(memDC);
            ReleaseDC(IntPtr.Zero, screenDC);
        }
    }

    public static byte[] UpscaleImage(byte[] imageBytes, int minWidth = 800, int minHeight = 800)
    {
        using var ms = new MemoryStream(imageBytes);
        using var src = new Bitmap(ms);

        int width = src.Width;
        int height = src.Height;

        // already large enough → no change
        if (width >= minWidth && height >= minHeight)
            return imageBytes;

        // compute scale factor to meet minimum requirement
        double scaleX = (double)minWidth / width;
        double scaleY = (double)minHeight / height;

        double scale = Math.Max(scaleX, scaleY);

        int newWidth = (int)Math.Ceiling(width * scale);
        int newHeight = (int)Math.Ceiling(height * scale);

        using var upscaled = new Bitmap(newWidth, newHeight);

        using (var g = Graphics.FromImage(upscaled))
        {
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            g.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;

            g.DrawImage(src, 0, 0, newWidth, newHeight);
        }

        using var outStream = new MemoryStream();
        upscaled.Save(outStream, System.Drawing.Imaging.ImageFormat.Jpeg);

        return outStream.ToArray();
    }

    public static byte[] AverageHash(byte[] imageBytes, int size = 16)
    {
        using var ms = new MemoryStream(imageBytes);
        using var src = Image.FromStream(ms);

        using var small = new Bitmap(size, size, PixelFormat.Format32bppArgb);
        using var g = Graphics.FromImage(small);
        g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.Bilinear;
        g.DrawImage(src, 0, 0, size, size);

        int total = 0;
        var pixels = new int[size * size];
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                var c = small.GetPixel(x, y);
                int brightness = (c.R + c.G + c.B) / 3;
                pixels[y * size + x] = brightness;
                total += brightness;
            }
        int avg = total / pixels.Length;

        var hash = new byte[(pixels.Length + 7) / 8];
        for (int i = 0; i < pixels.Length; i++)
            if (pixels[i] >= avg)
                hash[i / 8] |= (byte)(1 << (i % 8));

        return hash;
    }

    public static int HammingDistance(byte[] a, byte[] b)
    {
        int dist = 0;
        for (int i = 0; i < Math.Min(a.Length, b.Length); i++)
            dist += System.Numerics.BitOperations.PopCount((uint)(a[i] ^ b[i]));
        return dist;
    }
}

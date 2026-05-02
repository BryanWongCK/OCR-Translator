using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;

namespace OCRTranslator;

public record CaptureRegion(int X, int Y, int W, int H, double DpiScale, int HotX, int HotY);

public partial class RegionSelector : Window
{
    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(out POINT pt);

    [DllImport("user32.dll")]
    private static extern IntPtr GetCursor();

    [DllImport("user32.dll")]
    private static extern bool GetIconInfo(IntPtr hIcon, out ICONINFO info);

    [DllImport("gdi32.dll")]
    private static extern bool DeleteObject(IntPtr ho);

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT { public int X, Y; }

    [StructLayout(LayoutKind.Sequential)]
    private struct ICONINFO
    {
        public bool fIcon;
        public int  xHotspot;
        public int  yHotspot;
        public IntPtr hbmMask;
        public IntPtr hbmColor;
    }

    private POINT _startPhysical;
    private bool  _drawing;

    public CaptureRegion? SelectedRegion { get; private set; }

    public RegionSelector()
    {
        InitializeComponent();
        WindowStartupLocation = WindowStartupLocation.Manual;

        // Span all screens
        var allScreens = System.Windows.Forms.Screen.AllScreens;
        int left = allScreens.Min(s => s.Bounds.Left);
        int top = allScreens.Min(s => s.Bounds.Top);
        int right = allScreens.Max(s => s.Bounds.Right);
        int bottom = allScreens.Max(s => s.Bounds.Bottom);
        double dpi = GetDpi();

        Left = left / dpi;
        Top = top / dpi;
        Width = (right - left) / dpi;
        Height = (bottom - top) / dpi;

        KeyDown += (_, e) => { if (e.Key == Key.Escape) Close(); };
        MouseLeftButtonDown += OnMouseDown;
        MouseMove += OnMouseMove;
        MouseLeftButtonUp += OnMouseUp;
    }

    private double GetDpi()
    {
        var src = System.Windows.Interop.HwndSource.FromVisual(this);
        return src?.CompositionTarget?.TransformToDevice.M11 ?? 1.0;
    }

    // Returns the cursor hotspot in physical pixels
    private static POINT GetCursorHotspot()
    {
        var hCursor = GetCursor();
        if (hCursor != IntPtr.Zero && GetIconInfo(hCursor, out var info))
        {
            if (info.hbmMask  != IntPtr.Zero) DeleteObject(info.hbmMask);
            if (info.hbmColor != IntPtr.Zero) DeleteObject(info.hbmColor);
            return new POINT { X = info.xHotspot, Y = info.yHotspot };
        }
        return new POINT { X = 0, Y = 0 };
    }

    // Get cursor position adjusted to hotspot center
    private static POINT GetCursorCenter()
    {
        GetCursorPos(out var pt);
        var hot = GetCursorHotspot();
        // GetCursorPos returns top-left of cursor image; add hotspot to get click point
        return new POINT { X = pt.X + hot.X / 2, Y = pt.Y + hot.Y / 2 };
    }

    private void OnMouseDown(object sender, MouseButtonEventArgs e)
    {
        _startPhysical = GetCursorCenter();
        _drawing = true;
        SelectRect.Visibility = Visibility.Visible;
        SizeBorder.Visibility = Visibility.Visible;
        HintBorder.Visibility = Visibility.Collapsed;
        RootCanvas.CaptureMouse();
        UpdateRect();
    }

    private void OnMouseMove(object sender, MouseEventArgs e)
    {
        if (!_drawing) return;
        UpdateRect();
    }

    private void OnMouseUp(object sender, MouseButtonEventArgs e)
    {
        if (!_drawing) return;
        _drawing = false;
        RootCanvas.ReleaseMouseCapture();

        var cur = GetCursorCenter();

        int x = Math.Min(cur.X, _startPhysical.X);
        int y = Math.Min(cur.Y, _startPhysical.Y);
        int w = Math.Abs(cur.X - _startPhysical.X);
        int h = Math.Abs(cur.Y - _startPhysical.Y);

        if (w > 10 && h > 10)
        {
            var hot = GetCursorHotspot();
            SelectedRegion = new CaptureRegion(x, y, w, h, GetDpi(), hot.X / 2, hot.Y / 2);
        }

        Close();
    }

    private void UpdateRect()
    {
        var cur = GetCursorCenter();
        var hot = GetCursorHotspot();
        double dpi = GetDpi();

        double cx = (cur.X - (Left + hot.X / 2) * dpi) / dpi;
        double cy = (cur.Y - (Top + hot.Y / 2) * dpi) / dpi;
        double sx = (_startPhysical.X - (Left + hot.X / 2) * dpi) / dpi;
        double sy = (_startPhysical.Y - (Top + hot.Y / 2) * dpi)  / dpi;

        double x = Math.Min(cx, sx);
        double y = Math.Min(cy, sy);
        double w = Math.Abs(cx - sx);
        double h = Math.Abs(cy - sy);

        Canvas.SetLeft(SelectRect, x);
        Canvas.SetTop(SelectRect, y);
        SelectRect.Width = w;
        SelectRect.Height = h;

        double lx = cx + 14;
        double ly = cy + 14;
        if (lx + 100 > ActualWidth) lx = cx - 110;
        if (ly + 30 > ActualHeight) ly = cy - 34;
        Canvas.SetLeft(SizeBorder, lx);
        Canvas.SetTop(SizeBorder, ly);
        TxtSize.Text = $"{(int)(w * dpi)} × {(int)(h * dpi)}";
    }
}

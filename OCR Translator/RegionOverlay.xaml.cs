using System.Windows;

namespace OCRTranslator;

public partial class RegionOverlay : Window
{
    public RegionOverlay()
    {
        InitializeComponent();
    }

    public void SetRegion(CaptureRegion r)
    {
        // r.X/Y/W/H are physical pixels; divide by DpiScale for WPF logical units
        Left = (r.X - r.HotX) / r.DpiScale;
        Top = (r.Y - r.HotY) / r.DpiScale;
        Width = r.W / r.DpiScale;
        Height = r.H / r.DpiScale;
    }
}

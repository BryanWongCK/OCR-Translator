using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using Clipboard = System.Windows.Clipboard;

namespace OCRTranslator;

public partial class OutputWindow : Window
{
    private bool _forceClose;

    public string OriginalText { set { TxtOriginal.Text = value; } }

    public OutputWindow()
    {
        InitializeComponent();
        Closing += (_, e) => { if (!_forceClose) e.Cancel = true; Hide(); };

#if ShowSteps
        Visibility visibility = Visibility.Visible;
        Height = 480;
#else
        Visibility visibility = Visibility.Collapsed;
        Height = 160;
        RowDefinitionCollection rows = ((Grid)GridOriginal.Parent).RowDefinitions;
        rows[1].Height = new GridLength(0);
        rows[2].Height = new GridLength(0);
#endif
        GridOriginal.Visibility = visibility;
        GridRomaji.Visibility = visibility;
    }

    public void ForceClose() { _forceClose = true; Close(); }

#if ShowSteps
    public void ShowResult(string content)
    {
        Dispatcher.Invoke(() =>
        {
            JsonDocument json = ParseJSON(content);

#if !RapidOCR
            TxtOriginal.Text    = json.RootElement.GetProperty("og").GetString();
#endif
            TxtRomaji.Text      = json.RootElement.GetProperty("rm").GetString();
            TxtTranslation.Text = json.RootElement.GetProperty("en").GetString();
            TxtTimestamp.Text   = DateTime.Now.ToString("HH:mm:ss");

            if (!IsVisible) Show();
        });
    }

    private static JsonDocument ParseJSON(string content)
    {
        int start = content.IndexOf('{');
        int end = content.LastIndexOf('}');

        if (start == -1 || end == -1 || end < start)
        {
#if RapidOCR
            return JsonDocument.Parse(@"{""og"":""—"",""rm"":""—"",""en"":""—""}");
#else
            return JsonDocument.Parse(@"{""rm"":""—"",""en"":""—""}");
#endif
        }

        return JsonDocument.Parse(content.Substring(start, end - start + 1));
    }
    
#else    
    public void ShowResult(string content)
    {
        Dispatcher.Invoke(() =>
        {
            TxtTranslation.Text = content;
            TxtTimestamp.Text = DateTime.Now.ToString("HH:mm:ss");
            if (!IsVisible) Show();
        });
    }
#endif

    private void BtnCopyOriginal_Click(object sender, RoutedEventArgs e)    => Clipboard.SetText(TxtOriginal.Text);
    private void BtnCopyRomaji_Click(object sender, RoutedEventArgs e)      => Clipboard.SetText(TxtRomaji.Text);
    private void BtnCopyTranslation_Click(object sender, RoutedEventArgs e) => Clipboard.SetText(TxtTranslation.Text);
}

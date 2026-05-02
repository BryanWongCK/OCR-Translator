using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Media;
using Clipboard = System.Windows.Clipboard;

namespace OCRTranslator;

public partial class OutputWindow : Window
{
    private bool _forceClose;

    public OutputWindow()
    {
        InitializeComponent();
        Closing += (_, e) => { if (!_forceClose) e.Cancel = true; Hide(); };
    }

    public void ForceClose() { _forceClose = true; Close(); }

    public void ShowResult(string content)
    {
        Dispatcher.Invoke(() =>
        {
            var original    = Extract(content, "ORIGINAL",   @"ROMANIZED:|TRANSLATION:");
            var romaji      = Extract(content, "ROMANIZED",  @"TRANSLATION:");
            var translation = Extract(content, "TRANSLATION", null);

            TxtOriginal.Text    = string.IsNullOrWhiteSpace(original)    ? "(nothing extracted)" : original;
            TxtRomaji.Text      = string.IsNullOrWhiteSpace(romaji)      ? "(no romanization)"   : romaji;
            TxtTranslation.Text = string.IsNullOrWhiteSpace(translation) ? "(no translation)"    : translation;
            TxtTimestamp.Text   = DateTime.Now.ToString("HH:mm:ss");

            if (!IsVisible) Show();
        });
    }

    private static string Extract(string content, string section, string? stopPattern)
    {
        // Match the section header then capture until the next section or end
        string stop = stopPattern != null ? $"(?={stopPattern})" : "$";
        var m = Regex.Match(content, $@"(?is){section}:\s*(.*?)\s*{stop}");
        return m.Success ? m.Groups[1].Value.Trim() : "";
    }

    private void BtnCopyOriginal_Click(object sender, RoutedEventArgs e)    => Clipboard.SetText(TxtOriginal.Text);
    private void BtnCopyRomaji_Click(object sender, RoutedEventArgs e)      => Clipboard.SetText(TxtRomaji.Text);
    private void BtnCopyTranslation_Click(object sender, RoutedEventArgs e) => Clipboard.SetText(TxtTranslation.Text);
}

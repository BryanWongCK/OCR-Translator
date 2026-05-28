using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
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
            TxtTranslation.Text = content;
            TxtTimestamp.Text = DateTime.Now.ToString("HH:mm:ss");
            if (!IsVisible) Show();
        });
    }

    private void BtnCopyTranslation_Click(object sender, RoutedEventArgs e) => Clipboard.SetText(TxtTranslation.Text);
}

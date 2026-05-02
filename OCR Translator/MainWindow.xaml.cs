using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using Brush = System.Windows.Media.Brush;
using Brushes = System.Windows.Media.Brushes;

namespace OCRTranslator;

public partial class MainWindow : Window
{
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(90) };

    private CaptureRegion? _region;
    private DispatcherTimer? _autoTimer;
    private readonly OutputWindow _output;
    private readonly RegionOverlay _overlay = new();
    private byte[]? _lastHash;
    private bool _hasLastHash;
    private string modelName;

    public MainWindow()
    {
        if (File.Exists(".config"))
            modelName = File.ReadAllText(".config");
        else
        {
            // Assign default model name and generate config file
            modelName = "qwen3-vl-8b";
            File.WriteAllText(".config", modelName);
        }
        StartModel(modelName);

        InitializeComponent();
        _output = new OutputWindow();

        Loaded += (_, _) =>
        {
            _output.Left = Left + Width + 10;
            _output.Top  = Top;
            _output.Show();
            SliderThreshold.ValueChanged += (_, e) => TxtThresholdValue.Text = ((int)SliderThreshold.Value).ToString();
        };

        Closed += (_, _) =>
        {
            _autoTimer?.Stop();
            _output.ForceClose();
            _overlay.Close();
            StopModel(modelName);
        };
    }

    private static void StartModel(string modelKey)
    {
        // Start server
        Process.Start(new ProcessStartInfo
        {
            FileName = "lms",
            Arguments = "server start --cors",
            UseShellExecute = false,
            CreateNoWindow = true
        });

        // Load model
        Process.Start(new ProcessStartInfo
        {
            FileName = "lms",
            Arguments = $"load {modelKey} --gpu max",
            UseShellExecute = false,
            CreateNoWindow = true
        });
    }

    private static void StopModel(string modelName)
    {
        ProcessStartInfo psi = new ProcessStartInfo
        {
            FileName = "lms",
            Arguments = "ps",
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        Process? p = Process.Start(psi);
        string output = p.StandardOutput.ReadToEnd();
        p.WaitForExit();

        // Find the full identifier line that contains the model and unload it
        var lines = output.Split('\n');
        foreach (var line in lines)
        {
            if (line.Contains(modelName))
            {
                var fullId = line.Trim().Split(' ')[0];

                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "lms",
                    Arguments = $"unload {fullId}",
                    UseShellExecute = false,
                    CreateNoWindow = true
                });

                break;
            }
        }

        // Stop server
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = "lms",
            Arguments = "server stop",
            UseShellExecute = false,
            CreateNoWindow = true
        });

        // Wait for model to be unloaded and server to be stopped
        Thread.Sleep(2000);

        // Kill LM Studio process
        foreach (var winp in Process.GetProcesses())
        {
            try
            {
                // Explicit name kill to prevent accidental process casualty
                if (winp.ProcessName == "LM Studio")
                    winp.Kill(true);
            }
            catch
            {
                // ignore processes that already exited or are protected
            }
        }
    }

    // ── Region selector ──────────────────────────────────────────────────────
    private void BtnSelectRegion_Click(object sender, RoutedEventArgs e)
    {
        _overlay.Hide();
        WindowState = WindowState.Minimized;

        var selector = new RegionSelector();
        selector.ShowDialog();

        WindowState = WindowState.Normal;
        Activate();

        if (selector.SelectedRegion is { } r)
        {
            _region = r;
            TxtRegion.Text       = $"X:{r.X}  Y:{r.Y}  {r.W} × {r.H} px";
            TxtRegion.Foreground = Brushes.LightGreen;
            BtnCapture.IsEnabled = true;
            _hasLastHash = false;
            SetStatus("Region set — ready.", "#00e5a0");

            _overlay.SetRegion(r);
            _overlay.Show();
        }
    }

    // ── Mode radio buttons ───────────────────────────────────────────────────
    private void RadioManual_Checked(object sender, RoutedEventArgs e)
    {
        if (TxtInterval is null) return;
        TxtInterval.IsEnabled = false;
        PanelThreshold.Visibility = Visibility.Collapsed;
        Height = 396; // Hidden threshold slider
    }

    private void RadioAuto_Checked(object sender, RoutedEventArgs e)
    {
        if (TxtInterval is null) return;
        TxtInterval.IsEnabled = true;
        PanelThreshold.Visibility = Visibility.Visible;
        Height = 446;  // Visible threshold slider
    }

    // ── Capture button ───────────────────────────────────────────────────────
    private async void BtnCapture_Click(object sender, RoutedEventArgs e)
    {
        if (RadioAuto.IsChecked == true)
        {
            if (!int.TryParse(TxtInterval.Text, out int secs) || secs < 1) secs = 5;

            BtnCapture.IsEnabled      = false;
            BtnSelectRegion.IsEnabled = false;
            BtnStop.Visibility        = Visibility.Visible;
            SetStatus($"Auto mode — capturing every {secs}s…", "#888898");

            await RunCaptureAsync(); // immediate first capture

            _autoTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(secs) };
            _autoTimer.Tick += async (_, _) => await RunCaptureAsync();
            _autoTimer.Start();
        }
        else
        {
            await RunCaptureAsync();
        }
    }

    // ── Stop button ──────────────────────────────────────────────────────────
    private void BtnStop_Click(object sender, RoutedEventArgs e)
    {
        _autoTimer?.Stop();
        _autoTimer = null;
        BtnCapture.IsEnabled      = true;
        BtnSelectRegion.IsEnabled = true;
        BtnStop.Visibility        = Visibility.Collapsed;
        _hasLastHash = false;
        SetStatus("Stopped.", "#555566");
    }

    // ── Core capture + translate ─────────────────────────────────────────────
    private async Task RunCaptureAsync()
    {
        if (_region is null) return;

        SetStatus("Capturing screen…", "#888898");

        string base64;
        try   { base64 = ScreenCapture.CaptureBase64(_region); }
        catch (Exception ex) { SetStatus($"Capture error: {ex.Message}", "#ff5566"); return; }

        // Skip if image unchanged
        var hash = ScreenCapture.AverageHash(base64);
        if (RadioAuto.IsChecked == true && _hasLastHash && ScreenCapture.HammingDistance(hash, _lastHash) < (int)SliderThreshold.Value)
        {
            SetStatus($"No change — {DateTime.Now:HH:mm:ss}", "#333344");
            return;
        }
        _lastHash = hash;
        _hasLastHash = true;

        SetStatus("Sending to Model…", "#888898");

        try
        {
            string content = await CallLmStudioAsync(base64);
            _output.ShowResult(content);
            SetStatus($"Done — {DateTime.Now:HH:mm:ss}", "#00e5a0");
            SetDot("#00e5a0");
        }
        catch (Exception ex)
        {
            SetStatus($"Error: {ex.Message}", "#ff5566");
            SetDot("#ff5566");
        }
    }

    // ── LM Studio API call ───────────────────────────────────────────────────
    private async Task<string> CallLmStudioAsync(string base64)
    {
        string url = TxtServerUrl.Text.TrimEnd('/') + "/v1/chat/completions";

        const string prompt =
            "Extract the text from the image above as if you were reading it naturally. " +
            "Return only valid words and complete sentences. " +
            "If no text is detected, return — for all fields. " +
            "The ROMANIZED field must always use the latin alphabet. " +
            "The TRANSLATION field must always be in English, even if the original text is already in English. " +
            "Respond in exactly this format:\n" +
            "ORIGINAL:\n[the extracted text as-is]\n\n" +
            "ROMANIZED:\n[latin alphabet romanization of the original, or — if already latin]\n\n" +
            "TRANSLATION:\n[English translation, or copy the original if it is already English]";

        var body = new
        {
            model       = "",
            stream      = false,
            temperature = 0.1,
            max_tokens  = 1024,
            messages    = new object[]
            {
                new { role = "system", content = "You are a Japanese OCR and translation assistant. Always respond in the exact format given. TRANSLATION must always be in English. ROMANIZED must always use the latin alphabet. Never translate into Japanese. /no_think" },
                new
                {
                    role    = "user",
                    content = new object[]
                    {
                        new { type = "image_url", image_url = new { url = $"data:image/jpeg;base64,{base64}" } },
                        new { type = "text", text = prompt }
                    }
                }
            }
        };

        using var resp = await Http.PostAsJsonAsync(url, body);
        resp.EnsureSuccessStatusCode();

        var json = await resp.Content.ReadFromJsonAsync<JsonNode>();
        return json?["choices"]?[0]?["message"]?["content"]?.GetValue<string>()
               ?? throw new Exception("Unexpected API response format.");
    }

    // ── UI helpers ───────────────────────────────────────────────────────────
    private void SetStatus(string msg, string hex = "#555566")
    {
        Dispatcher.Invoke(() =>
        {
            TxtStatus.Text       = msg;
            TxtStatus.Foreground = (Brush)new BrushConverter().ConvertFrom(hex)!;
        });
    }

    private void SetDot(string hex) =>
        Dispatcher.Invoke(() => StatusDot.Fill = (Brush)new BrushConverter().ConvertFrom(hex)!);
}

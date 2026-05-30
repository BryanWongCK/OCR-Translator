using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using Brush = System.Windows.Media.Brush;
using Brushes = System.Windows.Media.Brushes;

namespace OCRTranslator;

public partial class MainWindow : Window
{
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(90) };

#if RapidOCR
    private OCR? ocr;
#endif

    private CaptureRegion? region;
    private DispatcherTimer? autoTimer;
    private readonly OutputWindow output;
    private readonly RegionOverlay overlay = new();
    private byte[]? lastHash;
    private bool hasLastHash;
    private string modelName;
    private string context = "";
    private string lastStatus = "";

    public MainWindow()
    {
        if (File.Exists(".config"))
            modelName = File.ReadAllText(".config");
        else
        {
            // Assign default model name and generate config file
#if RapidOCR
            modelName = "qwen2.5-7b-instruct";
#else
            modelName = "qwen3-vl-8b";
#endif
            File.WriteAllText(".config", modelName);
        }

        StartModel(modelName);

        InitializeComponent();
        output = new OutputWindow();

        Loaded += async (_, _) =>
        {
            output.Left = Left + Width + 10;
            output.Top  = Top;
            output.Show();
            SliderThreshold.ValueChanged += (_, e) => TxtThresholdValue.Text = ((int)SliderThreshold.Value).ToString();

#if RapidOCR
            ocr = new OCR();

            await ocr.Init(
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "model", "det.onnx"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "model", "cls.onnx"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "model", "rec.onnx"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "model", "dict.txt")
            );
#endif
        };

        Closed += (_, _) =>
        {
            autoTimer?.Stop();
            output.ForceClose();
            overlay.Close();
            StopModel(modelName);
        };
    }

    private void BtnReloadSettings_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Select Settings File",
            Filter = "All Files (*.*)|*.*",
            InitialDirectory = AppDomain.CurrentDomain.BaseDirectory
        };

        if (dialog.ShowDialog() == true)
        {
            try { LoadSettingFile(dialog.FileName); }
            catch { /* ignore malformed mapping file */ }
        }
    }

    private void LoadSettingFile(string path)
    {
        context = "";
        var arr = JsonNode.Parse(File.ReadAllText(path))?.AsArray();
        if (arr != null)
        {
            var lines = arr
                .Where(n => n != null)
                .Select(n => $"  {n!["og"]} → {n!["en"]}");
            context = "NAME MAPPINGS (you MUST use these exact spellings when these appear):\n"
                    + string.Join("\n", lines)
                    + "\n\n";
        }
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
        if (p == null) return;
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
        overlay.Hide();
        WindowState = WindowState.Minimized;

        var selector = new RegionSelector();
        selector.ShowDialog();
        
        WindowState = WindowState.Normal;
        Activate();

        if (selector.SelectedRegion is { } r)
        {
            region = r;
            TxtRegion.Text       = $"X:{r.X}  Y:{r.Y}  {r.W} × {r.H} px";
            TxtRegion.Foreground = Brushes.LightGreen;
            BtnCapture.IsEnabled = true;
            hasLastHash = false;
            SetStatus("Region set — ready.", "#00e5a0");

            overlay.SetRegion(r);
            overlay.Show();
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

            autoTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(secs) };
            autoTimer.Tick += async (_, _) => await RunCaptureAsync();
            autoTimer.Start();
        }
        else
        {
            await RunCaptureAsync();
        }
    }

    // ── Stop button ──────────────────────────────────────────────────────────
    private void BtnStop_Click(object sender, RoutedEventArgs e)
    {
        autoTimer?.Stop();
        autoTimer = null;
        BtnCapture.IsEnabled      = true;
        BtnSelectRegion.IsEnabled = true;
        BtnStop.Visibility        = Visibility.Collapsed;
        hasLastHash = false;
        Http.CancelPendingRequests();
        SetStatus("Stopped.", "#555566");
    }

    // ── Core capture + translate ─────────────────────────────────────────────
    private async Task RunCaptureAsync()
    {
        if (region is null) return;

        SetStatus("Capturing screen…", "#888898");

        byte[] imageBytes;
        try 
        {
            overlay.Hide();
            imageBytes = ScreenCapture.CaptureBytes(region);
            overlay.Show();
            imageBytes = ScreenCapture.UpscaleImage(imageBytes, 1200, 1200);
        }
        catch (Exception ex) { SetStatus($"Capture error: {ex.Message}", "#ff5566"); return; }

        var hash = ScreenCapture.AverageHash(imageBytes);
        if (RadioAuto.IsChecked == true && hasLastHash && ScreenCapture.HammingDistance(hash, lastHash ?? []) < (int)SliderThreshold.Value)
        {
            SetStatus($"{lastStatus}", "#00e5a0");
            return;
        }
        lastHash = hash;
        hasLastHash = true;

        SetStatus("Sending to Model…", "#888898");
        DateTime now = DateTime.Now;

        try
        {
#if RapidOCR
            if (ocr == null)
                throw new Exception("RapidOCR Engine unable to start up");
            string content = await CallLmStudioAsync(ocr.DetectText(imageBytes));
#else
            string content = await CallLmStudioAsync(Convert.ToBase64String(imageBytes));
#endif
            TimeSpan span = DateTime.Now - now;
            output.ShowResult(content);
            lastStatus = $"Done — {DateTime.Now:HH:mm:ss} ({(int)span.TotalMilliseconds}ms)";
            SetStatus(lastStatus, "#00e5a0");
            SetDot("#00e5a0");
        }
        catch (Exception ex)
        {
            SetStatus($"Error: {ex.Message}", "#ff5566");
            SetDot("#ff5566");
        }
    }

    // ── LM Studio API call ───────────────────────────────────────────────────
#if RapidOCR
    private async Task<string> CallLmStudioAsync(string textToTL)
    {
        if (string.IsNullOrWhiteSpace(textToTL))
            return "—";

        string prompt = 
        $@"
        {context}
        Translate the following text into english as if you were reading it naturally:
        ""{textToTL}""
        Return only valid words and complete sentences.
        You MUST respond in english. If there is no text you must respond with '—'
        Respond with only the translated text.";
#else
    private async Task<string> CallLmStudioAsync(string base64)
    {
        string prompt =
        $@"
        {context}
        Extract the text from the image above as if you were reading it naturally.
        Then translate it to english.
        Return only valid words and complete sentences.
        You MUST respond in english. If there is no text you must respond with '—'
        Respond with only the translated text.";
#endif

        var body = new
        {
            model       = "",
            stream      = false,
            temperature = 0.3,
            max_tokens  = 1024,
            messages    = new object[]
            {
#if RapidOCR
                new 
                { 
                    role = "system",
                    content = @"You are a Japanese to english translation assistant.
                            Never translate into anything but english.
                            Japanese honorifics (san, kun, chan, sama, senpai, sensei, dono, etc.) must always be kept as romaji — never convert them to Mr./Mrs./Miss or any English title.
                            /no_think"  
                },
                new
                {
                    role    = "user",
                    content = new object[]
                    {
                        new { type = "text", text = prompt }
                    }
                }
#else
                new
                {
                    role = "system",
                    content = @"You are a Japanese OCR translation assistant.
                            Never translate into anything but english.
                            Japanese honorifics (san, kun, chan, sama, senpai, sensei, dono, etc.) must always be kept as romaji — never convert them to Mr./Mrs./Miss or any English title.
                            /no_think"
                },
                new
                {
                    role    = "user",
                    content = new object[]
                    {
                        new { type = "image_url", image_url = new { url = $"data:image/jpeg;base64,{base64}" } },
                        new { type = "text", text = prompt }
                    }
                }
#endif
            }
        };

        string url = TxtServerUrl.Text.TrimEnd('/') + "/v1/chat/completions";
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

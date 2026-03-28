using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using TranscribeWin.Models;
using TranscribeWin.Services;

namespace TranscribeWin;

public partial class MainWindow : Window
{
    private readonly AudioRecorder _recorder = new();
    private TranscribeClient _transcribeClient = null!;
    private readonly HotkeyManager _hotkeyManager = new();
    private AppSettings _settings;
    private readonly DispatcherTimer _healthTimer = new();
    private bool _isToggleRecording;
    private Storyboard? _pulseStoryboard;
    private Hardcodet.Wpf.TaskbarNotification.TaskbarIcon? _trayIcon;
    private CancellationTokenSource? _statusResetCts;

    public MainWindow()
    {
        _settings = AppSettings.Load();
        I18n.SetLanguage("zh-TW");

        InitializeComponent();
        InitializeTray();
        LoadSettings();
        SetupEvents();
        StartHealthCheck();
        ApplyI18n();
    }

    #region Initialization

    private void InitializeTray()
    {
        _trayIcon = new Hardcodet.Wpf.TaskbarNotification.TaskbarIcon
        {
            ToolTipText = I18n.Get("AppTitle"),
            Visibility = Visibility.Collapsed
        };
        _trayIcon.TrayMouseDoubleClick += (s, e) => ShowFromTray();

        var menu = new ContextMenu { Background = new SolidColorBrush(Color.FromRgb(33, 38, 45)) };
        var showItem = new MenuItem
        {
            Header = I18n.Get("TrayShow"),
            Foreground = new SolidColorBrush(Color.FromRgb(230, 237, 243))
        };
        showItem.Click += (s, e) => ShowFromTray();

        var exitItem = new MenuItem
        {
            Header = I18n.Get("TrayExit"),
            Foreground = new SolidColorBrush(Color.FromRgb(248, 81, 73))
        };
        exitItem.Click += (s, e) => { _trayIcon.Dispose(); Application.Current.Shutdown(); };

        menu.Items.Add(showItem);
        menu.Items.Add(exitItem);
        _trayIcon.ContextMenu = menu;
    }

    private void ShowFromTray()
    {
        Show();
        WindowState = WindowState.Normal;
        Activate();
        _trayIcon!.Visibility = Visibility.Collapsed;
    }

    private void LoadSettings()
    {
        _transcribeClient = new TranscribeClient(_settings.ServerUrl);
        TxtServerUrl.Text = _settings.ServerUrl;

        // Language combo
        for (int i = 0; i < CmbLanguage.Items.Count; i++)
        {
            if (CmbLanguage.Items[i] is ComboBoxItem item && item.Tag?.ToString() == _settings.Language)
            {
                CmbLanguage.SelectedIndex = i;
                break;
            }
        }
        if (CmbLanguage.SelectedIndex < 0) CmbLanguage.SelectedIndex = 0;

        // Microphone combo
        var mics = AudioRecorder.GetMicrophones();
        CmbMicrophone.Items.Clear();
        for (int i = 0; i < mics.Count; i++)
            CmbMicrophone.Items.Add(new ComboBoxItem { Content = mics[i], Tag = i });
        if (_settings.MicrophoneDeviceIndex < mics.Count)
            CmbMicrophone.SelectedIndex = _settings.MicrophoneDeviceIndex;
        else if (mics.Count > 0)
            CmbMicrophone.SelectedIndex = 0;

        // UI Language combo
        for (int i = 0; i < CmbUILanguage.Items.Count; i++)
        {
            if (CmbUILanguage.Items[i] is ComboBoxItem item &&
                item.Tag?.ToString() == CultureInfo.CurrentUICulture.Name)
            {
                CmbUILanguage.SelectedIndex = i;
                break;
            }
        }
        if (CmbUILanguage.SelectedIndex < 0) CmbUILanguage.SelectedIndex = 0;

        // Record mode
        CmbRecordMode.SelectedIndex = _settings.RecordMode == "Toggle" ? 1 : 0;

        // Checkboxes
        ChkAutoCopy.IsChecked = _settings.AutoCopyToClipboard;
        ChkAutoEnter.IsChecked = _settings.AutoEnterOnPunctuation;
        ChkPunctuation.IsChecked = _settings.Punctuation;
    }

    private void SetupEvents()
    {
        _recorder.RecordingStopped += OnRecordingStopped;
        _recorder.VolumeChanged += OnVolumeChanged;

        // Toggle mode: fire on each press
        _hotkeyManager.HotkeyPressed += OnHotkeyPressed;
        // Push-to-talk mode: fire on key-down / key-up
        _hotkeyManager.HotkeyDown += OnHotkeyDown;
        _hotkeyManager.HotkeyUp += OnHotkeyUp;
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        var source = HwndSource.FromHwnd(new WindowInteropHelper(this).Handle);
        source?.AddHook(_hotkeyManager.HandleMessage);
        RegisterHotkey();
    }

    private void RegisterHotkey()
    {
        var handle = new WindowInteropHelper(this).Handle;
        bool ok = _hotkeyManager.Register(handle, _settings.HotkeyModifiers, _settings.HotkeyKey);
        var hotkeyStr = $"{_settings.HotkeyModifiers}+{_settings.HotkeyKey}";
        HotkeyText.Text = ok ? hotkeyStr : I18n.Get("HotkeyFailed");
    }

    private void StartHealthCheck()
    {
        _healthTimer.Interval = TimeSpan.FromSeconds(5);
        _healthTimer.Tick += async (s, e) => await CheckHealth();
        _healthTimer.Start();
        _ = CheckHealth();
    }

    private async Task CheckHealth()
    {
        bool healthy = await _transcribeClient.CheckHealthAsync();
        Dispatcher.Invoke(() =>
        {
            ServerDot.Fill = healthy
                ? (SolidColorBrush)FindResource("AccentGreenBrush")
                : (SolidColorBrush)FindResource("AccentRedBrush");
            ServerStatusText.Text = healthy ? I18n.Get("ServerOnline") : I18n.Get("ServerOffline");
        });
    }

    #endregion

    #region i18n

    private void ApplyI18n()
    {
        Title = I18n.Get("AppTitle");
        TitleText.Text = $"🎙️ {I18n.Get("AppTitle")}";
        StatusText.Text = I18n.Get("StatusIdle");
        TranscriptBox.Text = I18n.Get("TranscriptPlaceholder");
        BtnToggleSettings.ToolTip = I18n.Get("SettingsTitle");

        // Settings labels
        LblServerUrl.Text = I18n.Get("SettingsServerUrl");
        LblLanguage.Text = I18n.Get("SettingsLanguage");
        LblMic.Text = I18n.Get("SettingsMicrophone");
        LblUILanguage.Text = I18n.Get("SettingsUILanguage");
        LblRecordMode.Text = I18n.Get("SettingsRecordMode");
        CmbItemPTT.Content = I18n.Get("SettingsPushToTalk");
        CmbItemToggle.Content = I18n.Get("SettingsToggle");
        ChkAutoCopy.Content = I18n.Get("SettingsAutoCopy");
        ChkAutoEnter.Content = I18n.Get("SettingsAutoEnter");
        ChkPunctuation.Content = I18n.Get("SettingsPunctuation");
        BtnSave.Content = $"💾 {I18n.Get("SettingsSave")}";

        // Tray update
        if (_trayIcon != null)
            _trayIcon.ToolTipText = I18n.Get("AppTitle");
    }

    #endregion

    #region Recording

    // ---- Hotkey: toggle mode ----
    private void OnHotkeyPressed()
    {
        if (_settings.RecordMode != "Toggle") return;

        Dispatcher.Invoke(() =>
        {
            if (_isToggleRecording)
            {
                StopRecording();
            }
            else
            {
                TextInjector.SaveFocusedWindow();
                StartRecording();
            }
            _isToggleRecording = !_isToggleRecording;
        });
    }

    // ---- Hotkey: push-to-talk mode (key down → start) ----
    private void OnHotkeyDown()
    {
        if (_settings.RecordMode != "PushToTalk") return;

        Dispatcher.Invoke(() =>
        {
            if (!_recorder.IsRecording)
            {
                TextInjector.SaveFocusedWindow();
                StartRecording();
            }
        });
    }

    // ---- Hotkey: push-to-talk mode (key up → stop) ----
    private void OnHotkeyUp()
    {
        if (_settings.RecordMode != "PushToTalk") return;

        Dispatcher.Invoke(() =>
        {
            if (_recorder.IsRecording)
                StopRecording();
        });
    }

    // ---- UI button: mouse down/up for push-to-talk ----
    private void BtnRecord_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (_settings.RecordMode == "PushToTalk")
        {
            TextInjector.SaveFocusedWindow();
            StartRecording();
            e.Handled = true;
        }
    }

    private void BtnRecord_MouseUp(object sender, MouseButtonEventArgs e)
    {
        if (_settings.RecordMode == "PushToTalk" && _recorder.IsRecording)
        {
            StopRecording();
            e.Handled = true;
        }
    }

    // ---- UI button: click for toggle ----
    private void BtnRecord_Click(object sender, RoutedEventArgs e)
    {
        if (_settings.RecordMode == "Toggle")
        {
            if (_recorder.IsRecording)
            {
                StopRecording();
            }
            else
            {
                TextInjector.SaveFocusedWindow();
                StartRecording();
            }
        }
    }

    private void StartRecording()
    {
        // Cancel any pending status reset from previous recording
        _statusResetCts?.Cancel();

        int micIndex = 0;
        if (CmbMicrophone.SelectedItem is ComboBoxItem micItem)
            micIndex = (int)(micItem.Tag ?? 0);

        _recorder.StartRecording(micIndex);

        StatusText.Text = I18n.Get("StatusRecording");
        StatusText.Foreground = (SolidColorBrush)FindResource("AccentRedBrush");

        _pulseStoryboard = (Storyboard)FindResource("PulseAnimation");
        _pulseStoryboard.Begin();
    }

    private void StopRecording()
    {
        _recorder.StopRecording();
        _pulseStoryboard?.Stop();
        RecordGlow.Opacity = 0;
        StatusText.Text = I18n.Get("StatusTranscribing");
        StatusText.Foreground = (SolidColorBrush)FindResource("AccentOrangeBrush");
    }

    private async void OnRecordingStopped(byte[] audioData)
    {
        await Dispatcher.InvokeAsync(async () =>
        {
            try
            {
                var lang = "zh";
                if (CmbLanguage.SelectedItem is ComboBoxItem langItem)
                    lang = langItem.Tag?.ToString() ?? "zh";

                var text = await _transcribeClient.TranscribeAsync(
                    audioData, lang, _settings.Punctuation);

                if (!string.IsNullOrWhiteSpace(text))
                {
                    TranscriptBox.Text = text;
                    TranscriptBox.Foreground = (SolidColorBrush)FindResource("TextPrimaryBrush");

                    // Auto copy
                    if (_settings.AutoCopyToClipboard)
                        Clipboard.SetText(text);

                    // Type into focused window
                    TextInjector.RestoreAndType(text, _settings.AutoEnterOnPunctuation);

                    StatusText.Text = I18n.Get("StatusDone");
                    StatusText.Foreground = (SolidColorBrush)FindResource("AccentGreenBrush");
                }
                else
                {
                    StatusText.Text = I18n.Get("StatusDone");
                    StatusText.Foreground = (SolidColorBrush)FindResource("TextSecondaryBrush");
                }
            }
            catch (Exception ex)
            {
                StatusText.Text = $"{I18n.Get("StatusError")}: {ex.Message}";
                StatusText.Foreground = (SolidColorBrush)FindResource("AccentRedBrush");
            }

            // Reset status after 3 seconds (cancellable)
            _statusResetCts = new CancellationTokenSource();
            try
            {
                await Task.Delay(3000, _statusResetCts.Token);
                StatusText.Text = I18n.Get("StatusIdle");
                StatusText.Foreground = (SolidColorBrush)FindResource("TextSecondaryBrush");
            }
            catch (TaskCanceledException) { }
        });
    }

    private void OnVolumeChanged(float volume)
    {
        Dispatcher.Invoke(() =>
        {
            VolumeBar.Width = volume * 200;
        });
    }

    #endregion

    #region Settings

    private void BtnToggleSettings_Click(object sender, RoutedEventArgs e)
    {
        SettingsPanel.Visibility = SettingsPanel.Visibility == Visibility.Visible
            ? Visibility.Collapsed
            : Visibility.Visible;
    }

    private async void BtnSave_Click(object sender, RoutedEventArgs e)
    {
        // --- Apply immediately ---
        _settings.ServerUrl = TxtServerUrl.Text.Trim();
        _transcribeClient.UpdateBaseUrl(_settings.ServerUrl);

        if (CmbLanguage.SelectedItem is ComboBoxItem langItem)
            _settings.Language = langItem.Tag?.ToString() ?? "zh";

        if (CmbMicrophone.SelectedItem is ComboBoxItem micItem)
            _settings.MicrophoneDeviceIndex = (int)(micItem.Tag ?? 0);

        if (CmbRecordMode.SelectedItem is ComboBoxItem modeItem)
        {
            _settings.RecordMode = modeItem.Tag?.ToString() ?? "PushToTalk";
            _isToggleRecording = false; // Reset toggle state on mode change
        }

        _settings.AutoCopyToClipboard = ChkAutoCopy.IsChecked ?? true;
        _settings.AutoEnterOnPunctuation = ChkAutoEnter.IsChecked ?? false;
        _settings.Punctuation = ChkPunctuation.IsChecked ?? true;

        // UI Language change
        if (CmbUILanguage.SelectedItem is ComboBoxItem uiLangItem)
        {
            var newCulture = uiLangItem.Tag?.ToString() ?? "zh-TW";
            I18n.SetLanguage(newCulture);
            ApplyI18n();
        }

        // Persist to disk
        _settings.Save();

        // Re-register hotkey with new settings
        _hotkeyManager.Unregister();
        RegisterHotkey();

        // Immediately check health with new server URL
        _ = CheckHealth();

        // Show save confirmation toast
        var prevStatus = StatusText.Text;
        var prevFg = StatusText.Foreground;
        StatusText.Text = "✅ " + I18n.Get("SettingsSave") + "!";
        StatusText.Foreground = (SolidColorBrush)FindResource("AccentGreenBrush");
        SettingsPanel.Visibility = Visibility.Collapsed;

        await Task.Delay(1500);
        StatusText.Text = I18n.Get("StatusIdle");
        StatusText.Foreground = (SolidColorBrush)FindResource("TextSecondaryBrush");
    }

    #endregion

    #region Window Events

    private void BtnCopy_Click(object sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrWhiteSpace(TranscriptBox.Text) &&
            TranscriptBox.Text != I18n.Get("TranscriptPlaceholder"))
        {
            Clipboard.SetText(TranscriptBox.Text);
        }
    }

    private void Window_StateChanged(object sender, EventArgs e)
    {
        if (WindowState == WindowState.Minimized)
        {
            Hide();
            _trayIcon!.Visibility = Visibility.Visible;
        }
    }

    private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        _recorder.Dispose();
        _transcribeClient.Dispose();
        _hotkeyManager.Dispose();
        _trayIcon?.Dispose();
        _healthTimer.Stop();
    }

    #endregion
}
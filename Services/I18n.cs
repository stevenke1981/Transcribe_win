using System.Globalization;

namespace TranscribeWin.Services;

public static class I18n
{
    private static string _currentLang = "zh-TW";

    private static readonly Dictionary<string, Dictionary<string, string>> _strings = new()
    {
        ["zh-TW"] = new()
        {
            ["AppTitle"] = "語音轉文字",
            ["StatusIdle"] = "待命中",
            ["StatusRecording"] = "錄音中...",
            ["StatusTranscribing"] = "轉錄中...",
            ["StatusDone"] = "完成",
            ["StatusError"] = "錯誤",
            ["BtnRecord"] = "🎙️ 按住錄音",
            ["BtnStop"] = "⏹️ 停止",
            ["BtnSettings"] = "⚙️ 設定",
            ["SettingsTitle"] = "設定",
            ["SettingsServerUrl"] = "伺服器網址",
            ["SettingsLanguage"] = "轉錄語言",
            ["SettingsMicrophone"] = "麥克風",
            ["SettingsHotkey"] = "快捷鍵",
            ["SettingsRecordMode"] = "錄音模式",
            ["SettingsPushToTalk"] = "按住說話",
            ["SettingsToggle"] = "切換模式",
            ["SettingsAutoCopy"] = "自動複製到剪貼簿",
            ["SettingsAutoEnter"] = "標點結尾自動 Enter",
            ["SettingsPunctuation"] = "啟用標點符號",
            ["SettingsSave"] = "儲存",
            ["SettingsUILanguage"] = "介面語言",
            ["ServerOnline"] = "伺服器連線中",
            ["ServerOffline"] = "伺服器離線",
            ["TrayShow"] = "顯示",
            ["TrayExit"] = "結束",
            ["TranscriptPlaceholder"] = "轉錄文字與歷史紀錄將顯示在這裡...",
            ["TranscriptCopied"] = "已複製!",
            ["HistoryTooltip"] = "開啟歷史紀錄檔案",
            ["HotkeyRegistered"] = "快捷鍵已註冊: {0}",
            ["HotkeyFailed"] = "快捷鍵註冊失敗",
        },
        ["en-US"] = new()
        {
            ["AppTitle"] = "Voice to Text",
            ["StatusIdle"] = "Idle",
            ["StatusRecording"] = "Recording...",
            ["StatusTranscribing"] = "Transcribing...",
            ["StatusDone"] = "Done",
            ["StatusError"] = "Error",
            ["BtnRecord"] = "🎙️ Hold to Record",
            ["BtnStop"] = "⏹️ Stop",
            ["BtnSettings"] = "⚙️ Settings",
            ["SettingsTitle"] = "Settings",
            ["SettingsServerUrl"] = "Server URL",
            ["SettingsLanguage"] = "Language",
            ["SettingsMicrophone"] = "Microphone",
            ["SettingsHotkey"] = "Hotkey",
            ["SettingsRecordMode"] = "Record Mode",
            ["SettingsPushToTalk"] = "Push to Talk",
            ["SettingsToggle"] = "Toggle",
            ["SettingsAutoCopy"] = "Auto Copy to Clipboard",
            ["SettingsAutoEnter"] = "Auto Enter on Punctuation",
            ["SettingsPunctuation"] = "Enable Punctuation",
            ["SettingsSave"] = "Save",
            ["SettingsUILanguage"] = "UI Language",
            ["ServerOnline"] = "Server Online",
            ["ServerOffline"] = "Server Offline",
            ["TrayShow"] = "Show",
            ["TrayExit"] = "Exit",
            ["TranscriptPlaceholder"] = "Transcribed text and history will appear here...",
            ["TranscriptCopied"] = "Copied!",
            ["HistoryTooltip"] = "Open History File",
            ["HotkeyRegistered"] = "Hotkey registered: {0}",
            ["HotkeyFailed"] = "Hotkey registration failed",
        }
    };

    public static string Get(string key)
    {
        if (_strings.TryGetValue(_currentLang, out var dict) && dict.TryGetValue(key, out var val))
            return val;
        if (_strings.TryGetValue("zh-TW", out var fallback) && fallback.TryGetValue(key, out var fb))
            return fb;
        return key;
    }

    public static string Get(string key, params object[] args)
    {
        return string.Format(Get(key), args);
    }

    public static void SetLanguage(string cultureName)
    {
        _currentLang = cultureName;
        var culture = new CultureInfo(cultureName);
        CultureInfo.CurrentUICulture = culture;
        Thread.CurrentThread.CurrentUICulture = culture;
    }

    public static string CurrentLanguage => _currentLang;
}

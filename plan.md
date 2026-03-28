# Transcribe Win - 語音轉文字焦點視窗 App

## 概述
WPF 桌面 App，錄音 → 送到 Cohere Transcribe Server → 把轉錄文字打到焦點視窗。

## API (http://192.168.80.60:9000)
- **POST** `/v1/audio/transcriptions` — multipart/form-data (file, model, language, punctuation, response_format, temperature)
- **GET** `/healthz` — 伺服器健康檢查

## 技術棧
- .NET 8 WPF
- NAudio (錄音)
- Hardcodet.NotifyIcon.Wpf (系統匣)
- Win32 SendInput (文字注入焦點視窗)

## 架構

### Services/
| 檔案 | 功能 |
|------|------|
| `AudioRecorder.cs` | NAudio 麥克風錄音 (16kHz/16bit/mono WAV)、靜音偵測 |
| `TranscribeClient.cs` | HttpClient 呼叫 `/v1/audio/transcriptions` |
| `TextInjector.cs` | Win32 SendInput 打字到焦點視窗 |
| `HotkeyManager.cs` | 全域熱鍵 RegisterHotKey (預設 Ctrl+Alt+Space) |

### Models/
| 檔案 | 功能 |
|------|------|
| `AppSettings.cs` | 設定模型 + JSON 持久化 |

### UI
| 檔案 | 功能 |
|------|------|
| `MainWindow.xaml` | 深色主題 UI：錄音按鈕、狀態、轉錄結果、設定面板 |
| `App.xaml` | 全域樣式、深色主題資源 |

## 功能
1. 🎙️ 按住 `Ctrl+Alt+Space` 錄音，放開即轉錄並打字
2. 🔄 支援 Toggle 模式 (按一次開始/再按停止)
3. 📋 可選自動複製到剪貼簿
4. ⏎ 標點符號結尾自動 Enter
5. 🔧 可設定：伺服器 URL、語言、麥克風、快捷鍵
6. 📌 最小化到系統匣

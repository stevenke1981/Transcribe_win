# WPF 語音轉文字工具開發紀錄 (Transcribe Win)

本文件紀錄了此 WPF 應用程式從無到有的核心開發流程、技術決策、遭遇的問題及解決方案。

## 1. 核心需求與架構設計

**初始化開發與目標：**
- **平台與框架**：目標為 Windows 平台，選用 .NET 8 WPF 建立乾淨、具備現代感（GitHub Dark 主題）的 UI 介面。
- **後端端點**：對接本地的 Cohere Transcribe 伺服器 HTTP API (`http://192.168.80.60:9000/v1/audio/transcriptions`)。
- **核心功能**：語音錄製、文字轉錄、全域快捷鍵操作、自動打字發送到上一個啟用的焦點視窗。

**架構模組化：**
對功能進行了切分，確保主視窗程式碼盡量簡潔：
- `AppSettings.cs`: 處理設定的序列化與反序列化 (`settings.json`)，包含 API URL、錄音模式、自動 Enter、繁簡轉換等行為配置。
- `AudioRecorder.cs`: 使用 `NAudio` (v2.2.1) 處理麥克風截取，設定為 16kHz、16bit、單聲道以符合多數語音模型的預期格式。包含透過 RMS (Root-Mean-Square) 估算音量的邏輯，提供即時的視覺回饋。
- `TranscribeClient.cs`: 使用 `HttpClient` 發送含語音資料的 `multipart/form-data` 請求，以及處理伺服器的 `/healthz` 健康檢查。
- `TextInjector.cs`: 負責在轉錄完成後，精確地把文字用鍵盤輸入到目標視窗上。
- `HotkeyManager.cs`: 負責綁定全域快捷鍵。
- `I18n.cs`: 使用基於本機資料結構 (`Dictionary`) 的雙語支援系統 (zh-TW / en-US)，避免 `.resx` 資源檔容易導致的命名空間與編譯期綁定錯誤。

---

## 2. 階段性功能演進與問題排除

### 階段一：實作底層邏輯與介面 (V1)
- **UI 與動畫**：製作了毛玻璃特效的面版、發光的雷達波紋錄音提示 (Storyboard PulseAnimation) 以及可折疊的系統設定面板。
- **基礎錄音**：使用 `NAudio.WaveInEvent` 獲得即時音量，結束時轉換為 `MemoryStream` 以寫出 .wav 標頭與資料。
- **全域快捷鍵 1.0**：最初透過 Win32 `RegisterHotKey` API 註冊系統全域快捷鍵。
  🔴 **問題**：`RegisterHotKey` 只能捕捉「按下 (KeyDown)」的時機。
  🟢 **方案**：改寫 `HotkeyManager`，引入低階鍵盤掛鉤 `SetWindowsHookEx(WH_KEYBOARD_LL)` 來同時偵測 KeyDown 與 KeyUp，這成了實現「按住錄音 (Push-to-Talk)」功能的核心。

### 階段二：精進設定重載與流程改善 (V2)
- **即時套用機制**：確保使用者在修改「錄音模式」、「快捷鍵」或「API Url」後不需重啟軟體。按下儲存會動態關閉並重啟掛鉤、重新啟動物件。
- **解決健康檢查堵塞問題**：
  🔴 **問題**：如果在伺服器離線時按下儲存按鈕，健康檢查預設 60 秒的 Timeout 會造成介面卡住。確保 `HttpClient` 長時間等待的問題不影響主執行緒。
  🟢 **方案**：實例化一個僅用於健康檢查的短 Timeout (`_healthClient`, 3秒超時)，並將等待隔離在平行 Task。
- **RMS 運算整數溢位**：
  🔴 **問題**：計算音量矩陣時 `short * short` 超過了 `float sum` 所能承載的最大值產生 Overflow 溢位，且陣列數為 0 時有除零風險。
  🟢 **方案**：改由 `(long)sample * sample` 後才轉換數值，確保了錄音波形反應更穩定。

### 阶段三：改善鍵盤文字注入模組 (TextInjector)
- 轉錄後需要自動將內容送出到使用者先前聚焦的視窗（如記事本或網頁）。
- **Win32 SendInput 遭遇問題**：
  🔴 **問題**：原本透過 Win32 底層呼叫 `SendInput`，手動轉換文字成為 `KEYEVENTF_UNICODE` 事件時，某些軟體或高權限視窗有時候吃不到輸入。
  🟢 **方案**：捨棄繁雜的 struct 定義，直接宣告採用更穩定且專門處理虛擬按鍵對接的 `InputSimulatorCore` (NuGet套件)。利用 `InputSimulator.Keyboard.TextEntry(text)` 能夠自動處理各種複雜甚至標點符號的輸入細節，大幅提升可靠性。
- 為了確保不打字打到自己的軟體上，我們在 `StartRecording()` 被觸發的第一時間先利用 `GetForegroundWindow()` 記下畫面擁有權。

### 階段四：新增進階易用性功能

#### 1. 系統匣化與隱形模式 (System Tray)
因為沒有附帶現成的外部 `.ico` 圖示檔，直接利用 C# `System.Drawing` 的 API 用**程式碼憑空動態畫出**一個麥克風圖形 (`CreateTrayIcon`) 作為系統右下角托盤的圖案（藍色圓底＋底座），讓程式得以完美最小化至系統匣並支援背景錄音，不占據桌面空間。

#### 2. 即時歷史紀錄與獨立視窗
- 主畫面輸入框邏輯更改為「僅顯示最近一次的轉錄文字」，以維持精簡感。
- 增加 `HistoryWindow.xaml` 成為一個獨自的歷程記錄頁面。每次錄音完成時，把結果套用 `[HH:mm:ss]` 時間戳記附加儲存到執行目錄下的 `history.txt` 檔案。這樣既保證程式重啟不遺失，又可隨時查閱。

#### 3. 簡體自動轉換為繁體 (零依賴模組)
- 由於開源語音模型在轉錄許多專有名詞時經常回傳「簡體中文」，為了讓在台灣等使用繁體的使用者更好用，需加入自動轉換功能。
- 為了保持程式極小化不載入數十 MB 的 OpenCC 函式庫，自製了 `ChineseConverter.cs` 。
- **技術決策**：透過 P/Invoke 調用 Windows 本機強大的 `kernel32.dll` 中的 `LCMapString` `0x0804` (`LCMAP_TRADITIONAL_CHINESE`) 標記來達成繁簡轉換，效率極高且零額外依賴。

---

## 未來展望與擴充可能
1. **指令介面 / LLM 結合**：可將翻譯出的文字再次打給內建的輕量型 LLM（例如過濾語氣詞或進行自動校閱）。
2. **更多語系轉換**：擴充除了 `LCMapString` 外的其他多國語系轉換與自訂熱詞修正辭典（如果模型有嚴重錯讀）。
3. **介面顯示增強**：對超長錄音引入 Streaming WebSocket 推入方式而非單點發送，這需伺服器端（Cohere） API 具有對應支援。

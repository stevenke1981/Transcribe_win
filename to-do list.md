# 商用版本進階功能規劃 (To-Do List)

若要將此「語音轉文字 (Transcribe Win)」應用程式從個人/內部工具推向**商業化產品（SaaS 或 付費軟體）**，建議擴充以下幾個方面的能力與機制：

## 1. 軟體發布與安全 (Deployment & Security)
- [ ] **正規安裝程式**：製作專屬的安裝檔 (如 MSIX、Inno Setup 或 WiX)，提供乾淨的安裝與反安裝體驗。
- [ ] **程式碼簽章 (Code Signing)**：購買 EV 憑證對 `.exe` 與 `.dll` 進行數位簽章，避免被 Windows Defender 或 SmartScreen 阻擋並顯示「不明的發行者」。
- [ ] **自動更新機制 (Auto-Updater)**：整合 Squirrel.Windows、WinSparkle 或 GitHub Releases，讓應用程式能在背景偵測新版本並提醒/自動升級。

## 2. 商業模式與帳號系統 (Monetization & Auth)
- [ ] **帳號註冊與登入**：支援 Email / Google / Microsoft / Apple OAuth 第三方登入。
- [ ] **授權與訂閱管理 (Billing)**：串接 Stripe 或 LemonSqueezy，實現 Freemium (免費額度) 與 Pro (訂閱/買斷) 機制。
- [ ] **用量限制 (Quota)**：若後端 API 由官方雲端提供，需加入「每月轉錄時數／字數」的追蹤與限流防護。

## 3. 核心語音與 AI 增強功能 (Advanced Transcription)
- [ ] **串流即時轉錄 (Streaming ASR)**：改用 WebSocket / gRPC 取代目前的 HTTP POST。在使用者講話的同時「即時」在畫面上吐出文字，而不是等放開按鍵後才一次轉錄。
- [ ] **AI 降噪 (Noise Suppression)**：整合本機端 AI 降噪模組 (如 RNNoise)，過濾鍵盤與背景雜音，提升 ASR 伺服器辨識準確度。
- [ ] **自訂熱詞與專有名詞庫 (Hotwords/Custom Vocabulary)**：允許專業人士 (如醫生、律師、工程師) 加入自訂字庫，避免行業術語被模型辨識錯誤。
- [ ] **AI 潤飾與翻譯 (LLM Integration)**：
  - 過濾語氣詞 (`嗯`、`啊`、`這個`)。
  - 將轉錄出的語音直接透過 LLM 整理成流暢的文章或條列式重點。
  - 語音實時翻譯 (講中文，打字出來是英文)。

## 4. 使用者體驗與介面 (UX/UI)
- [ ] **首次啟動導覽 (Onboarding)**：新手教學流程，說明快捷鍵用法並要求設定預設語言。
- [ ] **麥克風硬體測試精靈**：在設定面板加入可以測試收音、調整麥克風增益 (Gain) 的介面，提早排除沒聲音的問題。
- [ ] **浮動小工具 (Floating Widget)**：提供一個可在螢幕最上層的小巧狀態指示燈，讓使用者在玩遊戲或全螢幕工作時，能隨時看見目前的錄音與轉錄進度。
- [ ] **深淺色主題切換**：目前已實作 Dark Mode，可加入系統預設亮色 (Light Mode) 與自訂主題色支援。

## 5. 營運支援與數據回饋 (Ops & Telemetry)
- [ ] **崩潰報告 (Crash Reporting)**：整合 Sentry 或 MS AppCenter，當應用程式發生未預期閃退時自動回傳 Exception Log 以便工程師修復。
- [ ] **隱私合規分析 (Analytics)**：整合 PostHog 或 Mixpanel，在「取得使用者同意 (Opt-in 取向)」的前提下追蹤按鈕與熱鍵的使用頻率，以優化產品。
- [ ] **完整的使用者條款與隱私權政策 (TOS & Privacy Policy)**。

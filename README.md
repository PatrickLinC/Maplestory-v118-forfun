# Maplestory v118 for fun

楓之谷 **TMS v119.1**(TWMS_118 原始碼,搭配 v118 客戶端)私人伺服器的**自訂開發**專案。

> ⚠️ **這個 repo 只收「我們自己寫的、可公開的技術程式碼與文件」**。
> Nexon/遊戲橘子的**客戶端、WZ 資源、伺服器模擬器發行版、JRE、資料庫、記憶體 dump、編譯產物**等**一律不含**(有版權、且體積達數十 GB)。
> 內容僅供**個人學習與研究**,請自備合法的客戶端與伺服器環境。

---

## 招牌玩法:PoE 風格附魔系統

本伺服器的核心特色是一套 **PoE(流亡黯道)風格的附魔石／潛能系統**:24 種詞綴、10 種附魔石,對應賦予／重洗／加排／污染等玩法,並在原生不支援的 v118 客戶端上把詞綴顯示到裝備 tooltip。

👉 **玩法完整介紹見 [docs/PoE附魔系統.md](docs/PoE附魔系統.md)。**

不過這套玩法的**實作原始碼**(引擎、數值表、機率、整合掛鉤)是本服獨門設計,**不放上 GitHub**;有興趣交流實作歡迎私訊。repo 內公開的是通用技術的部分(見下)。

---

## repo 內公開的部分

| 主題 | 內容 |
|---|---|
| **客戶端 tooltip 改造(通用逆向技術)** | v118 客戶端原生不顯示潛能行。透過 inline hook 讀取 item 欄位所編碼的資料、翻譯後畫到裝備素質下方。這是通用的 tooltip 顯示技術;詞綴名稱表(`names.h`)已以佔位符替代,不揭露玩法設計。見 [`client-hook/`](client-hook/)。 |
| **伺服器位元碼注入** | 用 Javassist 對 `TWMS.jar` 注入改動(分段經驗、背包全開、寵物技能、自訂指令等)。因該檔同時整合了 PoE 附魔掛鉤,**實作未公開**;通用改動的說明見開發日記。 |
| **NPC / WZ / 逆向工具** | 世界旅遊 NPC;HaRepacker/MapleLib 程式化編輯 WZ;一批記憶體掃描/反組譯/驗證用的小工具。見 [`server/scripts/`](server/scripts/)、[`tools/wztool/`](tools/wztool/)。 |

完整開發歷程(高層次)見 **[開發日記.md](開發日記.md)**。

---

## 目錄結構

```
server/scripts/npc/9000020.js   史匹奈亞世界旅遊 NPC(四國選單)
client-hook/                    v118 客戶端 tooltip 改造 DLL 原始碼(通用顯示技術)
  show.cpp / names.h            主 hook(names.h 詞綴名已佔位符化)
  AutoInject.cs / Injector.cs   DLL 注入器
  *.cpp                         開發過程各版診斷 hook
tools/
  OpProbe.java                  封包 opcode 探測工具
  wztool/                       WZ 編輯 / 記憶體掃描 / 反組譯輔助(C# + Python)
launchers/                      客戶端啟動批次檔(範本;IP 已抽換成佔位符)
docs/PoE附魔系統.md              招牌附魔玩法介紹(純玩法,無實作碼)
```

---

## 技術備忘(踩雷點)

- **wzpath 要以 `/` 結尾**;WZ 解包檔名要是 `<名稱>.img.xml`,否則伺服器起不來。
- **NPC 腳本含中文必須存 UTF-8 with BOM**,否則被當 GBK 解成亂碼。
- **客戶端 WZ 加密是 `WzMapleVersion.EMS`**;`SaveToDisk` 第二參數要傳 `(bool?)false`;捲軸節點 `DeepClone()` 會爆棧,要從零 new。
- **v120/v122 客戶端在開發機繪圖出不來**(黑屏/亂碼 Error),故顯示相關功能改走 v118 方案。

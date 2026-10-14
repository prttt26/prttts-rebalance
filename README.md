# prttt's Rebalance

**Energy & Deck Cycling** — 以能量经济和牌组运转为重点的《杀戮尖塔2》平衡 Mod。

调整战士、猎人、机器人、亡灵契约师和储君的部分卡牌及卡池，涉及能量获取、费用、抽牌、弃牌、回收和跨回合资源安排，并引入部分塔1经典卡牌和新设计卡牌。提供简体中文与英文文本，需要 BaseLib。

An opinionated Slay the Spire 2 rebalance focused on energy economy and deck cycling, with adjustments across all five characters, selected returning cards, and new designs. Includes Simplified Chinese and English text. Requires BaseLib.

创意工坊 / Steam Workshop：https://steamcommunity.com/sharedfiles/filedetails/?id=3819462077

## 本机构建

需要 .NET SDK（目标框架 net9.0）和本机《杀戮尖塔2》安装。游戏程序集由项目从 Steam 安装目录读取，不包含在仓库中。

在本仓库根目录执行：

```powershell
dotnet restore ./Rebalance_by_sts1_revival.csproj
powershell -NoProfile -ExecutionPolicy Bypass -File ./workshop/Prepare-Release.ps1
```

脚本生成 `publish/prttts-rebalance/` 官方上传工作区，以及 `publish/in-game-uploader/Rebalance_by_sts1_revival/` 本地 Mod 文件夹。内部 ID 保留 `Rebalance_by_sts1_revival`。发布包须实机验证；本 Mod 可能与修改相同卡牌或卡池的其他 Mod 冲突。

## 项目资料

- [发布及更新流程](workshop/RELEASE.md)
- [卡牌改动列表](CARD_CHANGES.md)

公开源码未授予额外的开源许可；第三方与游戏素材的权利归各自权利人。

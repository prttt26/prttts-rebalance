# 塔2 Mod 开发环境搭建 SOP（Windows 笔记本）

> 初版在无游戏、无 dotnet 的 Linux 服务器上写成（2026-07-17），所有事实来自上游仓库文件。
>
> **2026-07-26 已在笔记本上全程实机跑通**：环境搭建 → 构建 → 游戏内加载 →
> add / remove / modify（含**改数值**和**改效果**两种）全部验证，0 error。
> 原先带 ⚠️ 的条目已全部有结论，逐条标注在下方。
> **原「🔴 最大技术风险」已解除，可以开始批量写卡。**
>
> 实机环境：Windows 11 / 游戏 v0.107.1 / BaseLib v3.3.7（创意工坊）/ .NET SDK 9.0.316

## 0. 背景：塔2 与塔1 完全不同

塔2 是 **MegaCrit fork 的 Godot 4.5.1 + C# + .NET 9**（Unity 授权风波后换的引擎）。
塔1 的 ModTheSpire / BaseMod（Java/LibGDX）**全部作废**，经验不能迁移。

- Steam AppID：`2868840`
- 状态：Early Access（2026-03-05 起）
- 创意工坊：v0.107.1（2026-06-19）才开放
- 官方 modding 文档：**基本不存在**。官方只发布了上传工具。
  真正的文档是社区的（见文末链接）。

---

## 1. 装 .NET 9 SDK ✅

winget 一条命令即可（比手动下 installer 省事）：

```powershell
winget install --id Microsoft.DotNet.SDK.9 -e
dotnet --list-sdks
```

⚠️ **装完当前终端里 `dotnet` 还不可用**，要么重开终端，要么临时
`$env:PATH = "C:\Program Files\dotnet;$env:PATH"`。

**本机同时装了 9.0.316 和 10.0.302，`dotnet --version` 显示 10.0.302** ——
这不影响：模板的 `TargetFramework` 是 `net9.0`，SDK 10 能正常编 net9.0（9.0 targeting pack
由 SDK 9 提供）。不需要加 global.json。

## 2. 装 IDE

Rider（社区 wiki 推荐）或 Visual Studio 均可。
**纯命令行 `dotnet build` 也完全够用**，本次全程没开 IDE。

## 3. 确认游戏已安装 ✅

本机实测路径就是默认路径：
```
C:\Program Files (x86)\Steam\steamapps\common\Slay the Spire 2
```
`data_sts2_windows_x86_64\` 里 `sts2.dll`、`0Harmony.dll`、`SmartFormat.dll`、
`Steamworks.NET.dll` 均在。

**版本号看 `release_info.json`**（决定 §6 选包）：本机为 `v0.107.1`。

> 来源：[BaseLib.csproj](https://github.com/Alchyr/BaseLib-StS2/blob/master/BaseLib.csproj) 的 `<Reference>` 段

## 4. 装项目模板

```powershell
dotnet new install Alchyr.Sts2.Templates
```

三个模板短名（**已逐个从 template.json 核实**）：

| 短名 | 用途 |
|---|---|
| `alchyrsts2mod` | Empty mod |
| `alchyrsts2contentmod` | **Content mod ← 本项目用这个** |
| `alchyrsts2charmod` | Character mod |

本项目改的是既有角色卡池 + 无色卡池，**不新增角色**，所以用 Content。

> 来源：[ModTemplate-StS2](https://github.com/Alchyr/ModTemplate-StS2) 各 `.template.config/template.json`

## 5. 生成工程 ✅

本项目实际用的命令（工程名即 mod id，定了别改）：

```powershell
dotnet new alchyrsts2contentmod -n Rebalance_by_sts1_revival --ModAuthor "prttt26"
```

- `ModAuthor` 是必填参数
- ⚠️ **模板把工程文件生成在「当前目录」，不是 `-n` 指定的子目录**。
  跑完 `sts2/` 下会多出 `project.godot`、`<ModName>.csproj`、`<ModName>.json`、
  `Directory.Build.props`、`Sts2PathDiscovery.props`、`export_presets.cfg`、`.gitignore`，
  而 `-n` 的同名子目录只放美术/本地化资源，代码在 `<ModName>Code/`。
  这就是 README 里 "Put solution and project in same directory" 的 CLI 版表现，属正常。
  **副作用：Godot 工程根目录 = `sts2/`，SETUP.md / cards.yaml 等文档也在工程根下**
  （实测不影响导出，Godot 只打包它认识的 resource）。
- `PublicizeSts` 结论：**目前不需要开**。模板 csproj 里已经写好
  `<ItemGroup Condition="False">` 的 `<Publicize Include="sts2" />`，要开就把 `False` 改 `True`。
  本次 add/remove/modify 三类改动全程没用到 private 成员。

模板默认已带 BaseLib 依赖 ✅（csproj 里 `<PackageReference Include="Alchyr.Sts2.BaseLib" />`，
manifest 里 `dependencies` 也自动写好），我们要用，所以不动。

### 5.1 ⚠️ BaseLib 版本必须钉住（实机踩到的坑）

模板写的是 `Version="*"`，会解析到 NuGet 最新的 **3.3.8**，并由 csproj 里的
`UpdateDependencyVersions` target 把 `min_version` 自动写进 manifest。
但**创意工坊上的 BaseLib 运行时是 v3.3.7，而 NuGet 上根本没有 3.3.7**（3.3.6 直接跳 3.3.8）。
`min_version: 3.3.8` > 装着的 3.3.7 → 游戏会拒绝加载你的 mod。

所以钉到 3.3.6：
```xml
<PackageReference Include="Alchyr.Sts2.BaseLib" Version="3.3.6" PrivateAssets="All"/>
```
等创意工坊 BaseLib 更新到 ≥3.3.8 之后再往上抬。**每次 BaseLib 更新都要重新对一次这个版本号。**

## 6. 加 RitsuLib 依赖（两处都要写）

> **2026-07-26 实机结论：本项目暂时不加 RitsuLib。**
> add / remove / modify 三条路子用 **BaseLib + 裸 Harmony** 就全部走通了（见文末）。
> 加 RitsuLib 会让玩家侧多一个必须订阅的运行时 mod，收益不明确，先不加。
> 下面内容保留，等真的需要时再用。
>
> 若要加，**版本要钉**：本机订阅的 RitsuLib 运行时是 **0.4.60**，
> 而 NuGet 上 `STS2.RitsuLib.Compat.0.107.1` 已经到 **0.4.64**。
> 编译期版本高于运行时版本 → 用到新 API 会 MissingMethodException。
> 按本机装的版本钉 `Version="0.4.60"`（这一点和下面「不要写死版本号」的通用建议相反，
> 以实机装的运行时版本为准）。
> 运行时那侧不用操心：workshop 包里带 `lib/0.107.1`、`lib/0.108.0`、`lib/0.109.0`
> 三个变体，靠 `ritsulib-variants.manifest` 自动选。

RitsuLib 与 BaseLib 是**并存**关系，其 README 明确写 "It sits beside the base game API
and other libraries such as BaseLib instead of replacing them"，且优先保证 BaseLib 兼容。

**6.1 编译期** —— `.csproj`：
```xml
<PackageReference Include="STS2.RitsuLib" />
```
> ⚠️ **不要写死版本号**。RitsuLib README 明确说：若未用 Central Package Management，
> 让 NuGet/IDE 自己选当前兼容版本，不要照抄文档里的固定版本。
> （该库 7/4–7/17 两周发了 9 个版本，写死必然过期）

**6.2 运行期** —— manifest 的 `dependencies`，游戏 API 0.105+ 用对象形式：
```json
"dependencies": [
  { "id": "STS2-RitsuLib" }
]
```
更老的 API 分支要用旧版字符串形式 `["STS2-RitsuLib"]`，因为老解析器会在对象形式上失败。

**6.3 选包**：
| 场景 | 编译期包 |
|---|---|
| 跟游戏最高 API（通常是 beta 分支） | `STS2.RitsuLib` |
| 正式/较老分支 | `STS2.RitsuLib.Compat.<api-version>` |

⚠️ **先确认你笔记本上游戏的实际版本**，再决定用哪个。当前活跃的 compat 包是
`0.107.1` 和 `0.108.0`。玩家侧还需要订阅 RitsuLib 运行时 mod
（[Workshop](https://steamcommunity.com/sharedfiles/filedetails/?id=3747602295)）。

> 来源：[RitsuLib README](https://github.com/BAKAOLC/STS2-RitsuLib/blob/main/README.md)

## 7. 游戏路径配置 ✅ 不用管

`Sts2PathDiscovery.props` 的自动探测**本机直接命中**（注册表两个键都在），
没有做任何路径配置。探测顺序：

1. 注册表 `HKLM\...\Uninstall\Steam App 2868840` 的 `InstallLocation`
2. `HKCU\Software\Valve\Steam@SteamPath` + `\steamapps`
3. 兜底 `C:/Program Files (x86)/Steam/steamapps`

探测失败时（游戏装在别的 Steam 库盘）才需要手动填。
⚠️ **模板 2.5.0 已经不用 `local.props` 了**，改成在 `Directory.Build.props` 里填：
```xml
<Sts2Path>D:\SteamLibrary\steamapps\common\Slay the Spire 2</Sts2Path>
```
`Directory.Build.props` 已在模板生成的 `.gitignore` 里，不会提交，各机器可不同。

### 7.1 GodotPath / .pck ✅ 不用装 Godot

`Directory.Build.props` 里默认写死 `C:/megadot/MegaDot_v4.5.1-stable_mono_win64.exe`，
本机没这个文件——**不用装**。改用模板 csproj 里注释掉的那行，把它打开：

```xml
<PackageReference Include="BSchneppe.StS2.PckPacker" Version="0.1.1" PrivateAssets="All" />
```

实测 `dotnet build` 就会直接产出 `.pck` 并拷进 mods 目录：
```
PCK packed: ...\Rebalance_by_sts1_revival.pck
Copying quick .pck to mods folder.
```
本地化 JSON 走的就是这个 .pck，所以**这一步是必须的**（manifest 里 `has_pck: true`）。
只有 PckPacker 搞不定的复杂资源（场景/shader）才需要真去装 MegaDot 走 Publish。

> 来源：[Sts2PathDiscovery.props](https://github.com/Alchyr/BaseLib-StS2/blob/master/Sts2PathDiscovery.props)

## 8. Manifest ✅ 文件名已确认

文件名就是 **`<ModName>.json`**，跟项目名走。模板自动生成、自动拷贝，不用手工建。
游戏日志实证：

```
[INFO] Found mod manifest file ...\mods\Rebalance_by_sts1_revival\Rebalance_by_sts1_revival.json
```

> RitsuLib 那个 `mod_manifest.json` 确实是特例 —— 它的 workshop 目录名是数字 id
> （`3747602295`），`<文件夹名>.json` 这套对它不成立，所以它用了固定名。
> 加载器两种都吃，但**你自己的 mod 跟着模板走 `<ModName>.json` 就对了**。

字段（据 `BaseLib.json` 实物）：
```json
{
  "id": "ModName",
  "name": "ModName",
  "author": "prttt26",
  "description": "...",
  "version": "v1.0.0",
  "has_pck": true,
  "has_dll": true,
  "dependencies": [],
  "affects_gameplay": true
}
```
- **`id` 决定游戏加载时找的文件名，定了就别改**
- 本 mod 改卡池，`affects_gameplay` 应为 `true`
- 其他见过的字段：`pck_name`、`min_game_version`

## 9. 构建与加载验证 ✅

`CopyToModsFolderOnBuild` target 实测好用，Build 后 dll / pdb / json / pck 四件套自动进
`<Sts2Path>/mods/<ModName>/`。开了 §7.1 的 PckPacker 之后，**所有改动（含本地化）都只要
`dotnet build`，不需要 Publish**。

### 9.1 ⚠️ 改代码前必须先关游戏

运行中的 `SlayTheSpire2.exe` 会锁住 `mods/<ModName>/<ModName>.dll`，
拷贝 target 重试 10 次后以 `MSB3027` / `MSB3021` 失败。标准循环：

```powershell
Stop-Process -Name SlayTheSpire2 -Force -EA SilentlyContinue
dotnet build <ModName>.csproj
Start-Process "steam://rungameid/2868840"
```

### 9.2 验证靠日志，别靠点 UI

日志：`%APPDATA%\SlayTheSpire2\logs\godot.log`
（每次启动会把上一次的轮转成带时间戳的文件，`godot.log` 永远是当前这次）。

加载成功的标志：
```
[INFO] Loading assembly DLL ...\Rebalance_by_sts1_revival.dll
[INFO] Loading Godot PCK ...\Rebalance_by_sts1_revival.pck
[INFO] Calling initializer method of type ...MainFile for Rebalance_by_sts1_revival
[INFO] Finished mod initialization for 'Rebalance_by_sts1_revival'
[INFO]  --- RUNNING MODDED! --- Loaded 4 mods (4 total)
```

把断言写成 `MainFile.Logger.Info(...)` 再 grep，比点进卡牌图鉴快得多。
⚠️ **卡池和卡牌属性大量是惰性求值的**，主菜单不一定触发；要在 patch 里主动读一次
（例如读 `card.DynamicVars`）强制求值，否则日志里什么都不会有。

游戏内还有开发者控制台：`~` / `` ` `` / `*` / `'` 打开，`help` 看命令，
`card` 可以直接刷卡；`open logs` 开日志目录。
BaseLib 的 Mod Configuration 里可以开 "Open log window on startup"。

## 10. 上传创意工坊

用官方工具 [megacrit/sts2-mod-uploader](https://github.com/megacrit/sts2-mod-uploader)：

1. 跑 `ModUploader.exe` → 生成 `NewModWorkspace` 文件夹，重命名
2. mod 文件放进 `content/`，填 `workshop.json`，换掉 `image.png` 缩略图（**必须 <1MB**）
3. `ModUploader.exe upload -w <workspace-folder>`
4. 更新：改 `content/`，可在 `workshop.json` 加更新说明，重跑同一命令
   （mod id 从 `mod_id.txt` 读回）
5. 出错看 `mod-uploader.log`

---

## ✅ 实机确认清单（2026-07-26 全部完成）

- [x] manifest 文件名 = `<ModName>.json` —— 见 §8，日志实证
- [x] 游戏 API 版本 = `v0.107.1`（`release_info.json`）→ 若用 RitsuLib 选 `Compat.0.107.1`
- [x] BaseLib + RitsuLib 共存 —— **本项目不加 RitsuLib，见 §6**，该问题暂时不存在
- [x] 模板 csproj 已内置 BaseLib 引用 —— 是，但**版本必须钉到 3.3.6，见 §5.1**
- [x] `PublicizeSts` —— **不需要开**，三类改动都没用到 private 成员
- [x] 自动路径探测 —— 命中，零配置

新增确认项：
- [x] 不用装 MegaDot / Godot，PckPacker 就能出 .pck（§7.1）
- [x] 改代码前必须关游戏，否则 dll 被锁构建失败（§9.1）

## ✅ 原「最大技术风险」已解除：remove / modify 都能做

原判断是对的——`ModHelper.AddModelToPool` 只解决加卡，remove / modify 确实**没有现成 API**，
BaseLib 和 RitsuLib 都没有提供。但**用裸 Harmony patch 三类都跑通了**，
不需要 RitsuLib，也不需要 Publicize。

### 关键结构（反编译 `sts2.dll` 得到）

卡池基类 `MegaCrit.Sts2.Core.Models.CardPoolModel`：

```csharp
public virtual IEnumerable<CardModel> AllCards {
    get {
        if (_allCards == null) {
            _allCards = GenerateAllCards();                              // ← 基础游戏卡
            _allCards = ModHelper.ConcatModelsFromMods(this, _allCards)  // ← mod 卡追加在后面
                                 .ToArray();
        }
        return _allCards;                                                // ← 之后一直用缓存
    }
}
```

各角色卡池：`MegaCrit.Sts2.Core.Models.CardPools.{Ironclad,Silent,Defect,Necrobinder,Regent,
Colorless,Curse,Event,Status,Token,Quest,Deprecated}CardPool`。
注意命名：**储君 = `Regent`，亡灵契约师 = `Necrobinder`**。
每个池的 `GenerateAllCards()` 就是一个写死的 `new CardModel[N]{ ModelDb.Card<Bash>(), ... }`，
所以基础卡的类名可以直接从这里抄（如 邪眼=`EvilEye`、被遗忘的仪式=`ForgottenRitual`、
余烬=`Cinder`、踩踏=`Stomp`、欺凌=`Bully`、飞剑回旋镖=`SwordBoomerang`）。

### 三类改动的做法（均已实机验证，参考实现见 `Rebalance_by_sts1_revivalCode/`）

| 类型 | 切入点 | 验证结果 |
|---|---|---|
| **add** | `[Pool(typeof(XxxCardPool))]`（BaseLib 属性）标在卡类上 | `ColorlessCardPool has 65 cards; SpikeTestCard present = True` |
| **remove** | Harmony **postfix `XxxCardPool.GenerateAllCards`**，过滤掉不要的 | `IroncladCardPool: 87 -> 86 cards` |
| **modify（数值）** | Harmony **postfix 卡类的 `CanonicalVars` getter**，返回新的 DynamicVar | `ForgottenRitual block = 5, GainsBlock = True` |
| **modify（效果）** | Harmony **prefix 卡类的 `OnPlay`**，自造 Task + `return false` | `replaced OnPlay ran; granting 5 block` |

`GenerateAllCards` 是 remove 的最佳切入点：每个池只调用一次（结果被 `_allCards` 缓存），
且在 mod 卡追加之前，不会误伤别的 mod 加的卡。

### ✅ 「改效果」也验证过了，而且不用转译状态机

卡的行为写在 `protected override async Task OnPlay(...)` 里，是编译器生成的状态机，
**postfix 改不了**。但不需要动 BaseLib 那套 `AsyncMethodCall` 转译工具——因为
`CardModel.OnPlayWrapper` 里只有**一处** `await OnPlay(choiceContext, cardPlay);`，
所以 **prefix 里自己造一个 Task、`return false` 跳过原方法**，就整体换掉了效果：

```csharp
[HarmonyPatch(typeof(ForgottenRitual), "OnPlay")]
private static class Play
{
    [HarmonyPrefix]
    private static bool Prefix(CardModel __instance, CardPlay cardPlay, ref Task __result)
    {
        __result = NewOnPlay(__instance, cardPlay);
        return false;                       // 跳过原版
    }

    private static async Task NewOnPlay(CardModel card, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(card.Owner.Creature, "Cast", card.Owner.Character.CastAnimDelay);
        await CreatureCmd.GainBlock(card.Owner.Creature, card.DynamicVars.Block, cardPlay);
    }
}
```

hook / history / 附魔 / 消耗这些都在 `OnPlay` 外面，不受影响，照常跑。

### 改一张基础卡要动的 5 个成员（漏一个就崩或者文不对图）

以 被遗忘的仪式 为例（原：消耗过卡牌则获得3(4)费 → 新：获得5(8)格挡），
完整实现见 `Rebalance_by_sts1_revivalCode/Spike/ModifyBaseCardPatch.cs`：

| # | 成员 | 怎么改 | 不改会怎样 |
|---|---|---|---|
| 1 | `CanonicalVars` | postfix 换成新的 DynamicVar | 数值还是原版 |
| 2 | `OnUpgrade` | **prefix + return false** 整体换 | ⚠️ 原版会去动已经不存在的 `DynamicVars.Energy` → **抛异常** |
| 3 | `GainsBlock` | postfix 返回 true | UI 不显示格挡预览 |
| 4 | `OnPlay` | prefix + return false（见上） | 行为还是原版 |
| 5 | 描述文本 | 覆盖 loc 表里 `<Id.Entry>.description` 条目（见下） | 卡面还写着原效果 |

第 2 条是最容易漏的：换了 `CanonicalVars` 就**必须**同时换 `OnUpgrade`。

### ⚠️ 改描述别用 `DescriptionOverrides.CustomizeDescription`

直觉上该用 BaseLib 这个 event，但**它是错的钩子**。`CardModel.GetDescriptionForPile` 里：

```csharp
span[index] = description.GetFormattedText();   // ← 变量在这里就替换完了
List<string> list2 = list;                      // ← BaseLib 的 CustomizeDescription 插在这附近
```

它拿到的已经是**格式化完的成品字符串**，你在里面写 `{Block}` 不会被替换，
卡面上会原样显示 `{Block}`。那个 event 是用来给成品文本做微调的，不是用来重写描述的。

正确做法是改**数据源**：`CardModel.Description` 就是
`new LocString("cards", Id.Entry + ".description")`，
所以直接覆盖 loc 表里那个条目，后面 `DynamicVars.AddTo()` → `GetFormattedText()`
的正常流程就会把 `{Block}` 正确替换（升级值、预览染色也都自动对）：

```csharp
[HarmonyPatch(typeof(ModelDb), "Init")]        // 和 BaseLib 写 mod 卡文本同一个时机
private static class Loc
{
    [HarmonyPostfix]
    private static void Postfix()
    {
        var table = LocManager.Instance.GetTable("cards");
        var translations = (Dictionary<string, string>)AccessTools
            .Field(typeof(LocTable), "_translations").GetValue(table);

        string key = ModelDb.Card<ForgottenRitual>().Id.Entry + ".description";
        translations[key] = SimpleLoc.TrySimplify("#获得 {B} 点*格挡*。");
    }
}
```

基础卡的 `Id.Entry` 就是类名转 SCREAMING_SNAKE，如 `FORGOTTEN_RITUAL`
（不带 mod 前缀，mod 自己的卡才有）。不确定就先打一行日志看。
这里是直接写进已加载的表，绕过了 `LoadTable`，所以 SimpleLoc 不会自动跑，要显式
`SimpleLoc.TrySimplify()`。

### ⚠️⚠️ 本机游戏是中文，`localization/eng/` 一个人也伺候不了

`LocManager` 用**3 字母语言码**，简中是 **`zhs`**（映射表 `{"zh_Hans", "zhs"}`）。
本机 `LocManager.Instance.Language == "zhs"`。这有两个后果：

1. **mod 自己的卡**：文本要放 `<ModName>/localization/zhs/cards.json`。
   只有 `eng/` 的话，中文环境下 32 张新卡全是缺文本。**两个目录都要写。**
   （analyzer 的 STS001 只查 `eng/`，所以漏了 `zhs/` 编译期不会报错，只有实机才看得出来。）
2. **改基础卡描述**：上面那段覆盖的是「当前已加载语言」的表，
   所以要按 `LocManager.Instance.Language` 分语言给文本，
   否则中文界面上会冒出一行英文。

### ⚠️⚠️ 最坑的一条：patch 目标必须是「声明」该成员的类

`GainsBlock` 是 `public virtual bool` **声明在 `CardModel` 上**，`ForgottenRitual` 并没有 override。
写成 `[HarmonyPatch(typeof(ForgottenRitual), "GainsBlock", MethodType.Getter)]` →
Harmony 找不到目标 → 抛 `Undefined target method` → **整个 `harmony.PatchAll()` 中断**，
**排在它后面的 patch 类全部静默不生效**。

而且日志里照样会打 `Finished mod initialization for '<你的mod>'`，看起来一切正常。
真正的线索在上面几行：

```
[ERROR] Exception thrown when calling mod initializer of type ...MainFile:
  ---> HarmonyLib.HarmonyException: Patching exception in method null
  ---> System.ArgumentException: Undefined target method for patch method ...
```

正确写法是 patch 声明它的那个类，再用 `__instance is XxxCard` 判断：
```csharp
[HarmonyPatch(typeof(CardModel), "GainsBlock", MethodType.Getter)]
private static void Postfix(CardModel __instance, ref bool __result)
{
    if (__instance is ForgottenRitual) __result = true;
}
```

**每次改完启动都 grep 一次 `godot.log` 的 `[ERROR]`**，别只看 mod 有没有出现在列表里。
另外在 `MainFile.Initialize` 末尾把 `harmony.GetPatchedMethods()` 打一遍日志，
能直接确认每个 patch 是不是真的挂上了——这次就是靠它发现 `ForgottenRitual.OnPlay` 没挂上的。

> 这条**在同一个项目里已经踩过两次**（`GainsBlock`、`Rarity`），所以别指望「知道了就不会犯」。
> 判断方法很快：**反编译那张卡，看它自己的类体里有没有这个成员**。
> 基础卡类通常只写 `CanonicalVars` / `OnPlay` / `OnUpgrade` / `CanonicalKeywords` 这几样，
> 其余（`Rarity`、`GainsBlock`、`Pool`、`CanBeGeneratedInCombat`、`HasEnergyCostX` …）
> **全都声明在 `CardModel` 上**，一律要 patch `CardModel` + `__instance is XxxCard`。

### ⚠️ 升级态卡面文本：`{IfUpgraded:show:升级后|未升级}`，顺序是反的

要让卡面在升级后显示不同文本（如 X → X+1），用 SmartFormat 的 `show` 格式化器。
反编译 `ShowIfUpgradedFormatter` 确认的分支规则：

| `upgradeDisplay` | 取哪个分支 |
|---|---|
| `Upgraded` | **第一个** |
| `Normal` | **第二个** |
| `UpgradePreview` | 第一个，自动包一层 `[green]` |

**升级态在前、常态在后**，写反了会让未升级的卡显示升级后的数值。最多 2 个分支，超了抛 `LocException`。

```json
"...DOPPELGANGER.description": "下回合获得 {IfUpgraded:show:X+1|X} 点能量，抽 {IfUpgraded:show:X+1|X} 张牌。"
```

BaseLib 的 SimpleLoc 有简写 `-常态-+升级态+`（生成的就是上面这个形式），但当「升级态」文本里
**本身含字面 `+`** 时要转义成 `/+`，和那条正则的交互很容易写错。这种情况直接写最终 SmartFormat，
**并且整条不要加 `#` 前缀**（加了会走 SimpleLoc 处理）。

⚠️ 这个占位符只在卡牌**被显示**时求值，所以「启动无报错」不代表渲染正确，必须实际看卡面。

### 写卡速查（实现无色卡 11 张时确认的可复用套路）

**加费效果：永续用 `ModifyMaxEnergy`，一次性用 `EnergyNextTurnPower`**

```csharp
// 永续（狂暴、天人形态）——照抄基础卡薪火之源 PyrePower
public override decimal ModifyMaxEnergy(Player player, decimal amount)
    => player == Owner.Player ? amount + Amount : amount;
```

别拿 `AfterEnergyReset` + `PlayerCmd.GainEnergy` 做永续效果。两者都加得到费，但后者走
`Hook.ModifyEnergyGain`，会被 `NoEnergyGainPower` 之类「禁止获得能量」的效果拦掉，
而 `ModifyMaxEnergy` 不会——同一类效果不该一个被拦一个不被拦。
`EnergyNextTurnPower` 用 `AfterEnergyReset` 是对的，因为它本来就是**一次性**的，给完自我移除。
递增（天人形态）＝ 在 `AfterEnergyReset` 里给自己 `PowerCmd.Apply` 加 1 层。

**「恢复性」不是 CardKeyword，是 `CanBeGeneratedInCombat`**

```csharp
public override bool CanBeGeneratedInCombat => false;
```

`CardKeyword` 枚举只有 `None/Exhaust/Ethereal/Innate/Unplayable/Retain/Sly/Eternal`，没有恢复性。
真正的开关是 `CardModel` 的这个虚属性，`CardFactory.FilterForCombat` 按它过滤，
于是结茧/羽化/磁力/发现 等战斗内生成都不会产出此卡；卡牌奖励、商店、转化不受影响。
同一个过滤顺带排除了 `Ancient` 和 `Event` 稀有度——**先古卡天然不会被随机生成，不用另外处理**。

**把基础游戏的卡提升/挪进别的池**

塔2 里已存在、效果正好合用的卡，别重写一张（会变成两张并存），改现成的：

```csharp
// 1) 稀有度（注意 patch CardModel，不是那张卡）
[HarmonyPatch(typeof(CardModel), "Rarity", MethodType.Getter)]
static void Postfix(CardModel __instance, ref CardRarity __result)
{ if (__instance is Metamorphosis) __result = CardRarity.Rare; }

// 2) 入池：[Pool] 特性只能标自己的卡类，基础卡必须走这个 API
//    且必须在游戏初始化完成前调用
ModHelper.AddModelToPool(typeof(ColorlessCardPool), typeof(Metamorphosis));
```

⚠️ 卡进了两个池之后，`CardModel.Pool` 的默认实现是「扫 `ModelDb.AllCardPools` 取**第一个**
命中的池」并缓存，顺序不受控。而卡图路径是 `atlases/card_atlas.sprites/{Pool.Title}/xxx.tres`，
解析到错误的池就找不到图。**必须 patch `Pool` 钉死**在原来那个池上。

好消息：`EventCardPool` 和 `ColorlessCardPool` 都是 `card_frame_colorless` + `colorless` 能量图标
+ `IsColorless => true`，只有 `Title` 不同，所以钉住 Pool 没有任何视觉代价。

**事件给的卡不用单独处理**——事件发的就是 `ModelDb.Card<T>()` 那同一个模型，
改了模型属性，事件给出来的自然就是改后的版本，事件逻辑一行都不用动。

### 本地化（写卡时天天要用）

- 文本放 `<ModName>/localization/eng/cards.json`，**扁平的点号 key**：
  ```json
  {
    "REBALANCE_BY_STS1_REVIVAL-SPIKE_TEST_CARD.title": "Spike Test",
    "REBALANCE_BY_STS1_REVIVAL-SPIKE_TEST_CARD.description": "#Gain {B} Block."
  }
  ```
- key 前半是 `ModelId.Entry`：`<MOD_ID 全大写>-<类名转 SCREAMING_SNAKE>`；
  后半是 **`title` / `description`**（小写，不是 `Name`/`Description`）。
- **不用背这个规则**：`Alchyr.Sts2.ModAnalyzers` 会在编译期报 `STS001` 并**把正确的 key 直接打在错误信息里**。先写卡类、编译一次、照着报错填 json 最快。
- 开头的 `#` 打开 BaseLib 的 SimpleLoc 简写：`{B}`→`{Block}`、`{D}`→`{Damage}`、`{E}`→`{Energy}`，
  `*文字*` 金色高亮，`-旧-+新+` 升级前后替换。
- BaseLib 的 `CustomCardModel.Localization` 可以在 C# 里内联写文本，但
  **analyzer 看不见，照样报 STS001 编译失败**，所以还是走 json。
- 卡图缺失不会崩：模板的 `CardImagePath()` 会回落到占位图并打一行 INFO 日志。

---

## 参考链接

| 资源 | 链接 |
|---|---|
| **ModTemplate wiki（目前最好的文档，14 页）** | https://github.com/Alchyr/ModTemplate-StS2/wiki |
| BaseLib | https://github.com/Alchyr/BaseLib-StS2 |
| BaseLib wiki | https://alchyr.github.io/BaseLib-Wiki/ |
| RitsuLib | https://github.com/BAKAOLC/STS2-RitsuLib |
| RitsuLib 文档 | https://sts2-ritsulib.ritsukage.com/ |
| 官方上传工具 | https://github.com/megacrit/sts2-mod-uploader |
| 中文教程站（BaseLib + RitsuLib 都有） | https://tutorials.sts2modding.com/ |
| 官方 Discord `#sts2-modding` | https://discord.com/invite/slaythespire |
| Workshop | https://steamcommunity.com/app/2868840/workshop/ |

## 反编译（写卡时最主要的信息来源）

没有官方效果参考手册，基础卡怎么写的只能反编译看。命令行版 ILSpy 够用，不用装 GUI：

```powershell
dotnet tool install -g ilspycmd
$env:PATH = "$env:PATH;$env:USERPROFILE\.dotnet\tools"
$dll = 'C:\Program Files (x86)\Steam\steamapps\common\Slay the Spire 2\data_sts2_windows_x86_64\sts2.dll'

# 找类型（比如所有卡池）
ilspycmd -l class $dll | Select-String 'CardPool'

# 反编译单个类型到当前目录
ilspycmd -t MegaCrit.Sts2.Core.Models.Cards.EvilEye $dll -o .
```

同样的办法也能读 BaseLib，用来查它到底提供了什么：
```
C:\Program Files (x86)\Steam\steamapps\workshop\content\2868840\3737335127\BaseLib\BaseLib.dll
```

常用起点：
- `MegaCrit.Sts2.Core.Models.CardPools.*` —— 每个池有哪些卡、卡类名叫什么
- `MegaCrit.Sts2.Core.Models.Cards.*` —— 单张卡的费用/类型/稀有度/数值/效果
- `MegaCrit.Sts2.Core.Commands.*` —— `CreatureCmd.GainBlock`、`PlayerCmd.GainEnergy` 等效果原语
- `MegaCrit.Sts2.Core.Localization.DynamicVars.*` —— `DamageVar` / `BlockVar` / `EnergyVar`
- `MegaCrit.Sts2.Core.Modding.ModHelper` —— 官方给 mod 的挂载点（就那么几个）

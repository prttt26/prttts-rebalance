# prttt's Rebalance — Energy & Deck Cycling

首个测试版本为 v0.1.0。展示名称已更新，内部 ID 保持 `Rebalance_by_sts1_revival`。

在仓库根目录执行：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File ./workshop/Prepare-Release.ps1
```

脚本使用已还原的 NuGet 包，生成 Release DLL、PCK 和 manifest，并填入中英工坊简介。输出在 `publish/prttts-rebalance/`，不会复制到游戏安装目录。如果本机尚未还原依赖，先执行 `dotnet restore ./Rebalance_by_sts1_revival.csproj`。

发布前用本次 Release 包在游戏中确认加载、五个角色卡池和中英文本；编译成功不能代替实机验证。manifest 的最低游戏版本是 0.107.0，BaseLib 最低版本是 3.3.6，尚未重新验证所有后续游戏版本。

检查 `workshop.json` 和 `image.png`。当前可见性设为 public，上传后会公开。封面是简洁文字封面，可替换。

使用官方 ModUploader，在其所在目录执行（把路径替换为实际发布目录）：

```powershell
.\ModUploader.exe upload -w "C:\path\to\prttts-rebalance\publish\prttts-rebalance"
```

Steam 需要运行并登录对应账号。保留上传器生成的 `mod_id.txt`；以后重新打包到同一目录并上传，即更新同一工坊条目。打包脚本不会删除该文件。

官方工具及格式说明：https://github.com/megacrit/sts2-mod-uploader

v0.1.0 已于 2026-10-13 通过官方上传器上传：[创意工坊条目](https://steamcommunity.com/sharedfiles/filedetails/?id=3819462077)。上传器返回成功并添加 BaseLib 依赖；本次未完成发布包实机复测。`description.txt` 是工坊简介的来源，修改后重新运行打包脚本。

工坊条目 ID 为 `3819462077`。新克隆仓库首次准备更新时，把该数字写入生成的官方工作区 `mod_id.txt`，再运行上传器，以更新已有条目。

## 备用：游戏内 Workshop Uploader

主流程继续使用官方上传器；两种方式都上传本机编译的文件。游戏内上传 Mod 提供发布界面，不负责替本项目编译。

打包脚本同时生成 `publish/in-game-uploader/Rebalance_by_sts1_revival/`，其中只有同一份 Release DLL、PCK 和 manifest。关闭游戏后，把这个文件夹复制到游戏安装目录的 `mods/` 下；已有同名文件夹时只覆盖这三个文件，保留上传记录。然后启动游戏，确认本地 Mod 加载正常。

订阅并启用 [Workshop Uploader](https://steamcommunity.com/sharedfiles/filedetails/?id=3748678645) 及其依赖 RitsuLib，在游戏主菜单的 Mod 设置附近打开上传界面，选择本地 `prttt's Rebalance`。RitsuLib 是上传工具的依赖，本 Mod 的玩家依赖仍然只有 BaseLib。

填写或选择以下材料：

| 字段 | 内容或来源 |
| --- | --- |
| 标题 | `prttt's Rebalance` |
| 简介 | `workshop/description.txt`，可直接粘贴中英合并文本 |
| 主预览图 | `workshop/image.png` |
| 必需工坊项目 | BaseLib，工坊 ID `3737335127` |
| 可见性 | public（公开） |
| 更新说明 | `v0.1.0 — Initial testing release. Energy & Deck Cycling.` |
| 上传内容 | 选择内容与元数据一起上传；确认包含 DLL、PCK、JSON |

官方 `workshop.json` 不假定能被此 Mod 直接导入，以上字段按界面填写。其游戏版本范围指 Steam 分支范围，与 manifest 的 `min_game_version` 数字不同；按实际测试分支选择，不要把 `0.107.0` 填作分支名。

如果已经通过官方工具发布，先把同一本地 Mod **绑定到已有工坊条目**：条目 ID 从官方发布目录的 `mod_id.txt` 或工坊链接取得，再上传更新，避免另建重复条目。如果先使用游戏内工具发布，切回官方工具前，把同一条目 ID 写入官方发布目录的 `mod_id.txt`。

游戏内工具会在本地 Mod 文件夹的 `.sts2-workshop-uploader/` 保存上传记录，默认不上传这些记录。本仓库已忽略该目录；保留本机记录便于后续更新。两个工具的记录独立维护，切换工具后要核对标题、简介、依赖和图片。

工具行为来源：[作者 README](https://github.com/BAKAOLC/STS2-WorkshopUploaderMod)。目前只准备备用目录和说明，未安装上传 Mod、复制到游戏目录或上传。

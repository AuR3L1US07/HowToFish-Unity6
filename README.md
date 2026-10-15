# How to Fish · Unity 6 易读恢复版

恢复游戏资源与 C# 实现，整理为可继续研究、修改的 Unity 工程，并提供 **Windows 64 位本地游玩版**。

**[下载可玩版本](https://github.com/AuR3L1US07/HowToFish-Unity6/releases/latest)** · [整理说明](Documentation/Readability/manual-changes.md) · [验收记录](Documentation/OfflineRecoveryAcceptance.md) · [分享说明](SHARING.md)

## 游戏画面

以下图片来自本仓库 Windows 构建的实际运行画面。

![主菜单](Documentation/screenshots/main-menu.png)

![本地游戏](Documentation/screenshots/local-gameplay.png)

## 下载与游玩

1. 在 [Releases](https://github.com/AuR3L1US07/HowToFish-Unity6/releases/latest) 下载 `HowToFish-Readable-v1.1.3-Windows-x64.zip`。
2. 将压缩包**完整解压**到可写入的文件夹，保留 EXE 旁边的所有文件和目录。
3. 运行 `HowToFish.exe`，通过“创建房间”进入本地世界。无需通过 Steam 启动。

当前发布范围为本地游玩；Steam 联机不在本版验收范围。更新前请备份自己的存档。

## 用 Unity 打开

使用 **Unity 6000.4.4f1**（安装 Windows 构建支持）。克隆仓库，在 Unity Hub 中添加仓库根目录，等待首次导入与编译。打开 [Game 场景](Assets/Scenes/Game.unity)，进入 Play 模式。

保留 Assets 下的 `.meta` 文件以及 Packages 中的本地依赖。缓存、构建产物和个人存档未提交到仓库。

## 阅读 C# 代码

主要游戏逻辑位于 [Assets/Scripts/Assembly-CSharp](Assets/Scripts/Assembly-CSharp)。

| 功能 | 阅读入口 |
| --- | --- |
| 抛竿、收线和鱼线状态 | [FishingRod](Assets/Scripts/Assembly-CSharp/FishingRod.cs)、[FishingRodCast](Assets/Scripts/Assembly-CSharp/FishingRodCast.cs) |
| 咬钩与鱼类权重 | [Bait](Assets/Scripts/Assembly-CSharp/Bait.cs)、[CreatureManager](Assets/Scripts/Assembly-CSharp/CreatureManager.cs) |
| 玩家移动与拾取 | [PlayerMovement](Assets/Scripts/Assembly-CSharp/PlayerMovement.cs)、[PlayerHolding](Assets/Scripts/Assembly-CSharp/PlayerHolding.cs) |
| 物品栏 | [PlayerInventory](Assets/Scripts/Assembly-CSharp/PlayerInventory.cs)、[InventorySlot](Assets/Scripts/Assembly-CSharp/InventorySlot.cs) |
| 存档与恢复 | [SaveManager](Assets/Scripts/Assembly-CSharp/SaveManager.cs)、[SaveSystem](Assets/Scripts/Assembly-CSharp/SaveSystem.cs) |
| 本地启动 | [LocalPlayMode](Assets/Scripts/Assembly-CSharp/LocalPlayMode.cs)、[ConnectionManager](Assets/Scripts/Assembly-CSharp/ConnectionManager.cs) |
| 生命和饱食度 UI | [VitalsUI](Assets/Scripts/Assembly-CSharp/VitalsUI.cs)、[PlayerUI](Assets/Scripts/Assembly-CSharp/PlayerUI.cs) |

本轮整理涉及 98 个文件，包含语义绑定的局部变量重命名、256 处目标类型 `new()`、存档分支简化和关键逻辑注释。对 366 个脚本、10,191 个声明及脚本 GUID 的核对通过。类名、序列化字段、方法签名与 FishNet 生成的辅助代码保持兼容；不能确认语义的临时变量保留原名。

详见 [修改清单](Documentation/Readability/changes.json)、[人工整理说明](Documentation/Readability/manual-changes.md) 和 [发布验证](Documentation/Readability/ReleaseValidation)。

## 验证与恢复范围

Windows 正式构建包含 12 个启用场景，构建错误为 0。发布前对独立 EXE 检查新建本地世界、玩家生成、存档写入与重启读档；验收结果随仓库保留。历史功能与水面修复记录见 [Documentation](Documentation)。自动检查不代表完整正常难度通关，也不保证视觉逐像素等同原版。

这是从发布产物恢复并进一步整理的工程，**不是原作者的原始源码**。原游戏与第三方组件权利归各自权利人所有，保留第三方许可；本仓库未附加统一开源许可证。详见 [SHARING.md](SHARING.md)。

# 非 Steam 恢复验证记录（2026-10-14）

## 工程位置与范围

原工程：`E:\分析howtofish\HowToFish-Unity6`。用户另一个任务在此工程运行 Unity，未中断该进程。

隔离验证工程：`E:\分析howtofish\offline-validation-workspace`。官方包与第三方程序集源码替换在此实施，并使用独立公司/产品设置、存档目录与验证端口。该目录不是开发者原始仓库。

## 已运行通过

| 检查 | 实际结果 | 证据（隔离工程 CompilationChecks） |
| --- | --- | --- |
| 离线启动、移动、服务器和客户端、存档读回 | 180 秒综合会话完成，运行错误列表为空 | local-game-validation.json / offline-retest.log |
| 钓竿、库存、实际物品售卖 | 装备、真实鼠标抛竿/收线、切换/存放/丢弃、库存保存、实体售卖金额断言通过 | fishing-input-probe.json |
| 捕鱼核心链 | 原系统自然生成钩鱼，真实鼠标收线、原伤害流程击杀、合法拾取、入库和保存通过 | fishing-acceptance.json |
| NPC 任务交付 | 物品接收与消耗、原奖励交互、船钥匙解锁及磁盘存档通过 | npc-delivery-probe.json |
| 马达付费升级与驾驶 | 原第二岛 $230 马达购买，扣款 1230→1000，等级 0→1；驾驶前进、转向、倒车、退出通过 | boat-driving-probe.json |
| 伤害、武器与生命循环 | 非致死伤害同步；真实鼠标开火生成弹体，10→9 发，真实换弹恢复 10 发；死亡、尸体、原鼠标放弃流程复活至 50 HP | combat-probe.json / weapon-life-audio-probe.json |
| 换装与保存 | 配色更改与隔离本地存档读回，升级与库存保存通过 | customization-probe.json |
| 音频 | Master/FX 混音设置读回，原音效源播放状态通过；未以此宣称听感一致 | weapon-life-audio-probe.json |
| VFX 与配色 Shader | 11 个事件粒子与世界坐标通过，连续事件不覆盖；4 个配色区域实际 GPU 像素差异通过；检查的恢复 Shader 无编译错误 | visual-behavior-validation.json |
| Boss 与岛屿进度 | 四个原 Boss（Giant Piranha、Pufferfish、Albatross、Mutated Bowhead Whale）真实伤害（非致死 LocalHit 后同步）、死亡动画、原奖杯生成/拾取/交付/消耗、原任务奖励、岛屿解锁（2→3→4→5）与磁盘存档均通过 | boss-progression-probe.json（2026-10-14 21:45 重跑） |
| 最终结局 | 原结局交互后 FinalBossKilled、权威与客户端 HasFinishedGame、制作名单 UI 与特效、HasFinishedGame 存档读回全部通过 | boss-progression-probe.json |
| 独立 Windows 构建 | 修复 Addressables 运行时数据与随原版发布的 StreamingAssets catalog 冲突后构建成功，0 错误 | offline-build-validation.json |
| 独立程序启动与新进程存档重载 | 构建 exe 实际启动（图形模式，退出码 0、错误列表为空）：新进程加载既有 RecoveryStandaloneValidation 世界，金额/欧元/MaxIsland 状态一致，本地服务器与客户端启动，存档写回读回通过 | standalone-reload-graphics-validation.json |

捕鱼测试使用原饵的运行时副本缩短等待，并在咬钩前摆放水下测试点；没有直接设置捕获成功。船测试在专用世界准备预算并加载原第二岛；没有直接设置购买成功。详细干预均保存在各报告中。

## 已修复的实质错误

- Boss 探针最初按 `TotalItems == 1` 筛选任务 NPC，而原最终鲸鱼任务 `I5 Whale Quest` 声明 `TotalItems = 0`、`_type: 6 = FinalBoss`。原 NPC 流程收到奖杯后仍会完成任务。探针已改为 `TotalItems <= 1` 并接受 FinalBoss 类型，重跑后四 Boss 与结局全部通过；未修改任务资产或进度。

- Piranha、Manta、BowheadWhale、Spidercrab 的反编译祖先 `Awake` 调用错误地重新虚分派，导致递归。已依据原 DLL 的非虚 `call` 语义，以非虚辅助方法准确恢复祖先调用；原工程和隔离工程均修复。CIL 证据：`E:\分析howtofish\tools\ancestor-nonvirtual-awake-evidence.json`。
- 本地化表的导出 meta GUID 与原 TableCollectionNameGuid 不同。隔离工程已对齐 15 个表身份，保留原查表标识，并更新 255 个引用文件。
- 本地化设置启动顺序、临时实例及自定义数据库构造方式已修复，最新综合会话未出现原地址加载与空引用错误。
- 粒子 Shader meta GUID 格式、首次粒子播放/发射顺序、Grass/URPDecal 中 HLSL 保留标识符已修复。
- 验证启动会先等待编辑器启动回调，避免进入 Play 后才首次创建 Search 索引。[Unity 原始实现](https://github.com/Unity-Technologies/UnityCsReference/blob/master/Modules/QuickSearch/Editor/Indexing/SearchDatabase.cs) 在 Play 阶段不会创建该默认索引。
- 独立构建时 Addressables 玩家构建回调注入的重建运行时数据（catalog.bin、monoscripts/locale bundle）与随原版发布的 StreamingAssets/aa 旧文件在 Bee 构建程序层冲突。RecoveryBuildValidation 现在构建前将这些旧文件备份到 CompilationChecks/streaming-aa-backup，让回调复制与恢复后程序集一致的新集合；原发布文件保留可回滚。

## 源码依赖替换（隔离工程）

官方源码包：Addressables、Localization、Animation Rigging、AI Navigation、Transport、VFX Graph 及所需依赖。来源与校验保存在 `tools/OfficialPackages`，这些是兼容替换版本，未证明每个包就是原开发者版本。

恢复源码程序集：GameKit.Dependencies、FishyUnityTransport、Unified.Universal.Blur、FishNet.Runtime、FishNet.CodeAnalysis、Concentus、Adrenak.RNNoise4Unity.Runtime。Unity 已编译并在综合会话使用。原 DLL 与资源引用均保留备份。未引入新 FishNet CodeGen/ILPP，保留原生成 RPC 逻辑。

Unity 引擎/Mono、部分官方 NuGet 包及语音原生插件仍会包含 DLL；不能把这些正常运行条件称为游戏 C# 未恢复，也不能宣称原生实现已恢复成 C#。

## 画面诊断

此前贴满镜头的几何已由前向射线定位为岛屿静态残骸 `IslandHolder/Meshes/Island1/SliceOfBice`，不是玩家身体或玩家船。测试移动到残骸旁后面向它；相机 FOV 为 74，脚底与地面高度匹配。没有为了改善截图而隐藏它或抬高游戏相机。

原 DXBC 已支持人物配色、透明玻璃、水面双波与颜色、旗帜/草风、遮罩及其他自定义材质的重建。VFX 外观及部分照明、反射仍有明确重建部分，GPU 编译与参数测试不能证明原版视觉完全等价。

## 尚待验收

Boss 死亡奖励、奖杯交付、岛屿进度、最终结局、独立 Windows 构建、实际启动与新进程存档重载已于本轮补测通过。Boss 测试使用高伤害测试条件，不代表实战难度验收；独立构建使用重建的 Addressables 运行时数据，未证明与原发布内容逐字节一致。本轮结论不能宣称原版视觉完全等价，Steam 联机不在范围。

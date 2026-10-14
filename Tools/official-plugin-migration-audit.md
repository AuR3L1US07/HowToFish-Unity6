# 官方插件与二进制依赖审计

## 已实施

Visual Effect Graph 已用本机 Unity 6000.4.4f1 内置的官方 17.4.0 源码包替换 Unity.VisualEffectGraph.Runtime.dll，保持现有 URP/Core/Shader Graph 17.4.0 同一系列版本。备份目录为 ReferenceOnly/BeforeOfficialPluginMigration/com.unity.visualeffectgraph。原 DLL 和原 meta 均保留，未将反编译副本加入编译。

迁移前找到 35 个序列化脚本类型映射，无无法映射的实际引用。唯一改写资源为 Assets/MonoBehaviour/RuntimeResources.asset，m_Script 替换为官方 RuntimeResources.cs 的 GUID 8b3fd20ed9033b241b5235f00e27c333 和 fileID 11500000。原 DLL GUID 为 8b25d78d443c80b6b3b8c89d3a3cdab7。具体计划保存在备份目录 plan.json。

现存 Unity DLL 程序集身份保存在 Tools/original-unity-assembly-identities.json：目标五包均为版本 0.0.0.0、无强名称。这个版本是程序集版本，不能据此推断原始 UPM 包版本。VFX 官方 asmdef 保持相同程序集名；兼容性仍须编辑器编译和运行确认。此次迁移没有恢复被禁用的 VFX 组件或丢失 VFX Graph 资源。

## 官方源码缺口

Addressables、Localization、Animation Rigging、AI Navigation 的官方源码包不在本机 Library/PackageCache、tools/OfficialPackages 或编辑器内置包中。反编译 AssemblyInfo 仅提供 0.0.0.0，未发现可信原始包版本证据。官方 registry 可查询候选版本、依赖和带校验摘要的下载位置，但不能把最新版本当作原游戏版本自动迁移。

官方来源：

- https://packages.unity.com/com.unity.addressables
- https://packages.unity.com/com.unity.localization
- https://packages.unity.com/com.unity.animation.rigging
- https://packages.unity.com/com.unity.ai.navigation

下一步应从原始 Addressables catalog/构建日志或 DLL API 指纹比对建立具体版本候选，获取源码后在隔离验证中对比公开 API、序列化字段和运行行为，再迁移。未实施这些下载或替换。

## 依赖分类

- 游戏主程序集：已由工程恢复源码编译，属于项目必须维护和验证的部分。
- Unity 官方托管插件：Addressables/ResourceManager、Localization、Rigging、Navigation、Services、Transport、Profiling、ScriptableBuildPipeline 等。可寻找官方包源码；当前保留部分 DLL 不等于游戏玩法代码未恢复，但尚未达到所有非 Steam 托管插件源码替换。
- 非 Steam 第三方托管插件：FishNet.Runtime/CodeAnalysis、FishyUnityTransport、GameKit.Dependencies、Concentus、Adrenak.RNNoise4Unity.Runtime、Newtonsoft.Json、System.IO.Hashing、System.Runtime.CompilerServices.Unsafe、Unified.Universal.Blur。各自承担联网框架、语音编解码/降噪、JSON、框架兼容或模糊效果。需按功能路径判断是否删除或源码替换，不可批量删去导致重复/缺失类型。
- Steam 托管插件：com.rlabrecque.steamworks.net、Heathen.Steamworks；Heathen.Core 是公共依赖，不能只因名称关联而直接删去。Steam 功能可不验收，但要保证本地模式没有运行时强制调用。
- 非 Steam 原生插件：x86_64/rnnoise.dll、meta_voice_chat_neteq.dll、libonigwrap.dll。托管反编译不涵盖原生实现，需分别追踪调用；本地玩法不应强制依赖语音或在线服务初始化。
- Steam 原生插件：x86_64/steam_api64.dll，可依本地模式是否隔离 Steam 调用判断是否从发行构建排除。
- Unity 引擎与运行时：UnityPlayer、引擎模块、Mono/工具链依赖属于正常引擎运行条件，不应承诺无任何 DLL 的 Unity 可执行程序。

## 工具

Tools/migrate-official-plugins.mjs 默认只生成计划。实施必须显式传入 --apply 和 --package。工具保留所有包源码 meta，检查序列化引用唯一映射和依赖最低版本，遇到无法映射引用或非预期程序集版本/强名称拒绝迁移，Unity 正在运行时拒绝实施。同一备份目录已存在时也拒绝重复实施。

目前外部 Unity 进程占用工程，后续插件变更和编辑器启动已停止。需要主线程解决占用后验证本次迁移。

# How to Fish — Unity 6 恢复工程

在 Unity Hub 中选择“添加现有项目”，添加这个文件夹本身。目标编辑器为 **Unity 6000.4.4f1（Windows x64）**。不要选择 ReferenceOnly，也不要用正在安装的 2022.3.62f3c1 打开或降级工程。

## 已整理到一个文件夹

- Assets：场景、模型、贴图、材质、动画、游戏脚本、托管及原生插件。
- Packages、ProjectSettings：Unity 工程配置，保留原版编辑器版本与场景列表。
- ReferenceOnly/recovered-code：全部 178 个程序集的反编译源码，供阅读，不自动加入 Unity 编译。
- ReferenceOnly/original-artifacts：原始游戏的逐文件校验一致副本。
- ReferenceOnly/ExcludedEngineAssemblies：避免与编辑器重复的引擎 DLL，以及原游戏的 Burst AOT 缓存。
- Documentation：已有分析报告、人物/换装资源核对及完整性清单。
- CompilationChecks：独立 C# 编译日志和检查产物，不属于可导入插件。
- Tools：可重复执行的检查工具。

## 为编译做的整理

保持 Assembly-CSharp 和 Assembly-CSharp-firstpass 的原程序集结构；firstpass 脚本仍在 Assets/Plugins 下。依赖使用原游戏 DLL，不将第三方反编译源码再复制进 Assets，也不重复安装同名 Unity 包。

修正托管插件的导入设置，启用编辑器使用；为原生插件限定 Windows x64。把 8 个引擎内置模块移出 Assets，保留 UnityEngine.UI.dll。原始 lib_burst_generated.dll 是玩家构建的 AOT 产物，外部保留，不当作编辑器缓存加载。修改详情见 Documentation/unity-importer-changes.json。

## 验证结果与边界

使用 Microsoft C# 编译器 4.12、C# 9、Unity 编辑器相关编译标志、原版 Unity 6 引擎/框架引用和本工程插件，分别重编译两个游戏程序集：

| 程序集 | 错误 | 警告 |
|---|---:|---:|
| Assembly-CSharp-firstpass | 0 | 3 |
| Assembly-CSharp | 0 | 1581 |

这是独立 C# 编译验证，尚不是 Unity 编辑器内导入/构建成功的证明。警告包括序列化字段未显式赋值、未使用字段，以及 FishNet 已生成序列化类型同名冲突；没有为了隐藏问题而统一关闭警告。保留原版玩法代码。

2026-10-13 已使用 E:/Unity/6000.4.4f1/Editor/Unity.exe 完成真实编辑器编译及主场景加载验证。最终检查没有 C# 编译错误，也没有场景加载异常；URP 全局设置和管线创建成功，Assets/Scenes/Game.unity 的丢失脚本数为 0。结果见 CompilationChecks/render-runtime-validation.json，完整日志为 render-final-validation.log。尚未验证进入 Play 模式、完整玩法或打包发布。

本次修复将游戏运行用的裁剪渲染 DLL 替换为同编辑器自带的完整 URP/Core/Shader Graph 17.4.0 和依赖，迁移了 161 个资源文件的脚本引用。替换前文件在 ReferenceOnly/BeforeRenderPackageRepair 中保留。补齐了本地化设置的编辑器注册及预加载配置。Unity 自带 Shader Graph 的三处源码补充了 UnityEngine 命名空间引用，以解决 GUID 类型无法解析的问题。

## 已知画面限制

已用官方同名源码恢复 45 个标准 shader，并移出 5 个有对应官方源码的计算着色器导出文件。相关报告在 CompilationChecks/official-shader-recovery.json 和 official-compute-recovery.json，替换前文件保留于 ReferenceOnly。人物等自定义 shader 仍为 AssetRipper 的替代输出，人物 shader 当前恒输出白色。模型、骨骼、服装和贴图引用检查完整不代表画面等价；完整玩法与画面尚未验收。完整情况见 Documentation/analysis/人物资源核对.md。

## 重复检查

Tools/Check-CSharp.ps1 使用已安装的 .NET 8 运行时和保留的原版运行库完成独立 C# 基线检查。它无需 .NET SDK，不能代替当前完整包工程的 Unity 验证；当前验收以编辑器日志为准。

Tools/Validate-Unity6.ps1 的参数 UnityEditorPath 指向你安装好的 Unity 6000.4.4f1/Editor/Unity.exe；日志写入 CompilationChecks/unity-editor-import.log。首次导入会生成 Library 等缓存。

插件导入格式参考 Unity 官方项目示例：https://github.com/Unity-Technologies/VHACD-deprecated/blob/main/libvhacd.dll.meta 。报告中的历史绝对路径仅用于说明分析来源；Unity 工程代码和资源不依赖原 Steam 安装路径。

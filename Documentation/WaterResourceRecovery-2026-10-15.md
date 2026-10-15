# 原版水面资源恢复（2026-10-15）

## 查明的问题

原版 `Render Water_0` 使用 RenderObjects 材质覆盖，在事件 450（BeforeRenderingTransparents）绘制水面。它仍指向工程的 `Water.mat`，因此即使水面 Renderer 已切换成原版 Shader，这个绘制通道也会再次使用近似 Shader。仅检查 Renderer.sharedMaterial 或 Shader.isSupported 无法发现这一点。

WaterManager 现在同时将实际水面覆盖通道切换到包含原版 Shader、原法线和原材质参数的运行时材质，并调用 RenderObjects.Create 重建内部缓存的绘制通道。保留原事件、模板、深度、LayerMask 和 overrideMode；不更改 overrideMode=None 的其他通道。后续更换画质时会检查新加载的渲染资源，退出场景时恢复原资源引用。

## 原始资源依据

- `original-artifacts/How to Fish_Data/globalgamemanagers.assets`：原 Water 材质 pathID 5、WaterShader pathID 54、法线纹理 pathID 29。
- `sharedassets0.assets`：水面网格 pathID 172；原九块水面的材质、网格、位置和缩放与场景导出记录匹配。
- 原材质仅法线纹理非空，其他三个 lightmap/shadowmask 纹理原本为空，未发现额外丢失的水面纹理。
- 原场景雾和环境颜色与导出配置一致，没有通过猜测修改颜色或光照。
- 已逐字节核对运行 bundle：水面编译程序、网格顶点数据、网格索引和法线像素数据的 SHA-256 均与原始资源一致，见 `WaterValidation-2026-10-15/resource-provenance.json`。
- 详细资源与二进制配置审查：`tools/water-original-audit-report.md` 及其关联 JSON。

## 验证与边界

主工程 Direct3D 12 实际运行验证已通过：原版 Shader、原法线、原网格及事件 450 的内部缓存材质一致。960×540 同帧对比中，153295 个像素的 RGB 合计差异超过 9；修复前明显的长条纹消失，恢复原版编译程序的水面细节。65 秒离线验证同时通过，错误列表为空。证据与图片副本位于 `Documentation/WaterValidation-2026-10-15`。

更新后的 Windows 构建成功，0 错误、274 警告（没有关闭警告）。游戏程序集已包含此次水面通道修复。构建报告副本位于同一验证目录。

更新的独立程序已实际启动，本地玩家、服务器、客户端和存档读回均通过，退出码 0，运行错误列表为空；见 `water-standalone-validation.json`。

`RecoveryWaterRenderingValidation.Run` 检查实际运行的原版 Shader、法线、网格，以及 RenderObjects 内部缓存通道使用的最终材质。它还在相同帧、相同测试相机下分别模拟修复前的材质覆盖与修复后的覆盖，输出 `CompilationChecks/water-before.png` 和 `water-restored.png`。这两个文件是同一恢复工程的前后对比，不是原版游戏实拍对比。

原版编译 Shader 可以从 bundle 加载，但本次尝试将其克隆保存为独立原生 Shader 资产后，Unity 没有保留其可用编译程序。试验资产已移除，不影响主工程。当前原版效果仍在 Play/独立程序中由原始 bundle 提供；编辑状态的可编辑水面 Shader 仍是近似实现，不应宣称为原始 Shader Graph。

未证明所有时间、天气、画质和视角下与原版逐像素一致；原场景环境球谐缓存差异仍只是一项待对比线索，未据此调整场景。

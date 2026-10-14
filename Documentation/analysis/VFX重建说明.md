# 非 Steam 单机特效重建

本轮补回此前临时禁用的 VFX 事件的可见粒子行为。不是原版 Shader Graph / VFX Graph 源码恢复，也未证明视觉等价。

## 原版证据与实现范围

`SceneManager.prefab` 的 VFX holder 包含 WaterSplash、WaterExplosion、LavaExplosion、VolcanoEruption、LavaBarf、Poison、Angry、BossDeath、SmallBloodExplosion、DustExplosion，共 10 个事件。原 VFXManager 的接口写入 Position、Direction 并发送 Play。导出的 VisualEffectAsset 含编译时表达式和资源引用，缺少可供编辑器重新生成 GPU 程序的原始 graph。之前在 Game 场景和 SceneManager 预制体中禁用的 20 个 VisualEffect 组件继续保持禁用。

`RecoveryVfxParticles` 在收到上述事件时创建并缓存独立 ParticleSystem，手动发射粒子到传入的世界位置，按传入方向设置速度；零方向时使用事件对应的向上或全向扩散。每种事件共享一个粒子系统，世界空间粒子允许相邻事件并存，不会因下一次移动 emitter 而把前一批粒子移走。控制器销毁时清理缓存和材质。不使用 Unity 全局随机数，避免影响原游戏随机逻辑。血液关闭选项同时控制 SmallBloodExplosion。

BossDeath 在导出资源中对应 PufferfishDeath 资产；兼容实现还提供 PufferfishDeath 别名。实际调用使用 BossDeath。原版 Direction 传入形式不完全一致：有的调用传方向向量，ProjectileManager 传欧拉角。实现保留接口参数并将非零值归一化作为粒子速度轴；这不能证明与原 graph 对 Direction 的解释完全一致，需要对照原版特效验证。

粒子的颜色、数量、尺寸、速度、生命周期和软圆形外观为重建参数，非从原版 GPU 程序还原。没有恢复原版网格输出、多阶段发射、碰撞、噪声和纹理动画。原来正常启用且有 asset 的 VisualEffect 仍走原接口。本轮补回的是此前完全没有显示的事件反馈，仍需要 Unity Play Mode、玩家构建和原版画面对照。

## 验证工具

`Tools/Audit-VisualCompleteness.ps1` 输出 `CompilationChecks/visual-completeness-audit.json`，区分导出占位 shader 与带标记的近似重建，并列出全部 VisualEffect 的启用状态。静态审计不等于编译或运行验证。源码独立编译结果记录另见 `CompilationChecks/vfx-recovery-check.rsp` 与本轮执行输出。

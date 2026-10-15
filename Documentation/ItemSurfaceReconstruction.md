# 物品表面源码补全

`DefaultShader`、`BoatShader`、`CookableGlassShader` 的可编辑 Shader 源码原先只读取调色板，虽然声明了烹饪和皮肤属性，修改这些属性不会影响画面。现已接入表面计算。

- DefaultShader：生肉→熟食→烧焦两阶段颜色；皮肤开关、调色偏移、彩虹开关；梯度噪声、棋盘、Voronoi 三种图案；法线及金属/光滑度属性。
- BoatShader：相同的皮肤与材质表面计算，无烹饪属性。
- CookableGlassShader：两阶段烹饪颜色、法线与距离衰减。保留原始片段程序的 alpha=1；不能因为名字含 Glass 就改成透明材质。

证据来自 `CompilationChecks/defaultshader-surface-evidence`、`boatshader-surface-evidence`、`cookableglassshader-surface-evidence` 的原版 DXBC 反汇编，以及 `tools/inspect_item_shader_programs.py`。Default/Boat 的顶点和片段程序表明 UV0 为主调色坐标、UV1 为材质遮罩、UV2 为次调色坐标、UV3 为皮肤及法线坐标；CookableGlass 的法线坐标为 UV1。

这些文件是依据程序行为重建的源码，并非原始 Shader Graph 工程。当前光照是重建的 URP 光照，未证明与原版的完整光照、反射和变体逐像素等价。运行时若启用了原版编译 Shader bundle，仍可使用其原始编译程序。

`RecoveryItemSurfaceValidation.Run` 直接通过 AssetDatabase 读取这三份源码 Shader，避开同名原版 bundle 的替换。GPU 检查覆盖两份烹饪 Shader 的两阶段颜色变化、两份皮肤 Shader 的开关，以及每份三种图案的两两差异；报告为 `CompilationChecks/item-surface-validation.json`。此检查验证这些属性实际影响 GPU 输出，不代表完整视觉等价。

2026-10-15 已在独立验证工程以 Direct3D 11 运行该入口，Unity 正常退出（0），两份烹饪 Shader、两份皮肤 Shader、六组图案差异全部通过。结果副本为主工程 `CompilationChecks/isolated-item-surface-validation.json`，并非主工程运行报告。

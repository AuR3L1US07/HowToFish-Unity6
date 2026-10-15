# 人工语义整理

- `CreatureManager.GetRandomItem`：使用 `totalWeight`、`randomWeight`、`cumulativeWeight` 表达权重选择过程。保留向下地形查询，使用弃元明确当前不使用命中结果。
- `CreatureManager.HookItem`：将闪光判定相关的临时数字命名为 `shinyRoll` 和 `shinyChance`。
- `FishingRodCast.LateUpdate`：使用 `isBailOpen` 表达线杯开关状态，保留原有本地/远端选择及求值方式。
- `SaveSystem`：明确主文件、备份、临时文件的变量名；将固定文件数组的索引遍历改为 `foreach`；使用提前 `continue` 减少存档加载嵌套；日志使用字符串插值。主文件经校验有效时才更新备份，读写顺序保留。
- 核心类增加职责说明，注释均为本次整理新增。

自动转换记录在 `changes.json` 中。`local-name` 表示基于符号绑定的局部变量重命名，`target-typed-new` 表示保留明确声明类型的 `new()` 简写。未知含义和可能影响 Unity 引用的名称不做猜测性替换。

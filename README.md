<p align="center">
  <img src="Documentation/readme/hero.svg" width="100%" alt="How to Fish — Cast. Sail. Discover." />
</p>

<h3 align="center">一根鱼竿，一片海，一段岛屿冒险。</h3>

<p align="center">
  抛竿收获，驾船远行。与岛民交易，挑战海洋生物，<br />
  把每一次收获，变成探索下一座岛屿的底气。
</p>

<p align="center">
  <a href="#玩法一览">探索玩法</a> &nbsp; · &nbsp;
  <a href="#海面一瞥">游戏画面</a> &nbsp; · &nbsp;
  <a href="#开始你的航程">开始游玩</a>
</p>

<br />

## 关于游戏

**How to Fish** 将钓鱼、航海、岛屿探索与战斗串成一段海上旅程。你可以在岸边抛下第一竿，也可以驾驶自己的船出海，寻找新的鱼获、岛屿和挑战。

把捕获的生物带回岸边，出售收获、完成委托、升级装备。旅程越深入，遇到的生物也越难对付；除了鱼竿，你还需要准备好武器，迎接岛屿 Boss 的挑战。

## 玩法一览

<table>
  <tr>
    <td width="33%" valign="top">
      <img src="Documentation/readme/fish.svg" width="28" height="28" alt="" />
      <h3>抛竿与收获</h3>
      <p>准备鱼饵，等待咬钩，收线并带回鱼获。探索不同的海洋生物，积累你的收获。</p>
    </td>
    <td width="33%" valign="top">
      <img src="Documentation/readme/sail.svg" width="28" height="28" alt="" />
      <h3>驾船去远方</h3>
      <p>解锁自己的船，升级马达与雷达。穿过海面，把航线延伸到更远的岛屿。</p>
    </td>
    <td width="33%" valign="top">
      <img src="Documentation/readme/compass.svg" width="28" height="28" alt="" />
      <h3>岛屿与委托</h3>
      <p>与岛民交谈，交付任务物品，领取奖励。沿着任务与挑战，逐步解锁新的区域。</p>
    </td>
  </tr>
  <tr>
    <td width="33%" valign="top">
      <img src="Documentation/readme/coins.svg" width="28" height="28" alt="" />
      <h3>交易与成长</h3>
      <p>出售鱼获、赚取货币，购买装备与升级。让每次满载而归，都为下次出海做好准备。</p>
    </td>
    <td width="33%" valign="top">
      <img src="Documentation/readme/swords.svg" width="28" height="28" alt="" />
      <h3>武器与 Boss</h3>
      <p>应对危险生物，使用武器作战，挑战岛屿 Boss。将奖杯带回，继续推进旅程。</p>
    </td>
    <td width="33%" valign="top">
      <img src="Documentation/readme/shirt.svg" width="28" height="28" alt="" />
      <h3>你的专属造型</h3>
      <p>搭配帽子、服装和饰品，调整人物配色。带着自己的风格，开启下一次冒险。</p>
    </td>
  </tr>
</table>

<p align="center"><b>抛竿捕获 &nbsp; → &nbsp; 交易升级 &nbsp; → &nbsp; 驾船探索 &nbsp; → &nbsp; 挑战 Boss</b></p>

<br />

## 海面一瞥

![How to Fish 海面渲染画面](Documentation/WaterValidation-2026-10-15/water-restored.png)

<p align="center"><sub>工程内水面渲染画面 · 波浪、法线与海面细节</sub></p>

## 开始你的航程

1. **获取工程** — 克隆仓库，或通过 GitHub 的 **Code → Download ZIP** 下载并解压。
2. **使用 Unity 打开** — 在 Unity Hub 中添加工程目录，选择 **Unity 6000.4.4f1**，等待首次导入完成。
3. **进入游戏** — 双击 **Assets/Scenes/Game.unity**，点击 **Play**，创建或载入本地游戏。

> 当前提供 **Windows 离线游玩**。Steam 联机暂未开放。

<details>
<summary><b>工程环境与 Windows 打包</b></summary>

<br />

| 项目 | 配置 |
| --- | --- |
| Unity | 6000.4.4f1 |
| 目标平台 | Windows x64 |
| 脚本后端 | Mono / C# |
| 渲染管线 | URP 17.4.0 |
| 入口场景 | Assets/Scenes/Game.unity |

在仓库根目录使用 PowerShell 打包，先保存并关闭此工程的 Unity 编辑器。编辑器路径按本机安装位置修改：

```powershell
& 'E:\Unity\6000.4.4f1\Editor\Unity.exe' -batchmode -force-d3d11 -projectPath "$PWD" -executeMethod RecoveryBuildValidation.BuildWindows -logFile "$PWD\offline-build.log"
```

输出位置为 `Builds/Offline/HowToFish.exe`。运行时保留整个构建目录；仓库不包含本地构建，需要自行打包。

如果打开后只有天空、相机和灯光，请退出 Play，打开 `Assets/Scenes/Game.unity` 后重新运行。查看水面效果请进入 Play。

</details>

<br />

---

<p align="center">
  <a href="Documentation/Review-2026-10-15.md">更新记录</a> &nbsp; · &nbsp;
  <a href="Documentation/WaterResourceRecovery-2026-10-15.md">水面与渲染</a> &nbsp; · &nbsp;
  <a href="Documentation/OfflineRecoveryValidation-2026-10-14.md">验证记录</a> &nbsp; · &nbsp;
  <a href="SHARING.md">资源说明</a>
</p>
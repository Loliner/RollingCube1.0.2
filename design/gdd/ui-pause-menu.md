# GDD — 暂停菜单（GameFlow 集成）

## 1. 概述

游玩状态下按 Esc 冻结游戏并显示暂停面板，提供继续、重置和回到主界面。暂停菜单不直接
调用 `SceneManager.LoadScene`；所有操作由常驻 `GameFlowController` 统一执行，保证
Player、相机、计数器和 Additive 关卡生命周期一致。

## 2. 状态规则

- 只有 `GameFlowState.Playing` 可以进入暂停。
- 暂停后状态为 `Paused`，`Time.timeScale = 0`。
- 再按 Esc 或点击“继续”恢复 `Playing`，`Time.timeScale = 1`。
- 菜单预览、开始动画、关卡转场和返回主界面期间，Pause Menu 不响应 Esc。
- 直接运行单个关卡、场景中不存在 GameFlow 时，保留原单场景暂停兼容路径。

## 3. 按钮行为

### 3.1 继续

- 调用 `GameFlowController.ResumeGameplay()`。
- 不重建关卡，不更改 Player 和机关当前状态。

### 3.2 重置

- 隐藏暂停面板。
- 通过 `GameFlowController.ResetCurrentLevel()` 对当前关卡执行退出波纹。
- 卸载并重新 Additive 加载同名关卡。
- 复用持久 Player，调用 `PrepareForLevel` 恢复出生位置、朝向、刚体和死亡线。
- 进入动画结束后清零 Step/Dead 并恢复游玩。

### 3.3 回到主界面

- 隐藏暂停面板。
- 通过 `GameFlowController.ReturnToMenu()` 将当前关卡重建为干净的实时预览。
- 相机回到预览构图，Main Menu 淡入。
- 最终状态为 `PreviewReady` 且 `Time.timeScale = 0`。
- 不重新加载 `MainMenu` GameShell。

## 4. Player 与计数器

- `Player.Update` 同时尊重外部控制锁和 Pause Menu 状态。
- 转场开始会取消旧关遗留的 Player 协程和 Tween。
- `PrepareForLevel` 重置滚动/下落状态、刚体速度、Transform、出生点和 kill plane。
- Step/Dead 只在 GameFlow 明确进入一个新游玩回合时清零，不因 Additive sceneLoaded
  事件自动清零。
- 菜单预览和转场期间隐藏 Step/Dead HUD；Playing 与 Paused 时显示。

## 5. 边界情况

| 情况 | 处理 |
|---|---|
| 翻滚或机关移动中暂停 | `timeScale = 0` 冻结 scaled 动画；恢复后继续 |
| 暂停中点击重置 | 无缝重建当前关，旧动作被清理，不保留半途状态 |
| 暂停中回菜单 | 重建干净预览，不把被推动的箱子或机关状态带回选关背景 |
| 菜单按 Esc | MainMenu 处理关卡页返回；Pause Menu 不弹出 |
| 转场中按 Esc | 忽略，避免同时存在两个流程所有者 |
| 直接 Play 单个关卡 | 使用 StandaloneRig 与临时 Player，继续支持单场景重置/返回兼容行为 |

## 6. 验收标准

- [ ] Playing 按 Esc 立即进入 Paused，物理、玩家输入和 scaled 动画冻结。
- [ ] 继续恢复原回合，不改变关卡内容。
- [ ] 重置播放退出/进入动画并回到同一关出生状态，只有一个 Player。
- [ ] 回到主界面不加载第二个 MainMenu，而是恢复当前关卡的干净实时预览。
- [ ] 菜单、开始动画和关卡转场期间无法错误打开 Pause Menu。
- [ ] Step/Dead 只在开始新回合时清零，菜单预览期间不可见。

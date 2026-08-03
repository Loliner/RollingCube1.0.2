# GDD — 无缝关卡切换与波纹动画

## 1. 目标

选关预览和通关推进共用一套 Additive 场景切换流程。玩家、相机和 GameShell 不销毁，
只有 `LevelRoot` 内容退出、卸载和进入。

## 2. 动画单位

- `LevelRoot` 的每个直接子物体是一个动画单位。
- 地面砖必须分别是直接子物体，因此可以逐格传播。
- 复杂机关的零件保留在机关根物体下，整体作为一个单位缩放。
- `LevelTransitionExclude` 标记不参与动画的辅助对象，例如 `LevelSpawn`。
- 动画开始前缓存原始 `localScale`；进入结束必须恢复精确原值。

## 3. 距离排序

对每个动画单位计算它与 Player 的 XZ 平面距离：

```text
delay = distanceXZ(unit.position, player.position) × secondsPerGridUnit
```

- 越近的单位越早变化。
- Y 高度不影响传播顺序。
- 默认 `secondsPerGridUnit = 0.1s`。

## 4. 退出

- 从原始比例缩放到 0。
- 默认单体时长 0.35 秒。
- 默认缓动 `Ease.InBack`，允许自然产生先向外蓄力再收缩的感觉；不硬编码 1 → 1.2 → 0。
- 玩家不属于 `LevelRoot`，因此不消失。

## 5. 进入

- 从 0 缩放到缓存的原始比例。
- 默认单体时长 0.35 秒。
- 默认缓动 `Ease.OutBack`。
- Player 仍是传播中心。

## 6. 时序

```text
t=0.0  旧关退出开始；目标关 Additive 加载开始
t=1.0  若目标已就绪，新关进入开始；否则就绪后立即开始
       旧关退出完成后卸载
       新关进入与相机到位后 Physics.SyncTransforms
       Preview：保持 timeScale=0
       Gameplay：timeScale=1，释放 Player 外部控制
```

进入无需等待旧关所有远端单位完全消失，但必须晚于退出启动。`incomingOverlapDelay` 默认
1 秒，可在 GameFlowController 调整。

## 7. Player 生命周期

- 正常流程只使用 GameShell 的持久 Player。
- 新关 `LevelSpawn` 对齐当前 Player 世界位置，Player 不横跳。
- `PrepareForLevel` 清理旧 Tween/协程、刚体运动、缩放、旋转、出生点和 kill plane。
- 外部控制锁可嵌套；只有所有所有者都释放后才恢复输入。
- 直接打开关卡时，`LevelContext` 启用 `StandaloneRig` 并从 Player prefab 创建临时 Player。

## 8. 故障与输入竞争

- 加载目标必须在超时内提供 `LevelContext`，否则进入错误恢复路径。
- 快速预览点击不并行创建多个转场；只记录最后请求。
- 同名关卡重置必须先退出并卸载旧实例，再加载新实例，防止重复同名 scene。
- `Physics.SyncTransforms` 在交还控制前执行，防止缩放动画后的 collider 状态滞后。

## 9. 验收标准

- [ ] 退出和进入都以 Player 为中心按 XZ 距离传播。
- [ ] 退出使用 InBack，进入使用 OutBack，且均使用 unscaled DOTween。
- [ ] 新关可以在旧关尚未全部消失时开始出现。
- [ ] 玩家、相机和 GameShell 在选关与通关时保持同一实例。
- [ ] 同关重置不会产生重复 Scene、Player、Camera 或 AudioListener。
- [ ] 直接 Play 任意 Chapter 1 关卡仍可完成标准路线。

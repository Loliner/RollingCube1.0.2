# GDD — 复位机制（Respawn / Kill Plane）

## 1. 概述

玩家滚出关卡边缘、跌入下方没有地面的虚空时，此前会无限下落且没有任何恢复手段，只能重启场景（`level-01.md` 已记录的已知缺口）。本机制让 Player 在下落过程中持续检测「是否跌破一个可配置的高度阈值（`killPlaneY`）」，一旦跌破就视为跌出关卡边界，播放缩放消失/出现的过渡后传送回出生点并恢复正常控制，而不是继续等待一个本来就等不到的落地判定。

---

## 2. 玩家感受

「摔了一跤，缩成一团消失，又在起点冒出来」——不是关卡失败，也不需要手动重启。跌出边界后角色先缩小消失、再在出生点放大出现，比硬切瞬移更柔和，明确告诉玩家「发生了什么」而不是一个突兀的跳变；随后立刻恢复正常控制，可以立即重新尝试。这与关卡内「正常」的短距离下落（楼梯落差、被机关携带时的坠落、箱子牵引跌落）完全不冲突：那些情况都会在合理范围内落地，不会跌破 `killPlaneY`。

---

## 3. 详细规则

- Player 下落时（`StartFalling` → `LandWhenSettled`）原有的落地条件是「速度稳定 + 检测到支撑」；本机制在同一个轮询循环里，每帧优先检测 `transform.position.y < killPlaneY`
- 一旦跌破阈值：立即中止下落轮询，速度清零，`isKinematic` 恢复为 `true`，进入 `Respawn()`：先用 DOTween 把 `transform.localScale` 缩小到 `Vector3.zero`（`Ease.InSine`，时长 `respawnScaleDuration`），缩放完成后把位置/旋转瞬移回 `Awake()` 时记录的出生点（`spawnPosition`/`spawnRotation`），再放大回 `Vector3.one`（`Ease.OutSine`，同样时长）
- 整个缩放-传送-放大过程中 `isFalling` 保持 `true`（输入挂起），直到放大动画结束才清除，避免玩家在缩小/传送状态下还能操作
- 出生点只在 `Awake()` 记录一次（关卡摆放好之后的初始位置/旋转），不会因为之后机关移动、复位等操作而改变
- `killPlaneY` 是 Player 组件上的 Inspector 可调参数，按场景独立设置：建议设在该场景最低有效地形再往下 2~3 个单位，确保「正常下落」不会误触发，同时不会让玩家跌落太久才复位
- 本机制只作用于 Player；`PushableBlock` 的跌落不受影响——箱子跌落是很多关卡的有意设计（例如第九关箱子跌落砸中隐藏按钮触发桥升起），跌落后应该留在物理结算的位置，不应被传送回起点

---

## 4. 公式

```
触发条件：transform.position.y < killPlaneY（下落轮询期间每帧检测，优先于落地判定）
killPlaneY 建议值 = 场景最低有效地形 Y − 2~3 个单位（1 个单位 = cubeHalfSize × 2 = 1.0f）
复位动画：scale 1 → 0（Ease.InSine，respawnScaleDuration 秒）→ 瞬移到出生点 → scale 0 → 1（Ease.OutSine，respawnScaleDuration 秒）
respawnScaleDuration 默认 0.3f 秒
复位结果：position = spawnPosition，rotation = spawnRotation，
          linearVelocity = 0，angularVelocity = 0，isKinematic = true
```

---

## 5. 边界情况

| 情况 | 处理方式 |
|------|----------|
| 同一帧内跌破 `killPlaneY` 且同时检测到支撑 | 轮询循环里 `killPlaneY` 判断在前，一旦跌破直接复位，不再检查支撑 |
| 场景未手动调整 `killPlaneY` | 使用默认值 `-5f`，大多数关卡够用；地形明显更深的场景需要手动调低 |
| 玩家被机关（Elevator/Conveyor）携带时跌出边界 | 携带期间 `IsExternallyControlled = true`，不会进入 `StartFalling`/`LandWhenSettled`，本机制不介入 |
| 复位后出生点本身悬空（关卡搭建错误） | 不在本机制处理范围内，属于关卡搭建阶段需要保证的前提 |
| `PushableBlock` 被推下悬崖跌出场景 | 不受影响，箱子没有落地/复位逻辑，跌落后由物理引擎接管并保持非运动学状态 |

---

## 6. 依赖

- **Player.cs** — `StartFalling` / `LandWhenSettled` / 新增 `Respawn()`；改动集中在这一个脚本内部，无新增外部组件
- **level-01.md** — 该缺口的原始记录（本机制的修复对象）

---

## 7. 可调参数

| 参数 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `killPlaneY` | float | -5f | 跌出边界判定阈值，需按场景最低地形调整 |
| `respawnScaleDuration` | float | 0.3f | 缩小/放大各自的动画时长（秒） |

---

## 8. 验收标准

- [ ] 玩家滚出关卡边缘、下方没有地面时，不再无限下落，很快传送回出生点
- [ ] 复位后玩家立即恢复正常输入控制，可以重新尝试
- [ ] 楼梯落差、机关携带坠落等关卡内正常下落不受影响，仍然正常落地
- [ ] `PushableBlock` 跌落不受本机制影响，跌落后保持在物理结算位置（不会被传送）
- [ ] 复位过程中角色先缩小消失、传送后再放大出现，两段缩放时长一致、过渡平滑，不是硬切瞬移

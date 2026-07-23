# GDD — 推箱子（PushableBlock）

## 1. 概述

可推动的木箱，玩家滚动到木箱时可将其向前推动一格。木箱会检测目标格是否已被占用，推动后若失去支撑则下落。木箱也可被升降台携带。用于关卡谜题：堵路、填沟、压按钮。

---

## 2. 玩家感受

玩家向一个木箱方向滚动，木箱被顺势推出一格，和玩家的移动节奏完全同步。核心感受是 **「重量感与掌控感」** — 木箱阻力真实，每次推动都有明确的成功/失败反馈；以及 **「逻辑感」** — 玩家能预判木箱会停在哪里，并据此规划解题路线。

---

## 3. 详细规则

### 3.1 推动触发

- 玩家尝试向木箱所在格滚动时，`Player.cs` 调用 `PushableBlock.TryBeginPush(direction)`。
- `TryBeginPush` 返回 `true`：推动成功，玩家继续完成本次滚动。
- `TryBeginPush` 返回 `false`：推动失败（目标格被占用），触发 `Player.ShakeFeedback()`，玩家停留原地。

### 3.2 推动成功条件

同时满足以下所有条件才能推动：

1. 木箱当前没有正在进行的推动动画（`isMoving = false`）
2. 木箱没有被外部机关控制（`isExternallyControlled = false`）
3. 木箱目标格没有被其他碰撞体占用（`IsOccupied` 检测）

### 3.3 移动动画

- 木箱从当前位置滑向目标格（当前位置 + 推动方向 × 1格），使用 DOTween `InOutSine` 缓动。
- `pushDuration` 应与 `Player.rollDuration` 保持一致，确保玩家和木箱动画同步结束。
- 动画完成后：位置吸附到网格；检查支撑；若无支撑则触发下落。

### 3.4 下落

- 推动后（或被升降台放下后），木箱检测正下方是否有地面（射线距离 `cubeHalfSize + 0.05f`）。
- 无地面：`Rigidbody.isKinematic = false`，交由物理引擎处理下落。
- 下落后木箱不会自动归还运动学控制权（停在物理模拟的位置）。

### 3.5 被升降台携带

- 木箱实现了 `IExternallyControllable`，升降台可将其加入 riders 列表并携带移动。
- 携带期间 `isExternallyControlled = true`，无法被推动。
- 携带结束（`EndExternalControl()`）后：位置吸附到网格；检查支撑；若无支撑则下落。

### 3.6 占位检测（IsOccupied）

使用 `Physics.OverlapBox` 在目标格中心做 0.9× 缩小的盒形检测（避免贴边误判），任何非自身的**非 trigger** 碰撞体视为占用；trigger 碰撞体（`ElevatorSwitch`、`Elevator`、`SceneSwitcher` 等机关）不算物理阻挡，木箱可以被推入其触发区域——第八关"箱子压住开关"就依赖这一点：开关本身是 trigger，木箱必须能被推进去才能压住它。检测范围受 `surfaceMask` 图层过滤，与 `Player.GetBlockingCollider` 排除 trigger 的规则保持一致。

---

## 4. 公式

```
目标格位置 = SnapToGrid(当前位置 + 推动方向向量)
SnapToGrid：x/y/z 各自四舍五入到最近的 0.25 倍数
占位检测盒尺寸 = cubeHalfSize × 0.9（三轴）
下落检测射线长度 = cubeHalfSize + 0.05f（向下）
推动动画时长 = pushDuration（秒，应与 Player.rollDuration 一致）
```

缓动曲线：`Ease.InOutSine`。

---

## 5. 边界情况

| 情况 | 处理方式 |
|------|----------|
| 连续快速推两次 | 第一次动画未结束时 `isMoving = true`，第二次调用返回 `false` |
| 木箱对面也是木箱 | `IsOccupied` 检测到目标格被占用，推动失败 |
| 木箱对面是墙 | 同上，推动失败 |
| 木箱推入悬空位置 | 推动动画结束后 `FallIfUnsupported()` 触发下落 |
| 木箱下落后停在非网格高度 | 物理引擎负责，当前不做额外吸附（下落后木箱不再是运动学） |
| 升降台携带木箱到空中时升降台复位 | `EndExternalControl()` 后检查支撑；无支撑则下落 |
| 玩家站在木箱上时推另一个木箱 | 不在当前设计范围内（玩家无法站在木箱上，木箱顶面无 trigger） |
| 木箱前方是机关的 trigger 区域（`ElevatorSwitch`、`Elevator` 踏板等） | `IsOccupied` 忽略 trigger 碰撞体，推动正常成功；箱子进入后由该机关自己的 `OnTriggerEnter` 逻辑决定后续行为（如压力板记为占用体） |

---

## 6. 依赖

- **Player.cs** — 调用 `TryBeginPush(direction)` 发起推动
- **Elevator.cs** — 将木箱加入 riders 并携带（`IExternallyControllable` 接口）
- **IExternallyControllable.cs** — 接口定义
- **DOTween** — 推动动画
- **Rigidbody（Unity Physics）** — 下落时切换为非运动学模式

---

## 7. 可调参数

| 参数 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `cubeHalfSize` | float | 0.5f | 木箱半边长，用于网格吸附和下落检测 |
| `surfaceMask` | LayerMask | ~0（全部） | 参与占位检测和下落检测的图层 |
| `pushDuration` | float | 0.25f | 推动动画时长，应与 `Player.rollDuration` 保持同步 |

---

## 8. 验收标准

- [ ] 玩家向木箱方向滚动，木箱同步滑动一格，动画与玩家移动同步结束
- [ ] 目标格有障碍时，推动失败，玩家触发抖动反馈，木箱不移动
- [ ] 木箱连续两次快速推动时，第二次被忽略，不出现动画叠加
- [ ] 木箱推入悬空格后自动下落
- [ ] 木箱可被升降台携带，携带期间无法被推动
- [ ] 升降台释放木箱后，若无支撑则下落
- [ ] 两个木箱背靠背，推第一个时第二个作为障碍阻止推动

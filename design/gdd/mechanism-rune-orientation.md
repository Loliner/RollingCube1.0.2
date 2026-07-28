# GDD — 符文朝向（Rune Orientation）

## 1. 概述

玩家所滚动的立方体，固定有一个面标记为「符文面」（rune face），随立方体一起翻滚。原本「滚到终点即通关」的判定被替换为朝向判定：`SceneSwitcher` 现在要求立方体在终点格停留时符文面必须朝下才算通关。`ElevatorSwitch` 和 `Elevator` 也可以选配同样的朝向门控。这把游戏从「找到路径」的移动谜题，升级为「规划一条翻滚序列、让符文面在关键节点朝下」的朝向谜题——翻滚只能绕棱翻转，符文面会在 6 个面之间随翻滚方向循环，终点的朝向由整条路径共同决定，不是某一步单独能调整的。

本机制之前，现有 9 个关卡场景（`Chapter1_Scene1`~`Chapter1_Scene9`）和对应的 `level-01.md`~`level-09.md` 均以「到点即通关」为前提搭建，未考虑符文朝向。本机制上线后这些关卡大概率无法通关（终点朝向是巧合，不是设计出来的），暂不修改，等待后续单独的关卡重新设计任务处理。

---

## 2. 玩家感受

「原来最后一步的方向也有讲究」——玩家发现踩上终点格却没有通关，回想起自己最近几次翻滚的方向，意识到需要重新规划路径而不是重蹈原地。核心感受是 **「空间推理」**：不再只是记住一条路线，而是要在脑内（或通过试错）追踪符文面朝向的变化，规划出一条终点朝向刚好正确的路径——这是从「移动谜题」到「朝向谜题」的核心升级，也是 Bloxorz 一类翻滚谜题的经典乐趣来源。

---

## 3. 详细规则

### 3.1 符文面与朝向判定

- `Player.cs` 新增 `runeLocalAxis`（Inspector 可调的 `Vector3`，默认 `(0,1,0)`，即本地 +Y/顶面）：立方体自身坐标系下，指向符文面朝外方向的固定轴。翻滚只改变立方体的旋转，不改变这个本地轴的定义，所以符文面跟随立方体一起翻滚。默认放在顶面，是因为出生时符文朝上，直观对应「需要把它滚到朝下」这个目标。
- `Player.IsRuneFaceDown()`：把 `runeLocalAxis` 变换到世界空间（`transform.TransformDirection`），与 `Vector3.down` 做点积，`> 0.99f` 视为「朝下」。由于旋转始终吸附在 90° 增量上（见 3.5），这在实践中是二值判定，不存在「差一点」的模糊朝向。
- 符文面目前只是立方体六个面里普通的一个面，用一个占位视觉标记（子物体 + 占位材质）标识，不改动立方体网格本身；正式符文美术留待后续任务。
- 本机制只作用于 `Player`；`PushableBlock` 是滑动而非翻滚，没有旋转/朝向概念，天然不参与符文判定（见 5. 边界情况）。

### 3.2 SceneSwitcher：终点通关判定（无开关，全局生效）

- 玩家进入终点触发器后开始计时；`OnTriggerStay` 每帧检查 `IsRuneFaceDown()`，只有朝下时才累积停留时间（`dwellSeconds += Time.deltaTime`），朝向不对时停留时间不推进。
- 累积满 `requiredDwellSeconds`（默认 2 秒）且此时朝向仍然正确，才切换场景。
- 离开触发器（`OnTriggerExit`）会清零 `isTriggered`/`dwellSeconds`，重新进入需要重新计时。
- 不提供「关闭朝向要求」的开关——所有关卡统一生效，教学关卡的难度曲线完全通过关卡布局（起点朝向、路径长度）设计，而不是关闭规则。
- 玩家站在终点格上时无法通过翻滚改变朝向（翻滚必然离开当前格），所以不存在「停留中途从朝下变为不朝下」的过渡：实际只有「全程朝下」和「全程不朝下」两种情况。
- 今天不提供「朝向不对」的专门视觉/音效反馈——玩家踩上终点格但没有触发时，界面上没有额外提示，留待后续单独任务。

### 3.3 ElevatorSwitch：可选朝向门控（默认关闭）

- 新增 `requireRuneDown`（默认 `false`，保留原有行为：`Player` 或 `PushableBlock` 都能激活，不看朝向）。
- 为 `true` 时：`CanActivate` 只认符文朝下的 `Player`；`PushableBlock` 被排除在合法占用者之外——箱子没有朝向概念，永远无法满足条件。
- `holdDuration` 倒计时同样改为轮询式：每帧检查 `occupants.Count > 0`（即仍有合法占用者压着）才推进累积时间，而不是进入即开始的固定 `WaitForSeconds`。`requireRuneDown = false` 时这一分支保持原有 `WaitForSeconds(holdDuration)` 逻辑不变。
- 退出判定（`OnTriggerExit`）改为直接检查 `occupants` 是否包含该碰撞体，而不是重新调用 `CanActivate`——因为朝向门控下 `CanActivate` 读取的是实时朝向，不是每次调用都能得到同一结果的稳定判断，重新调用可能在退出时得到和进入时不同的结果。

### 3.4 Elevator：可选朝向门控（仅限自触发，不影响驮载）

- 新增 `requireRuneDown`（默认 `false`），只在 `selfTriggered = true` 时有意义。
- 为 `true` 时：`OnTriggerEnter` 只有在符文朝下的 `Player` 进入时才调用 `TriggerMove()`；箱子进入不会触发移动。
- **不影响 `riders` 列表**：任何 `IExternallyControllable`（箱子、玩家，任意朝向）进入触发器仍然无条件被加入 `riders`，电梯一旦因任何原因（自身触发、外部 `ElevatorSwitch`）开始移动，都会把当前所有 rider 一起搭载。「谁能让电梯自己动」和「电梯动的时候搭载谁」是两件独立的事。

### 3.5 旋转精度与吸附

- `Player.AnimateRoll()` 现在在每次翻滚结束时都调用 `SnapRotation()`（此前只在下落落地后调用），把旋转吸附到最近的 90° 增量。
- 原因：符文朝向现在是通关判定的直接依据，长关卡里几十次翻滚的四元数连乘可能累积浮点误差，不吸附的话有可能让「应该精确朝下」的姿态在点积判定里跌出 `0.99f` 阈值，导致诡异的判定失败。

---

## 4. 公式

```
符文朝向世界轴 = transform.TransformDirection(runeLocalAxis).normalized
朝下判定：Dot(符文朝向世界轴, Vector3.down) > 0.99f
  — 旋转始终吸附在 90° 增量，实际取值只会接近 1.0（朝下）、0.0（朝向水平四个方向之一）
    或 -1.0（朝上），0.99f 阈值在这三种情况之间留有充分余量，不存在临界模糊区间

SceneSwitcher 停留累积：
  dwellSeconds(t) = dwellSeconds(t-1) + (IsRuneFaceDown() ? Time.deltaTime : 0)
  通关条件：dwellSeconds >= requiredDwellSeconds（默认 2f）且 IsRuneFaceDown() == true

ElevatorSwitch 朝向门控倒计时（requireRuneDown = true 时）：
  dwellSeconds(t) = dwellSeconds(t-1) + (occupants.Count > 0 ? Time.deltaTime : 0)
  触发条件：dwellSeconds >= holdDuration（默认 1f）
  — occupants 在 requireRuneDown = true 时只包含符文朝下的 Player，箱子被 CanActivate 拒绝
```

示例：`requiredDwellSeconds = 2f`，玩家以符文朝下姿态进入终点格并保持不动 → 第 2 秒（约 120 帧 @60fps）触发场景切换；若中途翻滚离开再滚回（哪怕朝向依然正确），计时从 0 重新开始（`OnTriggerExit` 清零）。

---

## 5. 边界情况

| 情况 | 处理方式 |
|------|----------|
| 玩家以错误朝向踩上 `SceneSwitcher` | 停留时间不累积，永远不会满 `requiredDwellSeconds`；玩家需要离开重新规划路径再次进入 |
| 玩家在终点格上原地等待，朝向始终不对 | 同上——原地等待不会让朝向自己变化，翻滚离开是唯一改变朝向的方式 |
| `PushableBlock` 被推入 `requireRuneDown = true` 的 `ElevatorSwitch`/`Elevator` | 箱子没有朝向概念，`CanActivate`/`CanTriggerMove` 直接判定不满足，永远无法单独触发；但仍可作为 rider 被已经在动的电梯搭载（见 3.4） |
| 长关卡内几十次连续翻滚后到达终点 | `AnimateRoll` 每次结束都吸附旋转到最近 90°，避免浮点误差累积导致朝向判定失误 |
| 玩家因下落（`StartFalling`/`LandWhenSettled`）而不是翻滚改变了朝向 | 落地时的朝向由物理翻滚结果决定，不可预测；这是关卡设计需要规避的风险（比如终点前不安排会导致意外坠落的路径），不是本机制需要特殊处理的场景 |
| `ElevatorSwitch.requireRuneDown = true` 且箱子和朝向不对的玩家同时尝试进入 | 两者都不满足 `CanActivate`，都不会被加入 `occupants`，开关不会启动倒计时 |
| 现有 9 个关卡场景 / `level-01.md`~`level-09.md` | 均以「到点即通关」为前提搭建，未考虑符文朝向；本次改动后大概率无法通关，暂不修改，等待后续关卡重新设计任务 |

---

## 6. 依赖

- **Player.cs** — `runeLocalAxis` 字段、`IsRuneFaceDown()` 查询方法；`AnimateRoll()` 内新增的每次翻滚后旋转吸附
- **SceneSwitcher.cs** — 通关判定的主要消费者，无开关、全局生效
- **ElevatorSwitch.cs** / **mechanism-elevator.md** — 可选 `requireRuneDown`，双向依赖：本文档描述的判定逻辑由 `ElevatorSwitch` 消费，`mechanism-elevator.md` 也记录了该字段
- **Elevator.cs** / **mechanism-elevator.md** — 可选 `requireRuneDown`（仅限 `selfTriggered`），双向依赖同上
- **PushableBlock.cs** — 反向说明：不实现、不参与本机制（滑动而非翻滚，没有朝向概念）

---

## 7. 可调参数

| 参数 | 所属脚本 | 类型 | 默认值 | 说明 |
|------|----------|------|--------|------|
| `runeLocalAxis` | Player | Vector3 | (0,1,0) | 符文面在立方体本地坐标系下朝外的固定轴；需与实际摆放的符文视觉标记面一致 |
| `requiredDwellSeconds` | SceneSwitcher | float | 2f | 符文朝下状态下需要停留的秒数 |
| `requireRuneDown` | ElevatorSwitch | bool | false | 是否要求朝向门控（true 时排除箱子，只认朝向正确的 Player） |
| `holdDuration` | ElevatorSwitch | float | 1f | 朝向门控开启时，倒计时同样以此为目标（轮询式累积） |
| `requireRuneDown` | Elevator | bool | false | 仅在 `selfTriggered = true` 时生效；只门控自触发，不影响 riders 搭载 |

---

## 8. 验收标准

- [ ] 玩家以符文朝下姿态进入 `SceneSwitcher` 并停留 2 秒，场景切换
- [ ] 玩家以错误朝向进入 `SceneSwitcher`，停留任意久都不会切换场景
- [ ] 玩家在 `SceneSwitcher` 内以正确朝向停留期间离开再重新进入，计时从 0 重新开始
- [ ] 长关卡（20+ 次翻滚）到达终点时，符文朝向判定依然准确，不因浮点误差误判
- [ ] `ElevatorSwitch.requireRuneDown = false` 时，行为与改动前完全一致（Player/Box 均可触发，不看朝向）
- [ ] `ElevatorSwitch.requireRuneDown = true` 时，只有符文朝下的 Player 能触发；箱子压上去没有反应
- [ ] `Elevator.requireRuneDown = true` 且 `selfTriggered = true` 时，箱子进入不会让电梯自己动，但如果电梯因其他原因动了，箱子仍会被搭载
- [ ] `PushableBlock` 在任何符文相关判定中都不报错、不参与，行为与改动前一致

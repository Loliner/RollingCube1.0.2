# GDD — 升降台（Elevator）

## 1. 概述

升降台是一个可移动的平台，触发后沿固定偏移量移动到目标位置，并携带站在上面的玩家或木箱一起移动。支持自触发（踩上即动）和外部触发（踩按钮才动）两种模式，以及触发后自动复位的选项。

---

## 2. 玩家感受

玩家踩上升降台（或按下开关后），平台带着玩家平滑抬升或下降，打开通往新区域的路径。核心感受是：**「操控感」** — 玩家能预判平台会把自己送到哪里，并据此规划路线；以及 **「解谜感」** — 何时踩台、踩哪个台，是关卡的核心谜题。

---

## 3. 详细规则

### 3.1 触发方式

| 模式 | 说明 | Inspector 配置 |
|------|------|----------------|
| **自触发**（默认） | 玩家或木箱进入升降台的 Trigger 碰撞体即开始移动 | `selfTriggered = true` |
| **外部触发** | 由独立的 `ElevatorSwitch` 调用 `TriggerMove()` 触发 | `selfTriggered = false` |

### 3.2 移动行为

- 升降台从初始位置（`elevatorStartPos`）移动到 `elevatorStartPos + offset`，使用 DOTween `InOutSine` 缓动。
- 已触发时再次进入触发器不会重复移动（一次性锁定）。
- `switcherFollow = true` 时，触发器碰撞体跟随平台一起移动（适用于平台和触发器是分开物体的情况）。

### 3.3 驮载（Carrier）

- 任何实现了 `IExternallyControllable` 接口的物体（玩家、木箱）站在台上都会被一起携带。
- 携带期间调用 `BeginExternalControl()`，动画结束后调用 `EndExternalControl()`，物体的输入/物理在此期间挂起。
- 同时允许多个 rider。

### 3.4 复位（Reset）

- `reset = false`（默认）：移动到目标后永久停留，不复位。
- `reset = true`：触发复位倒计时，倒计时结束后返回初始位置；复位完成后 `isTriggered` 重置为 `false`，允许再次触发。倒计时何时开始由 `resetOnArrival` 决定：
  - `resetOnArrival = false`（默认）：等所有 rider 离开触发器后，才开始等待 `resetDelay` 秒。
  - `resetOnArrival = true`：一到达目标位置就立刻开始等待 `resetDelay` 秒，不管上面是否还站着人；复位时会把仍在平台上的 rider 一并带回起点（`CarryRiders(-offset)`）。适合"限时窗口"类设计——玩家如果没能在平台缩回前完成后续操作，会被平台原样带回起点重新尝试，而不是被晾在半路。

### 3.5 联动升降台（LinkedElevator）

`LinkedElevator` 继承自 `Elevator`，在触发时额外将一个 `linkedGameObject` 移动 `linkedOffset`，与主平台同步运动。两者使用相同的 `moveDuration`。（子类扩展，不在 `Elevator` 里加分支。）

### 3.6 外部开关（ElevatorSwitch）

- 玩家进入开关触发器后，需持续停留 `holdDuration` 秒才触发目标升降台；提前离开则取消。
- 支持同时触发多个升降台，每个可配置独立延迟（`delay`），实现顺序联动。

---

## 4. 公式

```
目标位置 = elevatorStartPos + offset
移动时长 = moveDuration（秒，默认 2f）
复位等待 = resetDelay（秒，默认 3f）
开关持续 = holdDuration（秒，默认 1f）
```

缓动曲线：`Ease.InOutSine`（起止慢、中间快，所有升降台一致）。

---

## 5. 边界情况

| 情况 | 处理方式 |
|------|----------|
| 升降台正在移动时再次踩上 | `isTriggered = true` 时 `TriggerMove()` 直接返回，不重复触发 |
| 玩家和木箱同时在台上 | 两者都加入 riders 列表，同步携带 |
| 携带过程中玩家输入 | 外部控制期间 `Player` 忽略输入（`IsExternallyControlled` 检查） |
| 木箱被携带到空中后平台复位 | 木箱 `EndExternalControl()` 后检查支撑，无支撑则触发下落 |
| 开关持续时间到后玩家仍站在上面 | 正常触发；后续是否 reset 由 `Elevator.reset` 控制 |
| 多开关触发同一升降台 | `isTriggered` 锁定，后续 `TriggerMove()` 调用均被忽略 |

---

## 6. 依赖

- **Player.cs** — 实现 `IExternallyControllable`；`BeginExternalControl` / `EndExternalControl` 接口
- **PushableBlock.cs** — 同样实现 `IExternallyControllable`，可被携带
- **IExternallyControllable.cs** — 接口定义（`Transform`、`IsExternallyControlled`、`Begin/EndExternalControl`）
- **DOTween** — 所有移动动画
- **ElevatorSwitch.cs** — 外部触发开关（非必须，取决于关卡设计）
- **LinkedElevator.cs** — 联动扩展（非必须）

---

## 7. 可调参数

| 参数 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `offset` | Vector3 | (0,0,0) | 相对初始位置的目标偏移量 |
| `moveDuration` | float | 2f | 升降动画时长（秒） |
| `reset` | bool | false | 是否复位 |
| `resetDelay` | float | 3f | 复位前等待时间（秒） |
| `resetOnArrival` | bool | false | 复位倒计时的起点：false=等所有人离开触发器；true=一到达目标位置就开始倒计时，到点后连人一起带回起点 |
| `selfTriggered` | bool | true | 是否自触发 |
| `switcherFollow` | bool | false | 触发器碰撞体是否跟随移动 |
| `holdDuration`（Switch） | float | 1f | 开关需持续踩住的时长（秒） |
| `delay`（Switch Target） | float | 0f | 各目标升降台的额外触发延迟 |

---

## 8. 验收标准

- [ ] 踩上自触发升降台后，平台以 `InOutSine` 缓动移动到目标位置
- [ ] 玩家被升降台携带，在移动期间无法自主移动
- [ ] 木箱也能被升降台携带
- [ ] 移动中再次触发不会重叠启动第二次移动
- [ ] `reset = true` 且 `resetOnArrival = false` 时，玩家离开后延迟复位，再次踩上可重新触发
- [ ] `reset = true` 且 `resetOnArrival = true` 时，到达目标后即使玩家仍站在平台上，也会在 `resetDelay` 秒后自动缩回并把玩家带回起点
- [ ] `reset = false` 时，升降台停留在目标位置不复位
- [ ] `ElevatorSwitch` 需持续踩住 `holdDuration` 秒才触发，提前离开取消
- [ ] `LinkedElevator` 触发时联动第二个物体同步移动
- [ ] 升降台复位过程中玩家站在上面不会被携带回去（riders 列表已清空）

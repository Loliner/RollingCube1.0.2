# GDD — 第八关：箱子与按钮桥

**所属章节**：第一章

**关卡编号**：1-8

**核心机制**：可逆压力板、箱子持续占位、升降桥

**场景文件**：`Assets/Scenes/Chapter1/Chapter1_Scene8.unity`

**状态**：Designed — 待场景重建

---

## 1. 概述

第八关教学“箱子可以代替玩家持续触发机关”。玩家亲自踩上压力板时，桥会升起；一旦
离开，桥又会落下。压力板上方放置一只箱子，周围地形将推动方向限制为向下。玩家把
箱子推到压力板上后，箱子永久占位，玩家得以自由离开并通过保持升起的桥。

## 2. 玩家感受

玩家先看到自己踩下按钮、桥随之上升，离开后桥又缩回，迅速理解“按钮需要持续压住”。
箱子与按钮处于同一视野内，下一步推理自然变成“让别的东西替我留下”。箱子压住按钮
后，桥稳定保持，给玩家明确的代理成功感。

## 3. 关卡布局

```rcmap
@level Chapter1_Scene8
@grid cell=1.00 height_step=0.25

@layer y=+0.00
      x+00 x+01 x+02 x+03 x+04 x+05 x+06 x+07 x+08 x+09
z+03 [#S] [#.] [#.] [#.] [..] [..] [..] [..] [..] [..]
z+02 [X.] [#a] [#.] [#.] [..] [#.] [#.] [#.] [#.] [#.]
z+01 [X.] [#s] [X.] [#.] [..] [#.] [#.] [#E] [#.] [#.]
z+00 [..] [X.] [..] [#.] [..] [#.] [#.] [#.] [#.] [#.]

@layer y=-1.00
      x+00 x+01 x+02 x+03 x+04 x+05 x+06 x+07 x+08 x+09
z+03 [..] [..] [..] [..] [..] [..] [..] [..] [..] [..]
z+02 [..] [..] [..] [..] [..] [..] [..] [..] [..] [..]
z+01 [..] [..] [..] [..] [=b] [..] [..] [..] [..] [..]
z+00 [..] [..] [..] [..] [..] [..] [..] [..] [..] [..]

@start S
at=(x=+00,z=+03,y=+0.00)
rune=UP

@end E
next=Chapter1_Scene9

@entity a
type=PushableBlock
name=holding_box
at=(x=+01,z=+02,y=+0.00)
target=(x=+01,z=+01,y=+0.00)

@entity s
type=ElevatorSwitch
name=bridge_pressure_plate
at=(x=+01,z=+01,y=+0.00)
targets=[b]
activator=player_or_box
hold=1.0s

@entity b
type=Elevator
name=reversible_bridge
at=(x=+04,z=+01,y=-1.00)
target=(x=+04,z=+01,y=+0.00)
self_triggered=false
reset=true
move_duration=1.0s

@solution primary
route="R PUSH(a,D) WAIT(1.0s) R R D R R R U R D WAIT(2.0s)"
steps=11
start_rune=UP
end_rune=DOWN
end_at=E
assert=[holding_box.on_bridge_pressure_plate,reversible_bridge.raised,rune.DOWN]
```

布局约束：

- `holding_box` 位于压力板正上方一格。
- `X` 格封住箱子的侧推和继续向下推动方向，使箱子只能被推到压力板且无法再移开。
- 玩家可从上方进入压力板并原路离开，以观察桥的可逆行为。
- 桥入口为 `(3,1)`，桥面为 `(4,1)`，终点平台入口为 `(5,1)`。
- 桥未升起时，左右区域不存在其他连接。
- `E` 位于右侧 `5 × 3` 草地区内部 `(7,1)`。

## 4. 详细规则

### 4.1 玩家试踩

玩家本人进入 `bridge_pressure_plate` 并停留一秒时，桥开始上升；玩家原路离开后，
`ElevatorSwitch` 调用 `TriggerReset()`，桥立即回落。桥的移动允许被新的触发反向
打断，不要求先完成整段动画。

### 4.2 箱子持续占位

标准解从 `S` 执行 `R PUSH(a,D)`。箱子进入压力板后保持在 `(1,1)`，玩家停在
`(1,2)`。箱子持续存在于 switch 的 occupants 中，因此桥升起后不会收到复位调用。

箱子后方 `(1,0)` 和侧面限位确保玩家不能把箱子从压力板推走。

### 4.3 过桥与终点

玩家等待桥升起后，沿 `R R D` 到达桥入口，再连续两次向右滚动经过桥面并进入右侧
平台。最后执行 `R U R D` 返回 `E`，符文面朝下。

## 5. 公式与解法验证

```text
switch_hold = 1.0s
bridge_rise = 1.00
bridge_move_duration = 1.0s
push_count = 1
primary_steps = 11

primary = R PUSH(a,D) R R D R R R U R D
```

玩家、箱子与桥状态联合检查已验证：

- 未触发 switch 时左右区域不连通；
- 玩家单独触发并离开后，桥回到低位；
- 箱子进入 switch 后无法从任何方向继续推动；
- bridge 处于高位时标准解每步合法；
- 第 11 步位于 `E`，符文方向为 `DOWN`。

## 6. 边界情况

| 情况 | 处理方式 |
|------|----------|
| 玩家单独踩住按钮 | 桥升起，但玩家离开后立即回落 |
| 玩家在桥移动中离开按钮 | 当前 tween 被反向移动取代，不发生双重动画 |
| 玩家从侧面尝试推动箱子 | `X` 格或不可达站位阻止推动 |
| 玩家尝试把箱子推过按钮 | `(1,0)` 的 `X` 格阻挡 |
| 箱子压住按钮后玩家再次踩入 | occupants 数量变化不影响桥的持续激活 |
| 玩家在桥回落时尝试通过 | 脚下失去支撑后下落并复位；标准解要求等待桥稳定 |
| 玩家错误朝向进入 `E` | 不通关，可在右侧草地区调整 |

## 7. 依赖

- `ElevatorSwitch.cs`
  - 玩家或箱子触发；
  - occupants 持续占位；
  - 离开后反向复位。
- `Elevator.cs`
  - 可在移动中响应 `TriggerMove()` / `TriggerReset()`；
  - `reset=true`。
- `PushableBlock.cs`
  - 箱子移动到 trigger 格。
- `Player.cs`
  - 推箱、符文朝向与出界复位。
- `mechanism-elevator.md`
  - 可逆压力板桥规则。

## 8. 可调参数

| 参数 | 建议值 | 影响 |
|------|--------|------|
| `bridge_pressure_plate.holdDuration` | `1.0s` | 让玩家看到持续触发关系 |
| `reversible_bridge.moveDuration` | `1.0s` | 试踩反馈足够迅速 |
| 桥面上升距离 | `1.00` | 低位不可通行，高位与两岸齐平 |
| 箱子限位 | 三侧明确阻挡 | 排除不可恢复的错误推动 |

## 9. 验收标准

- [ ] Unity 场景按钮、箱子、桥和地形与 RCMap 一致。
- [ ] 玩家单独踩住按钮 1 秒后桥升起，离开后桥回落。
- [ ] 桥在动画中反向时没有瞬移、重叠 tween 或错误终态。
- [ ] `holding_box` 只能向下推动到 `(1,1)`。
- [ ] 箱子压住按钮后无法再被推离。
- [ ] 箱子持续占位期间桥稳定保持在 `y=+0.00`。
- [ ] 未使用按钮或箱子时不存在到达右侧平台的路线。
- [ ] `R PUSH(a,D) R R D R R R U R D` 的 11 次滚动均合法。
- [ ] 标准解结束时玩家位于 `E`，符文面朝下。
- [ ] 正确停留 2 秒后加载 `Chapter1_Scene9`。

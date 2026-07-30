# GDD — 第三关：升降桥

**所属章节**：第一章

**关卡编号**：1-3

**核心机制**：压力开关、永久升起的双格桥

**场景文件**：`Assets/Scenes/Chapter1/Chapter1_Scene3.unity`

**状态**：Designed — 待场景重建

---

## 1. 概述

第三关首次教学“玩家可以触发机关”。起点平台和终点平台之间隔着两格宽的沟壑，两块
桥面初始位于低处。玩家踩住压力开关一秒，桥面升至与两侧地面齐平并永久保持，随后
通过桥梁到达终点。

开关、桥面和通行方向在同一镜头中可见，使玩家能直接读出因果关系。

## 2. 玩家感受

玩家踩上带有视觉标识的开关后，先听到确认反馈，再看见两块桥面同时升起。机关变化
不是抽象状态，而是立即打开一条此前不存在的路。通关路线随后经过刚刚升起的桥，
完成“发现—触发—利用”的第一次完整机关教学。

## 3. 关卡布局

```rcmap
@level Chapter1_Scene3
@grid cell=1.00 height_step=0.25

@layer y=+0.00
      x-04 x-03 x-02 x-01 x+00 x+01 x+02 x+03 x+04 x+05
z+01 [#.] [#.] [#a] [#.] [..] [..] [#.] [#.] [#.] [#.]
z+00 [#S] [#.] [#.] [#.] [..] [..] [#.] [#E] [#.] [#.]
z-01 [#.] [#.] [#.] [#.] [..] [..] [#.] [#.] [#.] [#.]

@layer y=-1.00
      x-04 x-03 x-02 x-01 x+00 x+01 x+02 x+03 x+04 x+05
z+01 [..] [..] [..] [..] [..] [..] [..] [..] [..] [..]
z+00 [..] [..] [..] [..] [=b] [=c] [..] [..] [..] [..]
z-01 [..] [..] [..] [..] [..] [..] [..] [..] [..] [..]

@start S
at=(x=-04,z=+00,y=+0.00)
rune=UP

@end E
next=Chapter1_Scene4

@entity a
type=ElevatorSwitch
name=bridge_switch
at=(x=-02,z=+01,y=+0.00)
targets=[b,c]
activator=player
hold=1.0s

@entity b
type=Elevator
name=bridge_west
at=(x=+00,z=+00,y=-1.00)
target=(x=+00,z=+00,y=+0.00)
self_triggered=false
reset=false

@entity c
type=Elevator
name=bridge_east
at=(x=+01,z=+00,y=-1.00)
target=(x=+01,z=+00,y=+0.00)
self_triggered=false
reset=false

@solution primary
route="R R U WAIT(1.0s) R D R R R R WAIT(2.0s)"
steps=9
start_rune=UP
end_rune=DOWN
end_at=E
assert=[bridge_switch.triggered,bridge_west.raised,bridge_east.raised,rune.DOWN]
```

布局约束：

- 起点平台为 `x=-04..-01`、`z=-01..+01` 的 `4 × 3` 区域。
- 终点平台为 `x=+02..+05`、`z=-01..+01` 的 `4 × 3` 区域。
- 两个平台之间只有 `z=+00` 上的 `bridge_west` 和 `bridge_east` 可以形成连接。
- 两块桥面必须同步升到 `y=+0.00`，不得留下高度差或格间缝隙。
- 开关位于起点平台上方支路，玩家必须主动偏离直线路线才能踩到。
- `E` 周围保留普通地形，错误朝向进入后仍可离开调整。

## 4. 详细规则

### 4.1 开关

玩家进入 `bridge_switch` 并持续停留 `1.0s` 后，开关同时调用两块桥面的
`TriggerMove()`。未满一秒离开时取消本次触发，桥面保持低位。

虽然 `ElevatorSwitch` 会在最后一个占用者离开时调用 `TriggerReset()`，两块桥均配置
为 `reset=false`，因此一旦触发就永久保持高位。

### 4.2 桥面

桥面初始表面为 `y=-1.00`。未触发时玩家即使误落到低位桥面，也无法攀爬到
`y=+0.00` 的终点平台，只能离开边缘并通过复位重新尝试。

升起后，桥面与两侧地面处于相同高度，玩家可以按正常网格滚动连续通过。桥面本身
`selfTriggered=false`，不会因为玩家误触而自行运动。

### 4.3 标准解

玩家执行 `R R U` 到达开关，等待一秒；桥升起后以 `R D` 回到桥入口，再连续向右四
格经过两块桥面并进入 `E`。第九次滚动结束时符文面朝下。

## 5. 公式与解法验证

```text
bridge_rise = 1.00
switch_hold = 1.0s
bridge_move_duration = 2.0s
primary_steps = 9

primary = R R U WAIT(1.0s) R D R R R R
```

状态搜索已验证：

- 未触发 `bridge_switch` 时，起点平台与终点平台之间不存在可行走路径；
- 标准解在第 3 步到达开关；
- 桥升起后每一步均位于合法格位；
- 第 9 步位于 `E`，符文方向为 `DOWN`。

## 6. 边界情况

| 情况 | 处理方式 |
|------|----------|
| 玩家在开关上停留不足 1 秒 | 取消待触发协程，桥保持低位 |
| 玩家触发后立即离开开关 | 两块桥继续完成上升，并永久保持 |
| 玩家在桥升起前滚入沟壑 | 可能落到低位桥面，但无法上岸；离开边缘后复位 |
| 两块桥面不同步 | 视为场景配置错误，不允许进入验收 |
| 玩家错误朝向进入 `E` | 不通关，可利用终点平台继续调整 |
| 玩家再次踩开关 | 已升起的桥不会重复移动或抖动 |

## 7. 依赖

- `ElevatorSwitch.cs`
  - `holdDuration`；
  - 多目标触发；
  - 提前离开取消。
- `Elevator.cs`
  - `selfTriggered=false`；
  - `reset=false`；
  - `offset=(0,+1,0)`。
- `Player.cs`
  - 网格滚动、阻挡、跌落和复位。
- `SceneSwitcher.cs`
  - 全局符文终点规则。
- `mechanism-elevator.md`
  - 升降机关配置约束。

## 8. 可调参数

| 参数 | 建议值 | 影响 |
|------|--------|------|
| `bridge_switch.holdDuration` | `1.0s` | 让玩家明确意识到自己正在持续触发 |
| 桥面上升距离 | `1.00` | 未触发时无法直接通行 |
| `Elevator.moveDuration` | `2.0s` | 保证机关因果变化可观察 |
| 沟壑宽度 | 2 格 | 明确阻断直行，同时保持桥结构易读 |

## 9. 验收标准

- [ ] Unity 场景地形、开关和两块桥面与 RCMap 坐标一致。
- [ ] 未触发开关时不存在从起点平台到终点平台的路线。
- [ ] 玩家在开关停留不足 1 秒后离开，桥不移动。
- [ ] 玩家停留满 1 秒后，两块桥面同步、平滑升起。
- [ ] 桥面升起后与两侧地面完全齐平。
- [ ] 桥升起后离开开关不会令桥复位。
- [ ] `R R U R D R R R R` 的九次滚动均合法，最终位于 `E` 且符文面朝下。
- [ ] 正确停留 2 秒后加载 `Chapter1_Scene4`。
- [ ] 误落低位桥面的玩家能够通过 `killPlaneY` 复位，不会永久卡住。

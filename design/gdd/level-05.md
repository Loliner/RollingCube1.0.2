# GDD — 第五关：双机关顺序

**所属章节**：第一章

**关卡编号**：1-5

**核心机制**：永久解锁闸块、限时返回移动平台、执行顺序

**场景文件**：`Assets/Scenes/Chapter1/Chapter1_Scene5.unity`

**状态**：Designed — 待场景重建

---

## 1. 概述

第五关组合两套玩家已经见过的机关，并首次要求安排顺序。压力开关 `prerequisite_switch`
会让对岸入口处的闸块永久下沉；自触发平台 `returning_platform` 则把玩家运到闸块前，
并在三秒后自动返回。

若玩家先搭平台，闸块会阻止离台，平台随后把玩家安全送回。正确顺序是先解除闸块，
再搭平台通过。错误尝试只损失时间，不造成死亡或死局。

## 2. 玩家感受

第一次反序尝试让玩家亲眼看到“我到得了那里，但出口仍被挡住”。平台自动返回后，
玩家保留全部进度理解，不必重启。再次观察起点区域中的独立开关，玩家会自然推导出
“先改变出口，再出发”的正确顺序。

## 3. 关卡布局

```rcmap
@level Chapter1_Scene5
@grid cell=1.00 height_step=0.25

@layer y=+0.00
      x-05 x-04 x-03 x-02 x-01 x+00 x+01 x+02 x+03 x+04 x+05 x+06 x+07 x+08
z+01 [#.] [#.] [#b] [#.] [#.] [..] [..] [..] [#.] [#.] [#.] [#.] [#.] [#.]
z+00 [#S] [#.] [#.] [#.] [#.] [=a] [..] [..] [#c] [#.] [#.] [#.] [#E] [#.]
z-01 [#.] [#.] [#.] [#.] [#.] [..] [..] [..] [#.] [#.] [#.] [#.] [#.] [#.]

@start S
at=(x=-05,z=+00,y=+0.00)
rune=UP

@end E
next=Chapter1_Scene6

@entity a
type=Elevator
name=returning_platform
at=(x=+00,z=+00,y=+0.00)
target=(x=+02,z=+00,y=+0.00)
self_triggered=true
reset=true
reset_on_arrival=true
reset_delay=3.0s
move_duration=2.0s

@entity b
type=ElevatorSwitch
name=prerequisite_switch
at=(x=-03,z=+01,y=+0.00)
targets=[c]
activator=player
hold=1.0s

@entity c
type=Elevator
name=passage_gate
at=(x=+03,z=+00,y=+0.00)
target=(x=+03,z=+00,y=-1.00)
self_triggered=false
reset=false
role=blocking_cube

@solution primary
route="R R U WAIT(1.0s) D R R R RIDE(a) R R R R R WAIT(2.0s)"
steps=12
start_rune=UP
end_rune=DOWN
end_at=E
assert=[prerequisite_switch.triggered,passage_gate.lowered,returning_platform.at_target,rune.DOWN]
```

布局约束：

- 起点区域为 `x=-05..-01`、`z=-01..+01`。
- `prerequisite_switch` 位于起点主路线的上方支路。
- `returning_platform` 初始位于 `(x=+00,z=+00)`，目标为 `(x=+02,z=+00)`。
- 平台目标位置与带普通地面的闸块格 `(x=+03,z=+00)` 相邻。
- 闸块初始占据玩家高度的碰撞空间；下沉后保留其下方静态地面，使该格可正常滚入。
- 终点平台为 `x=+03..+08`、`z=-01..+01`，`E` 位于 `(x=+07,z=+00)`。
- `x=+01` 没有静态支撑，玩家无法不借助平台跨越。

## 4. 详细规则

### 4.1 前置开关与闸块

玩家在 `prerequisite_switch` 上停留 `1.0s` 后，`passage_gate` 向下移动一格并永久
保持。闸块移入地面下方后，其顶面不得继续进入玩家的阻挡检测盒。

闸块下方始终存在普通静态地面，因此“解锁”只是移除阻挡，不会制造新的下落。

### 4.2 返回平台

玩家进入 `returning_platform` 后被运送到 `(x=+02,z=+00)`。平台从抵达目标位置开始
计时 `3.0s`，随后自动回到起点；仍站在平台上的玩家会被一并带回。

### 4.3 顺序结果

错误顺序：

```text
board returning_platform
-> passage_gate blocks R
-> wait 3.0s
-> platform carries player back
```

正确顺序：

```text
activate prerequisite_switch
-> passage_gate lowers permanently
-> board returning_platform
-> cross the cleared gate cell
-> reach E
```

玩家在错误顺序中尝试向右离台时，只会触发阻挡抖动并留在平台上，不会跌入沟壑。

## 5. 公式与解法验证

```text
gate_offset = (0.00,-1.00,0.00)
platform_offset = (+2.00,0.00,0.00)
platform_reset_delay = 3.0s
primary_steps = 12

primary = R R U D R R R RIDE(a) R R R R R
```

朝向状态：

```text
rune_before_RIDE = RIGHT
rune_after_RIDE = RIGHT
rune_after_five_exit_rolls = DOWN
```

状态搜索和机关前置条件检查已验证：

- `passage_gate` 未下沉时，从平台目标位置向右移动失败；
- 开关触发后，标准解的所有格位连续可达；
- 普通滚动共 12 步，最终位于 `E` 且符文朝下；
- 正确路线能在平台回程计时前离开平台，之后平台状态不影响通关。

## 6. 边界情况

| 情况 | 处理方式 |
|------|----------|
| 玩家先搭乘平台 | 闸块阻挡离台，3 秒后平台带玩家返回 |
| 玩家撞击闸块后连续输入 | 每次只产生阻挡反馈，不会穿过闸块 |
| 玩家在开关停留不足 1 秒 | 闸块不移动 |
| 闸块已经下沉后再次踩开关 | 不重复移动 |
| 玩家正确离台后平台开始回程 | 玩家已不在 riders 中，不会被隔空带回 |
| 玩家错误朝向进入 `E` | 不通关，可在终点平台调整 |

## 7. 依赖

- `ElevatorSwitch.cs`
  - 前置开关的停留确认。
- `Elevator.cs`
  - 闸块永久下沉；
  - 平台的 `resetOnArrival` 和 riders 携带。
- `Player.cs`
  - 外部控制、阻挡反馈和符文判定。
- `SceneSwitcher.cs`
  - 全局符文终点规则。
- `mechanism-elevator.md`
  - 两种 Elevator 配置的权威规则。

## 8. 可调参数

| 参数 | 建议值 | 影响 |
|------|--------|------|
| 开关停留时间 | `1.0s` | 前置动作的确认感 |
| 闸块下沉距离 | `1.00` | 确保完全离开玩家阻挡检测盒 |
| 平台移动时间 | `2.0s` | 维持与第四关一致的运送节奏 |
| 平台返回延迟 | `3.0s` | 给玩家足够时间理解出口被阻挡 |

## 9. 验收标准

- [ ] Unity 场景的开关、平台、闸块和地形坐标与 RCMap 一致。
- [ ] 闸块初始状态完全阻挡平台出口。
- [ ] 先搭平台时，玩家无法离台，并在 3 秒后被安全带回。
- [ ] 错误顺序不造成死亡、跌落或永久卡死。
- [ ] 踩住前置开关 1 秒后，闸块永久下沉。
- [ ] 闸块下沉后 `(x=+03,z=+00)` 保持完整地面支撑。
- [ ] 正确顺序下玩家能在平台回程前离开，并正常抵达终点平台。
- [ ] `R R U D R R R RIDE(a) R R R R R` 的 12 次正常滚动均合法。
- [ ] 标准解结束时玩家位于 `E`，符文面朝下。
- [ ] 正确停留 2 秒后加载 `Chapter1_Scene6`。

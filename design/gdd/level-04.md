# GDD — 第四关：移动平台

**所属章节**：第一章

**关卡编号**：1-4

**核心机制**：自触发移动平台、外部控制、朝向保持

**场景文件**：`Assets/Scenes/Chapter1/Chapter1_Scene4.unity`

**状态**：Designed — 待场景重建

---

## 1. 概述

第四关首次让机关直接运送玩家。自触发移动平台停在起点侧，玩家滚上平台后，平台接管
玩家位置并水平移动到对岸；平台不旋转玩家，因此符文朝向在运送前后保持不变。玩家
下平台后再滚动两格，以符文面朝下进入终点。

## 2. 玩家感受

玩家主动踏上平台，平台立即启动。短暂失去移动输入会带来新奇感，但平台始终沿清晰的
直线前进，对岸出口也在视野内，因此不会产生失控焦虑。到站后控制权立即归还，玩家
能把“被运送”理解为位置改变，而不是额外的翻滚或朝向变化。

## 3. 关卡布局

```rcmap
@level Chapter1_Scene4
@grid cell=1.00 height_step=0.25

@layer y=+0.00
      x-04 x-03 x-02 x-01 x+00 x+01 x+02 x+03 x+04 x+05 x+06
z+01 [#.] [#.] [#.] [#.] [..] [..] [..] [#.] [#.] [#.] [#.]
z+00 [#S] [#.] [#.] [#.] [=m] [..] [..] [#.] [#E] [#.] [#.]
z-01 [#.] [#.] [#.] [#.] [..] [..] [..] [#.] [#.] [#.] [#.]

@start S
at=(x=-04,z=+00,y=+0.00)
rune=UP

@end E
next=Chapter1_Scene5

@entity m
type=Elevator
name=carrier_platform
at=(x=+00,z=+00,y=+0.00)
target=(x=+02,z=+00,y=+0.00)
self_triggered=true
reset=false
move_duration=2.0s

@solution primary
route="R R R R RIDE(m) R R WAIT(2.0s)"
steps=6
start_rune=UP
end_rune=DOWN
end_at=E
assert=[carrier_platform.at_target,rune.DOWN]
```

布局约束：

- 起点平台为 `x=-04..-01`、`z=-01..+01`。
- 移动平台初始位于 `(x=+00,z=+00)`，与起点平台相邻。
- 平台目标位置为 `(x=+02,z=+00)`，与终点平台入口 `(x=+03,z=+00)` 相邻。
- `x=+01` 始终没有静态支撑，玩家不能靠普通滚动跨越。
- 终点平台为 `x=+03..+06`、`z=-01..+01`，`E` 位于 `(x=+04,z=+00)`。
- 平台在目标位置时不得与终点平台的碰撞体重叠。

## 4. 详细规则

### 4.1 登台与运送

玩家从 `S` 连续向右滚动四格，进入 `carrier_platform`。平台的 trigger 识别
`IExternallyControllable`，将玩家加入 riders，并因 `selfTriggered=true` 立即移动。

`RIDE(m)` 表示由机关完成的外部位移：

- 玩家从 `(x=+00,z=+00)` 移动到 `(x=+02,z=+00)`；
- 不计入 `steps`；
- 不改变玩家旋转或符文世界方向；
- 运送期间玩家输入被挂起；
- 抵达后调用 `EndExternalControl()` 并重新吸附位置。

### 4.2 离台与终点

平台抵达后，玩家向右滚动到终点平台入口，再向右滚动进入 `E`。玩家在登台前完成四次
向右翻滚，符文回到 `UP`；运送不改变朝向；离台后的两次向右翻滚使符文转为 `DOWN`。

平台配置为 `reset=false`，抵达后永久停留在目标位置。

## 5. 公式与解法验证

```text
platform_offset = (+2.00,0.00,0.00)
platform_move_duration = 2.0s
normal_roll_steps = 6
external_moves = 1

primary = R R R R RIDE(m) R R
```

状态验证：

```text
rune_after_four_rolls = UP
rune_after_RIDE = UP
rune_after_two_exit_rolls = DOWN
final_position = E
```

地图连通性检查确认：没有 `carrier_platform` 时，起点侧和终点侧属于两个不连通区域；
平台到达目标位置后，唯一连接为 `(x=+02) → (x=+03)`。

## 6. 边界情况

| 情况 | 处理方式 |
|------|----------|
| 玩家在平台移动时输入 | `IsExternallyControlled=true`，输入被忽略 |
| 玩家刚登台就尝试离开 | 平台立即取得控制，不接受新的正常滚动 |
| 玩家抵达后不离台 | 平台保持目标位置，等待玩家主动移动 |
| 玩家从平台侧边跌落 | 触发物理下落并通过 `killPlaneY` 复位 |
| 玩家错误朝向进入 `E` | 不通关，可在终点平台自由调整 |
| 平台已到站后再次进入 | `reset=false`，不会重复启动 |

## 7. 依赖

- `Elevator.cs`
  - `selfTriggered=true`；
  - riders 携带；
  - `reset=false`。
- `IExternallyControllable.cs`
  - 平台与玩家之间的控制权交接。
- `Player.cs`
  - `BeginExternalControl()` / `EndExternalControl()`；
  - 符文朝向保持。
- `SceneSwitcher.cs`
  - 全局符文终点规则。
- `mechanism-elevator.md`
  - 移动平台通用配置。

## 8. 可调参数

| 参数 | 建议值 | 影响 |
|------|--------|------|
| 平台水平位移 | 2 格 | 明确需要搭乘，但等待距离不过长 |
| `moveDuration` | `2.0s` | 提供可读的被运送体验 |
| 平台尺寸 | `1 × 1` 格 | 进入即触发，教学关系明确 |
| 终点平台尺寸 | `4 × 3` 格 | 提供安全离台和朝向调整空间 |

## 9. 验收标准

- [ ] Unity 场景的两岸、沟壑和平台坐标与 RCMap 一致。
- [ ] 玩家第四步进入平台后，平台立即开始水平移动。
- [ ] 运送期间输入被挂起，玩家不会从平台滑落或自行翻滚。
- [ ] `RIDE(m)` 前后玩家符文方向完全一致。
- [ ] 平台抵达 `(x=+02,z=+00)` 后立即归还玩家控制。
- [ ] 平台目标位置与终点平台入口无缝相邻。
- [ ] `R R R R RIDE(m) R R` 的六次正常滚动均合法。
- [ ] 第六次滚动进入 `E` 时符文面朝下。
- [ ] 正确停留 2 秒后加载 `Chapter1_Scene5`。
- [ ] 平台抵达后不复位、不重复移动。

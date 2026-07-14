# RollingCube1.0.1 → RollingCube1.0.2 迁移 TODO

迁移原则：**不直接照搬 1.0.1 代码**，而是理解其机关/关卡设计意图，按 1.0.2 已经确立的新架构重写；能直接复用的逻辑（如场景切换的正则规则）可以少改甚至原样搬。

---

## 已确认的架构决策

- **缓动动画：** 机关脚本（升降台/脆弱地板/传送门等）重新引入 **DOTween**。Player 自身滚动动画保留手写的绕 pivot 旋转数学，但插值参数 `t` 改由 `DOTween.To()` 驱动（`Ease.InOutSine`），换取缓动曲线和自动生命周期管理——已在 Phase 1 完成。
- **触发检测：** 所有机关脚本统一用 `other.GetComponent<Player>() != null` 判断玩家，替换 1.0.1 里 `other.gameObject.name == "Player"`（字符串比对，改名字就失效）和 `CompareTag("Player")`（依赖手动打 tag）两种写法。
- **网格模型：** 沿用 1.0.2 已有的「整数层级（`groundLevel`/`LevelToY`）+ `cubeHalfSize`」模型，**不使用** 1.0.1 的「0.25m 浮点坐标吸附」（`Mathf.Round(pos*100/25)*25/100`）。所有机关脚本的坐标/触发判断都要按新模型改写。
- **输入系统：** 沿用 1.0.2 已接入的新版 Input System（`Keyboard.current` + `wasPressedThisFrame`），机关触发继续用 `OnTriggerEnter`/`OnTriggerExit`。
- **攀爬机制：** 1.0.2 当前 `Player.cs` 已经实现的攀爬（`isStuck`/`ClimbStep`/`ClimbDownStep` 等）**本轮迁移期间不启用**。先将现有 `Player.cs` 改名备份（例如 `Player.WithClimb.cs.bak` 或挪到 `Assets/Script/_Archive/`），核心 `Player.cs` 重写为「纯滚动 + 推方块」（对应 1.0.1 的能力范围）。是否/何时重新启用攀爬、要和哪个机关结合，设计还没想清楚，先不排期，需要时另开讨论。

---

## Phase 1 — 核心移动重建

- [x] 备份当前 `Player.cs`（含攀爬逻辑）到归档文件，不删除 → `Assets/Script/_Archive/Player.WithClimb.cs.txt`
- [x] 重写 `Player.cs`：滚动移动（Input System + 整数层级模型 + 手写协程 Slerp），暂不含攀爬分支
- [x] 补上撞墙反馈（对应 1.0.1 `ShakeRandom` 抖动效果）→ `ShakeFeedback()`
- [x] 移植 `PushableBlock.cs`（机关 f），适配整数层级模型；修掉 1.0.1 里 `CanBePushed` 恒返回 `true` 的问题（新版 `IsOccupied()` 真正做了目标格检测）
- [x] 装回 DOTween 包（用户通过 Package Manager → My Assets 安装，1.0.1 里的 dll 实际缺失/未提交，改走这条路）
- [x] 顺手改造 `AnimateRoll`：保留绕 pivot 旋转数学，插值参数 `t` 改由 `DOTween.To()` 驱动 + `Ease.InOutSine`
- [x] **待验证：** 已在 Unity 编辑器里跑起来确认，前后左右移动正常，下落正常

## Phase 2 — 机关移植（复用 Phase 1 装好的 DOTween）

- [x] `SceneSwitcher.cs` — 场景推进，逻辑基本原样搬，玩家判定改成组件检测
- [x] `FragileGround.cs`（机关 c）— 脆弱地板：触发 → 延迟 → 渐隐 → 禁用；顺手删掉了文件顶部没在用、且在非 Editor 构建下会编译失败的 `using UnityEditor.Rendering;`
- [x] `Elevator.cs` / `LinkedElevator.cs` / `Elevators.cs`（机关 a）— 升降平台；修掉了 `recordElevators` 数组未初始化的空引用风险，做法是把「建空 GameObject 记录初始 transform」这个笨重写法换成直接存 `Vector3`/`Vector3[]`（Elevator/Elevators 都改了）；升降时长从硬编码的 `2` 抽成 `[SerializeField] moveDuration`
- [x] `ConveyorLogic.cs` + `ConveyorBeltAni.cs`（机关 b）— 传送带；`ConveyorLogic` 依赖的 `player.isBeingTransported`/`isControlLocked` 公共字段在新 `Player.cs` 里不存在了，改成新增的 `Player.BeginExternalControl()` / `EndExternalControl()` / `IsExternallyControlled` 接口（`EndExternalControl` 顺带从当前位置反推 `groundLevel`，交接回正常滚动）；逐格移动从 `Vector3.MoveTowards` 逐帧循环改成 DOTween 驱动，和其它机关动画风格保持一致
- [x] `BridgeTrigger.cs` / `RisingTerrain.cs`（Scene2 专属机关）— `RisingTerrain` 原来 `isTriggered` 声明了但从没赋值为 `true`，导致重复触发保护形同虚设，这次修了；升起高度/时长/间隔也抽成了字段
- [x] `TeleportEffect.cs`（机关 d）— 移植分裂/重组特效；内部类名从 `CubeTeleporter` 改成 `TeleportEffect`（原来文件名和类名对不上）；删掉了仅用于编辑器手测的 `Update()` 自触发钩子，`StartTeleport()` 保留为公共入口。**传送门触发逻辑本轮仍未接入**——放到 Phase 4 搭关卡、确定传送门摆放和目标点时再做，不在这里加

## Phase 3 — Prefab 与美术资源

**暂停在这里，等你确认怎么继续** — 见下方"当前阻塞"。

- [ ] 重建 Player/PlayerCube 相关 prefab，对齐新的整数层级模型和碰撞体设置
- [ ] 迁移地形 prefab（SoilTerrain / GlassTerrain / ConveyorBeltTerrain 等）
- [ ] 检查材质/Shader 是否需要因 URP 版本差异（1.0.1: 14.0.9 → 1.0.2: 17.5.0）调整

### 当前阻塞

Prefab（`.prefab`）本质是 YAML，理论上能直接写文件，但里面大量引用其他资源的 GUID（材质、网格、脚本 meta），手写伪造这些引用很容易做出编辑器打不开或丢引用的坏文件，而且没法在没连 Unity 的情况下验证对不对。Phase 1/2 都是纯代码，风险可控；Phase 3 这种资源类操作风险高很多，建议要么你在编辑器里重新批准 Unity MCP 连接让我操作+校验，要么这部分由你在编辑器里手动搭，我可以旁边给指导。

## Phase 4 — 关卡搭建

- [ ] 参考 `LevelDesign.md` 的教学递进，用新机关脚本重新搭建 Scene1–5（不直接导入旧场景文件，因为底层网格模型变了）
- [ ] 更新 `Design.md` / `LevelDesign.md` 内容，反映新架构下的机关行为差异

## Phase 5 — 文档收尾

- [ ] 为 1.0.2 写一份 `CLAUDE.md`，记录新架构、已知问题、机关实现状态（可参考 1.0.1 CLAUDE.md 的格式）

---

## 参考：1.0.1 已知问题（迁移时顺手修复，不要照搬）

1. `Elevators.cs` — `recordElevators` 数组使用前未初始化
2. `ResetRotate()` — x/y/z 三轴都错误地使用了 `angles.x`（1.0.2 新模型下应该不会复现这个问题，重写时留意）
3. `PushableBlock.CanBePushed()` — 恒返回 `true`，射线检测逻辑被注释掉
4. 传送门触发逻辑未与 `TeleportEffect.cs` 特效集成

# GDD — 暂停菜单（Pause Menu）

## 1. 概述

项目目前没有任何暂停能力——Esc 键未被占用，一旦进入关卡就只能一直玩下去或强行退出。本机制为 Player 接入 Esc 触发的暂停弹框：按下 Esc 将 Time.timeScale 冻住为 0、弹出包含三个按钮（继续/重置/回到主界面）的面板；再按一次 Esc 或点击"继续"则恢复游戏。因为项目里还没任何交互式 UI（按钮/EventSystem），本机制也会引入项目的第一个 EventSystem，并新建一个最简单的 MainMenu 占位场景作为"回到主界面"的跳转目标。

---

## 2. 玩家感受

"随时可以暂停，不用担心那一刻正在滚动或下跌的方块继续动作"——按下 Esc 的那一帧，整个世界完全冻住，没有任何"再走一小步"的缝隙，给人"随时可以安全离开"的安心感。面板本身不需要花里胡哨的过度动效，直接出现/消失，因为它的作用是"断开"而不是"过渡"。重置/回到主界面这两个比较"重"的操作放在继续之后，避免恐惧误点。

---

## 3. 详细规则

- 每帧检测 `Keyboard.current.escapeKey.wasPressedThisFrame`（`Update()` 不受 `Time.timeScale` 影响，因此暂停状态下依然能检测到 Esc 用于恢复）
- 按下 Esc 时切换 `isPaused`：
  - 变为 `true`：`Time.timeScale = 0`，面板 `SetActive(true)`
  - 变为 `false`：`Time.timeScale = 1`，面板 `SetActive(false)`
- 面板包含三个按钮，从上到下依次为：**继续 → 重置 → 回到主界面**
  - **继续**：等价于再按一次 Esc（`isPaused = false`，恢复 `timeScale`，隐藏面板）
  - **重置**：`Time.timeScale = 1` → `SceneManager.LoadScene(当前场景名)`，重新加载整个关卡场景
  - **回到主界面**：`Time.timeScale = 1` → `SceneManager.LoadScene("MainMenu")`
- `Player.cs` 的 `Update()` 新增暂停判断：暂停期间跳过按键检测，不进入 `TryMove`，避免 `Update()` 在 `timeScale = 0` 时仍正常运行、导致玩家在暂停期间排队或触发新的翻滚
- 面板开/关无动画，直接 `SetActive(true/false)`（与本项目其余机关普遍使用 DOTween 动效不同——面板的作用是瞬时断开，不是过渡，见「玩家感受」）
- 架构：新增 `PauseMenu.cs` 单例，与 `StepCounter.cs` 同样通过 `[RuntimeInitializeOnLoadMethod]` 启动并 `DontDestroyOnLoad`，从 `Resources` 加载 `PauseMenuCanvas.prefab`；该 prefab 包含项目里第一个 `EventSystem`、一个带 `GraphicRaycaster` 的 `Canvas`、一层全屏半透明背景遮罩，以及三个按钮
- 新建 `Assets/Scenes/MainMenu.unity` 占位场景（背景 + 标题文字，暂无按钮逻辑），并加入 Build Settings，作为"回到主界面"当前唯一可跳转的目标；正式的开始游戏界面设计会在此场景基础上补充

---

## 4. 公式

```
触发检测：Keyboard.current.escapeKey.wasPressedThisFrame（每帧检测，不受 Time.timeScale 影响）
状态切换：isPaused = !isPaused

isPaused == true  → Time.timeScale = 0，面板 SetActive(true)，Player.Update() 跳过按键检测
isPaused == false → Time.timeScale = 1，面板 SetActive(false)，Player.Update() 恢复正常

"重置"    ：Time.timeScale = 1 → SceneManager.LoadScene(当前场景名)
"回到主界面"：Time.timeScale = 1 → SceneManager.LoadScene("MainMenu")
```

---

## 5. 边界情况

| 情况 | 处理方式 |
|------|----------|
| Esc 按下时玩家方块正处于翻滚/下落动画中 | `AnimateRoll`/`LandWhenSettled` 里的 DOTween 动画默认受 `Time.timeScale` 影响，`timeScale = 0` 会让动画自然定格在当前插值位置；恢复后从冻结点继续播放，不需要额外处理 |
| 暂停期间 `Player.Update()` 仍每帧运行 | `Update()` 不受 `timeScale` 影响，因此显式加 `isPaused` 判断阻止处理新的方向键输入，避免暂停期间排队新的翻滚 |
| "重置"/"回到主界面"点击时 `Time.timeScale` 仍是 0 | 必须在调用 `SceneManager.LoadScene` 之前显式把 `timeScale` 恢复为 1，否则新场景加载后依然停留在冻结状态 |
| 在 `MainMenu.unity` 占位场景按 Esc | `PauseMenu` 是全局单例，占位场景里同样会弹出面板（"回到主界面"/"重置"作用于 MainMenu 自身）；当前阶段视为占位场景的已知行为，不做屏蔽，等正式开始界面设计时再决定是否需要禁用 |
| 玩家被机关（Elevator/Conveyor）携带或 `ShakeFeedback` 抖动动画播放时按下 Esc | 这些协程同样依赖 `Time.deltaTime`/DOTween，`timeScale = 0` 会让它们自然定格，恢复后继续播放，不需要额外处理 |
| 快速连续按 Esc（例如双击） | 每次按下都是一次开关切换，不做防抖/去重；`SetActive` 状态天然幂等，不会出现异常状态 |

---

## 6. 依赖

- **Player.cs** — `Update()` 新增暂停判断，读取 `PauseMenu` 暴露的暂停状态后跳过按键检测
- **StepCounter.cs** — "重置"/"回到主界面"都会触发 `SceneManager.LoadScene`，其现有的 `OnSceneLoaded` 钩子会自然清零 Step/Dead 计数，本机制不需要重复处理
- **SceneSwitcher.cs** — 场景命名规则（`Chapter{n}_Scene{m}`）用于"重置"时取得当前场景名并重新加载
- **Assets/Scenes/MainMenu.unity**（本机制新建的占位场景） — "回到主界面"当前唯一的跳转目标
- **design/gdd/（待建的"开始游戏界面" GDD）** — 会在 `MainMenu.unity` 基础上补充实际内容，那份文档需要回链本文档，说明它复用的是本机制新建的占位场景而非从零新建

---

## 7. 可调参数

| 参数 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| 暂停触发键 | 写死在 `PauseMenu.cs` 里 | Esc | 当前直接硬编码检测 `escapeKey`，未来若需要支持重新绑定需接入 Input System 的 Action Asset |
| 主界面场景名 | 写死在 `PauseMenu.cs` 里的常量字符串 | `"MainMenu"` | "回到主界面"跳转的目标场景名；与 `StepCounter.cs` 里 `CanvasPrefabResourcePath` 常量同样的写法，集中在一处方便之后修改 |

---

## 8. 验收标准

- [ ] 关卡内按下 Esc，游戏世界立即完全冻结（物理、DOTween 动画、玩家输入均停止响应新操作）
- [ ] 暂停面板显示三个按钮，从上到下依次为"继续"、"重置"、"回到主界面"
- [ ] 再按一次 Esc 或点击"继续"，游戏立即恢复正常运行，之前冻结的动画从冻结点继续播放
- [ ] 点击"重置"，当前关卡场景重新加载，Step/Dead 计数归零，`Time.timeScale` 恢复为 1
- [ ] 点击"回到主界面"，加载 `MainMenu` 占位场景，`Time.timeScale` 恢复为 1
- [ ] 暂停期间按 WASD/方向键不会让方块排队或立即执行新的翻滚
- [ ] `MainMenu.unity` 已加入 Build Settings，可以被 `SceneManager.LoadScene` 正确加载

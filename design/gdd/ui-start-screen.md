# GDD — 开始界面（章节 / 关卡选择 + 存档解锁）

## 1. 概述

`MainMenu.unity` 是正式的章节与关卡选择界面：进入后显示章节列表，点击章节后显示关卡网格，
已解锁关卡可直接进入，后续关卡显示为禁用。解锁进度保存在本地并跨启动保留。

---

## 2. 玩家感受

"打开游戏，一眼看到自己走到了哪一章、哪一关，想重玩哪一关就点哪一关"——章节/关卡选择不是障碍而是进度地图：已解锁的关卡随时可以重进（不限定只能玩"最新"的一关），未解锁的关卡清楚地灰掉，不会让玩家误以为卡关或者游戏坏了。整个导航是"平铺"的层级（章节→关卡→关卡场景），每一层都能用同一个"返回"动作（点击按钮或按 Esc）退回上一层，不会把玩家困在某个面板里出不去。

---

## 3. 详细规则

### 3.1 场景与面板结构

- `MainMenu.unity` 加载后直接显示**章节选择面板**（根面板）：标题文字（沿用现有 `TitleText`）+ 章节按钮列表。当前只有一个章节，显示 `[章节 1]` 一个按钮。
- 点击章节按钮 → 切换到**关卡选择面板**：3×3 按钮网格（对应 `Chapter1_Scene1`~`Scene9`），加一个"返回"按钮。面板切换无动画，直接 `SetActive(true/false)`（与暂停菜单面板的显隐方式一致）。
- 关卡按钮：
  - 已解锁：可点击，点击后 `Time.timeScale` 恢复为 1（以防万一）并 `SceneManager.LoadScene("Chapter1_Scene{n}")`。
  - 未解锁：`Button.interactable = false`（Unity 按钮组件自带的灰色禁用反馈色），数字文字照常显示，只是不可点击、没有高亮/按下反馈。
- "返回"按钮：从关卡选择切回章节选择面板。

### 3.2 Esc 键行为

- 关卡选择面板可见时，按 Esc 等价于点击"返回"（切回章节选择面板）。
- 章节选择（根）面板可见时，按 Esc 不做任何事（已经是最外层，没有更上一层可退）。
- 这个判断由新建的 `MainMenu.cs` 自己处理，与 `PauseMenu.cs` 的暂停开关是**两套独立的 Esc 处理**：`PauseMenu.cs` 检测到当前场景名等于自己已有的 `MainMenuSceneName` 常量时，直接跳过暂停开关逻辑（不弹暂停面板、不冻结时间），把 Esc 完全让给 `MainMenu.cs`。

### 3.3 关卡完成 / 解锁

- 复用 `SceneSwitcher.cs` 现有的"玩家在终点触发器停留满 `requiredDwellSeconds`"判定——这一直是代码里唯一的"关卡已通过"信号。在该判定成立、即将 `LoadScene` 进入下一关之前，新增一行调用 `LevelProgress.Instance.RegisterCompletion(chapter, scene)`（`chapter`/`scene` 就是 `SceneSwitcher` 已经用正则解析出来的当前章节/关卡号，不需要重新解析）。
- 解锁规则：每章第 1 关永远解锁；第 N 关（N>1）解锁当且仅当第 N-1 关已被标记为完成。
- 章节选择面板里的章节按钮本身不受解锁限制（当前只有 1 个章节，永远可点击）；未来如果要给章节本身加解锁条件，属于新的决策，本机制不处理。

### 3.4 存档

- 新建 `LevelProgress.cs` 单例，启动方式与 `StepCounter.cs`/`PauseMenu.cs` 一致：`[RuntimeInitializeOnLoadMethod]` 创建宿主 GameObject + `DontDestroyOnLoad`。
- 存档文件路径：`Application.persistentDataPath + "/levelprogress.json"`。
- 序列化方式：Unity 内置 `JsonUtility`（项目未引入 Newtonsoft.Json）。`JsonUtility` 不支持直接序列化 `Dictionary`，因此存档数据结构是一个可序列化的条目列表：
  ```csharp
  [Serializable] class LevelEntry { public string levelId; public bool completed; }
  [Serializable] class SaveData { public List<LevelEntry> levels; }
  ```
  运行时加载后转成 `Dictionary<string, bool>` 存在内存里，`levelId` 格式与场景名一致，例如 `"Chapter1_Scene3"`。
- 启动时（`LevelProgress.Awake`）尝试读取存档文件；文件不存在（例如第一次启动）则视为空存档——此时除了每章第 1 关，其余全部关卡按解锁规则计算为未解锁。
- `RegisterCompletion(chapter, scene)` 把对应 `levelId` 标记为 `completed = true` 后立即回写整份存档到磁盘（写穿，不做延迟批量写入），避免异常退出/崩溃丢失刚刚打完的这一关。
- `IsUnlocked(chapter, scene)` 供 `MainMenu.cs` 在铺关卡按钮网格时逐个调用，决定每个按钮的 `interactable`。

### 3.5 Build Settings

- `MainMenu` 与 `Chapter1_Scene1`~`Chapter1_Scene9` 均必须保持在 Build Settings 中，确保
  关卡选择和顺序通关都能按场景名加载。

---

## 4. 公式

```
关卡 levelId 格式：Chapter{chapter}_Scene{scene}（与 SceneSwitcher.cs 的正则 ^Chapter(\d+)_Scene(\d+)$ 一致）

解锁判定：
IsUnlocked(chapter, scene):
    if scene == 1: return true
    return completed[$"Chapter{chapter}_Scene{scene-1}"] == true   // 缺失视为 false

完成记录：
RegisterCompletion(chapter, scene):
    completed[$"Chapter{chapter}_Scene{scene}"] = true
    立即回写存档 JSON 到 Application.persistentDataPath + "/levelprogress.json"

章节/关卡数据来源（硬编码，非配置资产）：
chapterCount = 1
levelsPerChapter[1] = 9
```

**示例**：全新存档（`completed` 为空）→ `IsUnlocked(1,1)==true`，`IsUnlocked(1,2)==false`。玩家通过 `Chapter1_Scene1` 后 `RegisterCompletion(1,1)` 写入 → 再次打开关卡选择，`IsUnlocked(1,2)==true`，`IsUnlocked(1,3)` 仍为 `false`。

---

## 5. 边界情况

| 情况 | 处理方式 |
|------|----------|
| 存档文件不存在（第一次启动） | `LevelProgress` 视为空存档，只有每章第 1 关解锁，不报错、不弹提示 |
| 存档文件存在但内容损坏/无法解析 | `JsonUtility.FromJson` 解析失败时捕获异常，回退为空存档（等同于第一次启动），并 `Debug.LogWarning` 记录一次；不覆盖/删除损坏文件本身 |
| 玩家在关卡选择面板按 Esc | 等价于点击"返回"，回到章节选择面板；不会触发暂停菜单 |
| 玩家在章节选择（根）面板按 Esc | 不做任何事；`PauseMenu.cs` 因为场景名匹配 `MainMenuSceneName` 也不会弹出暂停面板 |
| 玩家从关卡场景里点击暂停菜单的"回到主界面" | 正常 `LoadScene("MainMenu")`，落地后是章节选择根面板（不会记住之前展开到哪个章节/关卡选择面板） |
| 点击一个已解锁但存档里对应章节还没建过（未来多章节时可能出现的数据缺口） | 当前只有 1 个章节、9 个关卡，硬编码范围内不会出现这种缺口；未来加章节时需要重新评估 |
| 通过最后一关（`Chapter1_Scene9`） | `RegisterCompletion(1,9)` 正常写入；此时没有 `Chapter1_Scene10`，`IsUnlocked` 不会被再往后查询，`SceneSwitcher` 自身在找不到下一关时已有 `Debug.LogWarning` 且不跳转（既有行为，本机制不改动） |
| 关卡按钮网格里某个按钮对应的场景恰好是当前 `SceneManager.GetActiveScene()`（即从关卡内点"回到主界面"又选回同一关） | 不做特殊处理，`SceneManager.LoadScene` 重新加载同一个场景是合法操作，效果等同于暂停菜单的"重置" |

---

## 6. 依赖

- **SceneSwitcher.cs** — 在其现有的关卡完成判定分支里新增一行 `LevelProgress.Instance.RegisterCompletion(chapter, scene)` 调用；不改变其原有的下一关跳转逻辑
- **PauseMenu.cs** — 新增"当前场景名等于 `MainMenuSceneName` 时跳过 Esc 处理"的判断，把 Esc 让给本机制的 `MainMenu.cs`；[[ui-pause-menu]] 需要回链本文档说明这个联动
- **Assets/Scenes/MainMenu.unity** — 本机制的落地场景，在既有的 `TitleText`/`Background` 基础上新增章节/关卡选择面板层级
- **ProjectSettings/EditorBuildSettings.asset** — 注册 `MainMenu` 与 Chapter 1 全部 9 个场景
- **LevelProgress.cs** — 存档单例，供 `SceneSwitcher.cs` 和 `MainMenu.cs` 调用
- **MainMenu.cs** — 负责面板切换、关卡按钮、解锁状态和 Esc 返回导航

---

## 7. 可调参数

| 参数 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `chapterCount` | int（硬编码常量） | 1 | 当前章节总数，写死在 `MainMenu.cs` 里；加新章节时需要改代码 |
| `levelsPerChapter` | int（硬编码常量，按章节） | 9（Chapter1） | 每章关卡数，写死在 `MainMenu.cs` 里 |
| 存档文件名 | 写死在 `LevelProgress.cs` 里的常量字符串 | `"levelprogress.json"` | 与 `StepCounter.cs`/`PauseMenu.cs` 的资源路径常量同样的写法，集中一处方便之后修改 |
| 关卡网格列数 | 写死在 `MainMenu.cs` 里 | 3（3×3 网格，对应 9 关） | 关卡数变化时需要同步调整网格布局 |

---

## 8. 验收标准

- [ ] 进入 `MainMenu.unity`，直接看到章节选择面板（标题 + `[章节 1]` 按钮），没有多余的"开始游戏"按钮
- [ ] 点击章节按钮，切换到关卡选择面板，显示 3×3 共 9 个关卡按钮 + 一个"返回"按钮
- [ ] 全新存档（无 `levelprogress.json`）时，只有关卡 1 可点击，关卡 2~9 显示为灰色且无法点击
- [ ] 通关 `Chapter1_Scene1` 后返回主界面，关卡 2 变为可点击，关卡 3~9 仍是灰色
- [ ] 关闭游戏进程后重新启动，之前的解锁进度依然保留（不重置回只解锁第 1 关）
- [ ] 点击已解锁的关卡按钮，正确加载对应的 `Chapter1_SceneN` 场景
- [ ] 关卡选择面板按 Esc，行为等同于点击"返回"，回到章节选择面板；章节选择面板按 Esc 无任何反应，且不会弹出暂停菜单
- [ ] 从关卡内暂停菜单点击"回到主界面"进入 `MainMenu`，按 Esc 不会弹出暂停菜单（确认 `PauseMenu.cs` 的场景名判断生效）
- [x] `Chapter1_Scene8`、`Chapter1_Scene9` 已加入 Build Settings

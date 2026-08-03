# ADR-0001: Seamless Menu and Level Flow

## Status

Accepted

## Date

2026-08-02

## Engine Compatibility

| Field | Value |
|-------|-------|
| **Engine** | Unity 6000.5.3f1 |
| **Domain** | Core / UI / Animation |
| **Knowledge Risk** | MEDIUM — the repository does not contain the expected `docs/engine-reference/unity/` library |
| **References Consulted** | `AGENTS.md`, `.claude/docs/technical-preferences.md`, current Unity Editor state, existing gameplay/UI scripts and Chapter 1 scenes |
| **Post-Cutoff APIs Used** | None; the design uses established `SceneManager` additive loading, `ScriptableObject`, Unity physics, uGUI, and DOTween APIs |
| **Verification Required** | Unity compilation, additive scene lifecycle, direct-scene Play Mode, duplicate Camera/AudioListener prevention, frozen-time transitions, Chapter 1 completion routes |

## ADR Dependencies

| Field | Value |
|-------|-------|
| **Depends On** | None |
| **Enables** | Data-driven chapter expansion and future menu art replacement |
| **Blocks** | Seamless menu/level-flow implementation until this decision is accepted |
| **Ordering Note** | Runtime flow and catalog must compile before the editor builder can migrate scenes |

## Context

### Problem Statement

The current start screen is a UI-only `MainMenu` scene. Selecting a level and completing a
level both call `SceneManager.LoadScene`, producing visible scene boundaries. Chapter 1 scenes
also duplicate cameras and shared environment settings. The desired experience is a live 3D
level behind the menu, animated preview switching, instant confirmation into gameplay, and an
animated transition to the next level without a conventional loading screen.

### Constraints

- Chapter 1 RCMap layouts and verified solutions must remain unchanged.
- The player must remain visible while level content transitions.
- Each level must still support being opened and played directly in the Unity Editor.
- Existing terrain, player, goal, and box prefabs remain authoritative.
- Level transition animation operates on direct children of `LevelRoot`.
- Gameplay and physics must not advance while a preview or transition is active.
- The current project uses uGUI, the Input System, URP, and DOTween.
- Placeholder menu art is acceptable; the structure must be easy to reskin later.

### Requirements

- Display a live selected level behind the chapter/level menu.
- Separate level selection/preview from the explicit Start action.
- Preserve the last selected level, otherwise preview the highest unlocked level.
- Animate old content outward from the player before/in parallel with new content entering.
- Start the incoming animation after a configurable one-second overlap when loading is ready.
- Reuse one persistent player in normal game flow.
- Support standalone level testing with a generated temporary player and fallback scene rig.
- Return to the menu after the final registered level.
- Route pause-menu reset and return actions through the same seamless flow.

## Decision

`MainMenu` becomes the persistent `GameShell`. It owns the main camera, shared lighting and
environment, uGUI, persistent player, catalog, and `GameFlowController`. Level scenes are loaded
additively and contribute level-specific runtime content through a `LevelContext`.

Each `LevelContext` identifies the level, exposes its `LevelRoot` and `LevelSpawn`, supplies
preview/gameplay camera framing, and owns a `StandaloneRig`. When a level is opened directly,
the context enables the standalone rig and creates a temporary player. When loaded by the
GameShell, the standalone rig stays disabled and the context uses the persistent player.
The standalone rig is authored inactive so an additive scene can never briefly enable a second
Camera, AudioListener, light, or Player during scene activation.

The persistent player does not move during a level-to-level transition. Instead, the incoming
`LevelRoot` is translated so that its `LevelSpawn` aligns with the player's current position.
This lets both the outgoing and incoming ripples use the same visible center without a camera
or player teleport.

Alignment and player injection happen synchronously from `LevelContext.Awake`, through a pending
load request registered with `GameFlowController`, before any level `Start` method can cache world
positions. The animator never auto-starts in GameShell flow. This is required for elevators and
other mechanisms that record world-space state during `Start`.

`LevelEntryAnimator` is extended into a bidirectional transition component while retaining its
existing type and serialized connections. Direct children of `LevelRoot` are transition units.
Exit uses a distance-staggered scale-to-zero tween with `Ease.InBack`; entry uses scale-from-zero
with `Ease.OutBack`. All transition, camera, and menu tweens use unscaled time.

`SceneSwitcher` remains the authority for the rune-down dwell condition, but delegates completed
flow to `GameFlowController` when the GameShell is active. In standalone mode it retains a
single-scene loading fallback for direct testing.

The GameShell scene stays active during normal flow so shared RenderSettings remain authoritative.
Code must never infer the current level from `SceneManager.GetActiveScene`; the current
`LevelContext` is the only level identity. All persistent runtime objects are explicitly owned by
the GameShell. Before simulation resumes, the flow makes dynamic bodies safe during translation,
calls `Physics.SyncTransforms()`, and only then restores scaled time.

### Architecture Diagram

```text
MainMenu / GameShell (persistent active scene)
├── MainMenuCanvas
├── Main Camera + shared environment
├── persistent Player
├── LevelCatalog
└── GameFlowController
    ├── currently loaded LevelContext
    ├── outgoing LevelContext (during overlap)
    └── incoming LevelContext (during overlap)

Additive level scene
├── LevelContext
├── StandaloneRig (disabled during GameShell flow)
├── LevelSpawn
└── LevelRoot
    ├── terrain tile
    ├── goal
    ├── box
    └── mechanism root
```

### Key Interfaces

- `GameFlowController.SelectLevel(string levelId)` queues a preview selection.
- `GameFlowController.StartSelectedLevel()` transitions preview into active gameplay.
- `GameFlowController.CompleteLevel(LevelContext level)` records completion and advances.
- `GameFlowController.ResetCurrentLevel()` recreates the current level through the transition.
- `GameFlowController.ReturnToMenu()` restores a clean preview of the current level.
- `LevelContext.AlignSpawnTo(Vector3 worldPosition)` translates level content to the player.
- `LevelEntryAnimator.PlayEnter(...)` and `PlayExit(...)` expose completion callbacks.
- `Player.PrepareForLevel(...)` kills player tweens/coroutines, resets physics, orientation,
  scale, spawn, kill plane, and external-control state.
- Player external control is nestable/owned; one subsystem cannot unlock a player still held by
  another subsystem.
- `LevelCatalog` is the single source for chapters, levels, unlock order, scene names, and display data.

## Alternatives Considered

### Alternative 1: Continue Single-Scene Loading

- **Description**: Keep one Unity scene per level and decorate `LoadScene` with a fade.
- **Pros**: Minimal refactor and simple lifecycle.
- **Cons**: Cannot provide an already-loaded live menu preview or overlap outgoing/incoming levels.
- **Rejection Reason**: Does not meet the seamless preview and transition requirements.

### Alternative 2: Put Every Level in One Scene

- **Description**: Store all Chapter 1 levels as disabled roots in a single scene.
- **Pros**: Instant switching after startup and no additive lifecycle.
- **Cons**: Increasing memory use, cluttered authoring, duplicated objects, and poor future chapter scalability.
- **Rejection Reason**: Couples content growth to one scene and weakens independent level testing.

### Alternative 3: Rebuild Levels Dynamically from RCMap at Runtime

- **Description**: Parse level data and instantiate all tiles/mechanisms at runtime.
- **Pros**: Compact content representation and no additive scenes.
- **Cons**: Large runtime-authoring rewrite, difficult mechanism serialization, and unnecessary risk for existing authored scenes.
- **Rejection Reason**: The current builder and scenes already provide reliable authored content.

## Consequences

### Positive

- Selecting a preview and confirming Start no longer requires another scene load.
- Completion transitions can overlap old and new level content.
- One player, camera, and shared environment remove duplicate ownership in normal flow.
- `LevelCatalog` makes future chapters and replacement art data-driven.
- Direct level Play Mode remains supported.

### Negative

- Scene lifecycle becomes more complex than single-scene loading.
- Existing systems that infer the current level from the active scene must use `LevelContext`.
- All nine Chapter 1 scenes and their builder must be migrated together.
- Menu and transition tweens must consistently use unscaled time.

### Risks

- Additive loading can briefly activate duplicate cameras or AudioListeners.
  Mitigation: `StandaloneRig` is authored inactive and is enabled only by direct-play bootstrap.
- An elevator can cache positions before the incoming level is aligned.
  Mitigation: `LevelContext.Awake` performs pending alignment before any `Start` method runs.
- Additive `sceneLoaded` callbacks can reset global UI at the wrong time.
  Mitigation: step/death counters reset only on explicit flow events.
- Physics objects can advance during preview.
  Mitigation: the flow state owns `Time.timeScale` and keeps it zero outside gameplay.
- An unfocused standalone window or Unity Editor can stop advancing transition coroutines.
  Mitigation: the persistent GameShell enables `Application.runInBackground`; player input
  remains locked and `Time.timeScale` remains zero during preview/transition states.
- A failed load can leave an empty background.
  Mitigation: retain the outgoing context until the incoming context is validated; restore it on failure.
- Nested hierarchy scaling can double-transform mechanisms.
  Mitigation: animate only direct `LevelRoot` children.
- Persistent player respawn data can point at a previous level.
  Mitigation: `Player.PrepareForLevel` replaces position, rotation, physics state, scale,
  kill-plane reference, tweens, coroutines, and control locks.
- A transition can unlock a player still owned by an elevator or conveyor.
  Mitigation: use nestable/owned control locks and clear obsolete owners during level preparation.
- Physics broadphase can retain pre-translation collider transforms while time is frozen.
  Mitigation: translate only while simulation is frozen, make dynamic bodies safe, then call
  `Physics.SyncTransforms()` before restoring gameplay.

## GDD Requirements Addressed

| GDD System | Requirement | How This ADR Addresses It |
|------------|-------------|--------------------------|
| `ui-start-screen.md` | Chapter/level selection, unlock state, direct entry | Replaces direct `LoadScene` with live additive preview plus explicit Start |
| `ui-pause-menu.md` | Resume, reset, and return actions | Routes reset/return through `GameFlowController` |
| `chapter-01.md` | Preserve Chapter 1 progression and rune-down victory | Leaves gameplay maps and dwell validation unchanged |
| `level-map-schema.md` | Preserve RCMap-relative coordinates and standard solutions | Translates the complete `LevelRoot` without changing internal coordinates |

## Performance Implications

- **CPU**: Short-lived DOTween work proportional to the number of direct `LevelRoot` children.
- **Memory**: At most two lightweight content scenes coexist during transition; other levels remain unloaded.
- **Load Time**: Additive async loading is hidden behind the outgoing animation when possible.
- **Network**: None.

## Migration Plan

1. Add `LevelCatalog`, `LevelContext`, flow-state types, nestable player control, and full player reset support.
2. Extend `LevelEntryAnimator` with unscaled enter/exit operations.
3. Add `GameFlowController` and update menu, completion, pause, and counter integrations.
4. Update the Chapter 1 scene builder to author contexts, spawn markers, standalone rigs, and flat transition children.
5. Convert `MainMenu` into the GameShell and create the Chapter 1 catalog.
6. Rebuild all nine Chapter 1 scenes from the existing RCMap builder.
7. Update design documents and PlayMode coverage.
8. Verify compilation, scene structure, menu previews, completion flow, last-level return, and standalone levels in Unity.

## Validation Criteria

- Main Menu shows a live default level preview and a two-level chapter/level card flow.
- Selecting a different unlocked level animates old content out and new content in.
- Start hides the menu and moves the camera without another scene load.
- Completing a level preserves the player and seamlessly advances.
- The final level returns to the menu.
- Reset and return-to-menu restore a clean level state.
- Rapid selections resolve to the final requested level without leaked scenes.
- Every Chapter 1 scene can still be opened and played directly.
- Existing primary routes remain valid.
- Unity Console contains no compilation errors, duplicate AudioListener warnings, or leaked-scene warnings.
- Elevators and other mechanisms cache their positions only after incoming-level alignment.
- Physics queries use the aligned collider positions on the first playable frame.

## Related Decisions

- `design/gdd/ui-start-screen.md`
- `design/gdd/ui-pause-menu.md`
- `design/gdd/chapter-01.md`
- `design/gdd/level-map-schema.md`

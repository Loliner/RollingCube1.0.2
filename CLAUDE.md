# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project overview

RollingCube is a Unity puzzle game (URP) where the player rolls a cube around a level built on an
integer grid, pushes blocks, and interacts with mechanisms (elevators, conveyors, fragile ground,
rising terrain, teleporters). This is a from-scratch architecture rewrite of an earlier `1.0.1`
version — see `MIGRATION.md` for the authoritative, up-to-date status of what has been migrated,
what decisions were made and why, and what is still pending. Always check `MIGRATION.md` before
assuming a feature (e.g. climbing) is enabled.

- Unity Editor version: `6000.5.3f1` (see `ProjectSettings/ProjectVersion.txt`) — open/build with this version.
- Render pipeline: URP `17.5.0`.
- Input: new Input System (`Unity.InputSystem`), read via `Keyboard.current` / `wasPressedThisFrame` — not the legacy `Input` class.
- Tweening: DOTween (`Assets/Plugins/Demigiant/DOTween`), used for all mechanism animation and for driving the player's roll interpolation.

## Commands

There is no CLI build/test pipeline in this repo (no README or scripts define one) — this is worked
on entirely through the Unity Editor:

- Open the project with Unity Hub / Unity Editor `6000.5.3f1` and press Play to test.
- Test scaffolding exists (`Assets/Tests/EditMode/RollingCube.EditMode.Tests.asmdef`,
  `Assets/Tests/PlayMode/RollingCube.PlayMode.Tests.asmdef`, using `com.unity.test-framework`), but
  no test files have been written yet. Once tests exist, run them via **Window → General → Test
  Runner** in the Editor.
- Gameplay scripts live in the `RollingCube` assembly (`Assets/Script/RollingCube.asmdef`), which
  references `Unity.InputSystem`.

## Architecture

### Grid model

Everything is built on an integer-level grid, not free-floating floats:

- A cube cell is `cubeHalfSize` (default `0.5`) half-extent; world positions are snapped to cell
  centers via `SnapToGrid()` (round to nearest integer + half-size offset), duplicated in each
  script that needs it (`Player.cs`, `PushableBlock.cs`).
- Vertical position is addressed by integer `groundLevel`, converted to world Y via
  `LevelToY(level) = level * cubeHalfSize*2 + cubeHalfSize`.
- This intentionally replaces 1.0.1's `0.25m` float-snapping model — don't reintroduce float
  snapping when porting old mechanism logic.

### Player movement (`Assets/Script/Player.cs`)

- Kinematic `Rigidbody` driven by hand-rolled input polling in `Update()` (WASD/arrows), one 90°
  roll per keypress via `StartCoroutine(TryMove(direction))`.
- `AnimateRoll()` does the actual roll: pivot/axis math is manual (rotate around the bottom edge in
  the movement direction), but the interpolation parameter `t` is driven by `DOTween.To()` with
  `Ease.InOutSine` rather than a hand-written lerp — this pairs manual geometry with DOTween-managed
  timing/lifecycle. Follow this pattern for any new player animation rather than a raw coroutine lerp.
- Blocked moves (wall or unpushable block ahead) play `ShakeFeedback()` instead of moving.
- After a successful roll, `FinishAfterRoll()` checks `HasSupportBelow()` (short downward raycast);
  if unsupported it calls `StartFalling()`, which flips the `Rigidbody` to non-kinematic and lets
  physics take over — there's no scripted fall animation.
- `BeginExternalControl()` / `EndExternalControl()` / `IsExternallyControlled` let a mechanism (e.g.
  `ConveyorLogic`) take direct ownership of the transform while suspending input polling.
  `EndExternalControl()` re-derives `groundLevel` from wherever the mechanism left the cube, so
  normal rolling resumes cleanly — always call it when handing control back, don't just stop moving
  the transform.
- **Climbing is currently disabled.** The previous climb-capable implementation (`isStuck`,
  `ClimbStep`, `ClimbDownStep`, etc.) is archived at
  `Assets/Script/_Archive/Player.WithClimb.cs.txt` for reference, not compiled. `Climbable.cs`
  (a `heightUnits` marker component) exists but isn't wired to anything yet. Don't reintroduce climb
  logic into `Player.cs` without checking `MIGRATION.md` — the design for how it reintegrates with
  mechanisms hasn't been decided.

### Mechanism convention

All mechanism scripts (`PushableBlock`, `SceneSwitcher`, `FragileGround`, `Elevator` /
`LinkedElevator` / `Scene4/Elevators`, `ConveyorLogic`, `Scene2/BridgeTrigger`,
`Scene2/RisingTerrain`, `TeleportEffect`) share conventions:

- Player detection is always `other.GetComponent<Player>() != null` via `OnTriggerEnter`/`OnTriggerStay`/`OnTriggerExit`
  — never tag or name comparison. This replaces two different string-based checks used in 1.0.1.
- Timed/animated behavior is a `StartCoroutine` that drives DOTween tweens (`transform.DOMove`,
  `DORotate`, `DOScale`, ...) and waits on either `WaitForSeconds` or a `bool done` flag flipped in
  `OnComplete`, rather than per-frame manual lerping.
- `Elevator` is a base class with `virtual OnStartAnimation()/OnResetAnimation()` hooks;
  `LinkedElevator` overrides these to move a second, linked object in sync — follow this pattern
  (subclass + override) rather than adding branching flags to `Elevator` itself for new linked
  behavior. `Scene4/Elevators.cs` is a separate, non-inheriting script for animating a whole array of
  elevators together (different enough shape that it wasn't folded into the `Elevator` hierarchy).
  `Scene2/` and `Scene4/` hold mechanism scripts specific to those levels.
- `ConveyorLogic` hands the player off between adjacent conveyor segments by physically overlap-
  testing for the next `ConveyorLogic` in `forwardPoint`'s direction (`GetNextConveyor`), looping via
  `player.BeginExternalControl()`/`EndExternalControl()` until no next segment is found.
- Scene progression: `SceneSwitcher` reads the active scene name with regex `Scene(\d+)` and loads
  `Scene{n+1}` after a dwell timer — scenes must be named `Scene1`, `Scene2`, etc. for this to work.
  Only `Assets/Scenes/SampleScene.unity` exists currently; the numbered level scenes have not been
  built yet.

### Migration status

Treat `MIGRATION.md` as the source of truth for what's implemented vs. pending — it tracks phase-by-
phase progress (core movement, mechanisms, prefabs/art, level building, docs) and records concrete
architectural decisions (grid model, trigger detection, DOTween usage, climb deferral) with the
reasoning behind each. Re-read it before making changes that touch player movement, mechanism
triggers, or the grid model, since it may be more current than this file.

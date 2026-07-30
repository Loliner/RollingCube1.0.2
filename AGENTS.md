# AGENTS.md

This file provides guidance to Codex when working in this repository.

## Project overview

RollingCube is a Unity URP puzzle game in which the player rolls a cube on a one-cell XZ grid,
tracks a marked rune face, pushes blocks, and operates mechanisms including elevators, pressure
switches, conveyors, fragile ground, rising terrain, and teleporters.

The current victory condition is global: the player must occupy the level's `SceneSwitcher` with
the rune face pointing down and remain there for two seconds.

- Unity Editor: `6000.5.3f1`
- URP: `17.5.0`
- Input System: `1.19.0`
- Tweening: DOTween in `Assets/Plugins/Demigiant/DOTween`
- Gameplay assembly: `Assets/Script/RollingCube.asmdef`

## Commands and testing

The project has no supported command-line build or test pipeline.

- Open it in Unity `6000.5.3f1`.
- Use Play Mode for gameplay and physics validation.
- Use **Window → General → Test Runner** for EditMode and PlayMode tests.
- Test assemblies exist under `Assets/Tests/EditMode/` and `Assets/Tests/PlayMode/`, but no test
  source files currently exist.

Do not claim Unity compilation or Play Mode verification unless it was actually run in the Editor.

## Design source of truth

- Chapter rules: `design/gdd/chapter-01.md`
- Level maps: `design/gdd/level-01.md` through `design/gdd/level-09.md`
- Map language: `design/gdd/level-map-schema.md` (RCMap 1.1)
- Mechanism rules: `design/gdd/mechanism-*.md`

RCMap describes walkable surface coordinates, height layers, entities, drops, rides, pushes, and
verified standard solutions. For Chapter 1:

- `E` always requires `rune.DOWN`.
- The dwell time is always two seconds.
- Those defaults are not repeated or overridden in individual levels.
- Every primary solution must end at `E` with the rune face down.

The Chapter 1 scene files exist under `Assets/Scenes/Chapter1/`, but they have not yet been rebuilt
against the current RCMap documents. Until a scene passes its GDD acceptance criteria, treat the
corresponding RCMap as the intended layout and the Unity scene as unfinished implementation.

## Grid and height model

- One normal XZ move is one cube width: `cubeHalfSize * 2`, normally `1.0`.
- Positions are snapped to `0.25` increments by `SnapToGrid()` in `Player` and `PushableBlock`.
- RCMap Y values represent walkable surface altitude, not Transform center height.
- Player center Y is `surface_y + cubeHalfSize`.
- There is no integer floor index. Legitimate terrain heights may differ by `0.25` or `0.50`.
- Upward movement into raised terrain is blocked because climbing is disabled.
- A successful move without support calls `StartFalling()`. `LandWhenSettled()` derives landing Y
  from the support raycast hit and snaps position and rotation.
- Falling below `killPlaneY` respawns the player at the position and rotation recorded in `Awake()`.

## Player movement

Authoritative implementation: `Assets/Script/Core/Player.cs`.

- `Update()` polls `Keyboard.current`; do not use the legacy `Input` API.
- Each keypress starts one 90-degree roll through `TryMove()`.
- `AnimateRoll()` uses manual pivot geometry with DOTween-driven interpolation and
  `Ease.InOutSine`.
- Blocked moves play `ShakeFeedback()`.
- `TryGetSupportBelow()` controls the transition to physics falling.
- `BeginExternalControl()` and `EndExternalControl()` are required when a mechanism owns movement.
- `IsRuneFaceDown()` is the shared orientation query used by goals and rune-gated mechanisms.
- `runeLocalAxis` must match the face carrying the visible rune.

Climbing is disabled. `Assets/Script/Mechanisms/Climbable.cs` is only a marker and is not connected
to `Player`. Do not add climbing behavior without a design decision covering its interaction with
pushables, elevators, conveyors, and falling.

## Pushable blocks

Authoritative implementation: `Assets/Script/Mechanisms/PushableBlock.cs`.

- A push moves the block one XZ cell with DOTween.
- Non-trigger colliders block the target cell; triggers remain valid destinations.
- Unsupported blocks become non-kinematic and fall under physics.
- Blocks do not use the player's kill-plane respawn.
- A pushed block implements `IExternallyControllable` and can be carried by elevators.

## Mechanism conventions

Mechanism scripts live in `Assets/Script/Mechanisms/`.

- Detect players with `other.GetComponent<Player>()`, not tags or object names.
- Detect pushable blocks with `other.GetComponent<PushableBlock>()`.
- Use DOTween for mechanism animation.
- Use coroutines for waits and delayed state changes.
- `Elevator` supports self-triggering, external switches, riders, resets, arrival-timed resets, and
  optional rune-down gating.
- `ElevatorSwitch` supports player or box occupants, multiple targets, dwell time, and reset when
  the last qualifying occupant leaves.
- Extend linked elevator behavior through `LinkedElevator` hooks instead of adding level-specific
  branches to `Elevator`.
- Always call `EndExternalControl()` when a mechanism returns control.

## Scene progression

`Assets/Script/Mechanisms/SceneSwitcher.cs` accepts scene names matching:

```text
Chapter{chapter}_Scene{scene}
```

After a valid two-second rune-down dwell it:

1. records completion through `LevelProgress`;
2. loads the next scene in the chapter if registered;
3. otherwise tries `Chapter{chapter + 1}_Scene1`;
4. otherwise logs a warning.

Do not use the older `Scene1`, `Scene2` naming convention in new content.

## Agent architecture

The repository includes Codex agent definitions under `.codex/` and shared production guidance
under `.claude/docs/`.

## Project structure

@.claude/docs/directory-structure.md

## Technical preferences

@.claude/docs/technical-preferences.md

## Coordination rules

@.claude/docs/coordination-rules.md

## Collaboration protocol

**User-driven collaboration, not autonomous execution.**

Default workflow: **Question → Options → Decision → Draft → Approval**

- Ask before writing unless the user has already approved the exact file or full changeset.
- Preserve unrelated user changes in a dirty worktree.
- Multi-file changes require explicit approval for the changeset.
- Do not create commits unless the user asks.

## Coding standards

@.claude/docs/coding-standards.md

## Context management

@.claude/docs/context-management.md

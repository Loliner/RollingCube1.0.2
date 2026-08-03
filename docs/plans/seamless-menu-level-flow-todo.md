# Seamless Menu and Level Flow — Implementation TODO

Related decision: [`ADR-0001`](../architecture/adr-0001-seamless-menu-level-flow.md)

## 0. Decision and Safety Baseline

- [x] Confirm the complete interaction and architecture with the user.
- [x] Record the accepted solution in ADR-0001.
- [x] Review the architecture against the existing Unity code.
- [x] Preserve unrelated untracked settings and screenshots.
- [x] Keep Chapter 1 RCMap layouts and verified solutions unchanged.

## 1. Runtime Foundation

- [x] Add the data-driven `LevelCatalog` and chapter/level definitions.
- [x] Add `LevelContext`, `LevelSpawn`, and inactive `StandaloneRig` support.
- [x] Add `GameFlowState` and `GameFlowController`.
- [x] Align incoming `LevelRoot` during `LevelContext.Awake`, before mechanism `Start`.
- [x] Keep the GameShell as the active scene during normal flow.
- [x] Add load-failure rollback so the outgoing preview is not lost.

## 2. Player Lifecycle

- [x] Reuse one persistent Player during normal GameShell flow.
- [x] Add `Player.PrepareForLevel(...)`.
- [x] Kill obsolete player tweens and coroutines during level preparation.
- [x] Reset rigidbody state, transform, scale, rune orientation, respawn and kill plane.
- [x] Make external-control ownership nestable so one system cannot unlock another.
- [x] Generate a temporary Player when a level is played directly.

## 3. Bidirectional Level Animation

- [x] Extend `LevelEntryAnimator` without breaking its serialized component type.
- [x] Animate direct `LevelRoot` children as transition units.
- [x] Add distance-staggered entry with `Ease.OutBack`.
- [x] Add distance-staggered exit with `Ease.InBack`.
- [x] Prepare additive scenes at zero scale before their first rendered frame.
- [x] Guarantee the outgoing ripple is still visible when the incoming ripple starts.
- [x] Use unscaled DOTween updates for menu and transition animation.
- [x] Support configurable overlap delay, entry duration, exit duration and distance delay.
- [x] Add an exclusion marker for non-visual transition children.
- [x] Call `Physics.SyncTransforms()` before gameplay resumes.

## 4. Seamless Flow Integration

- [x] Change `SceneSwitcher` to request completion through `GameFlowController`.
- [x] Retain single-scene fallback behaviour for standalone level testing.
- [x] Replace active-scene-name level identity with `LevelContext`.
- [x] Route next-level selection through `LevelCatalog`.
- [x] Return to Main Menu after the last registered level.
- [x] Coalesce rapid preview clicks so the last selection wins.
- [x] Reset/reload the same level safely without duplicate same-name scenes.

## 5. Main Menu and Camera

- [x] Convert `MainMenu` into the persistent GameShell.
- [x] Build the two-level chapter-card → level-card navigation.
- [x] Fade the chapter panel out completely before fading the level panel in.
- [x] Add selected, locked, completed and loading states.
- [x] Add a separate Start button.
- [x] Restore the last selected level; otherwise choose the highest unlocked level.
- [x] Display the initial live preview with an entry ripple.
- [x] Create placeholder visual assets/layout matching the reference composition.
- [x] Replace available placeholders with the approved Logo, chapter cards, thumbnails, icons and button-state sprites.
- [x] Fade/slide the menu over approximately 0.3 seconds.
- [x] Tween one persistent camera between preview and gameplay framing.
- [x] Support 4:3 through ultrawide layouts with 1920×1080 as the visual baseline.

## 6. Pause, Reset, Counters and Progress

- [x] Make flow state the authority for Menu, Playing, Paused and Transition.
- [x] Route Pause Menu Reset through the seamless transition.
- [x] Route Return to Main Menu through a clean preview reload.
- [x] Keep Resume from changing level state.
- [x] Reset Step/Dead counters only on explicit level-begin events.
- [x] Persist the last selected level in `LevelProgress`.
- [x] Keep existing completion/unlock save compatibility.

## 7. Scene Authoring and Migration

- [x] Update `Chapter1SceneBuilder`.
- [x] Keep every terrain tile and mechanism root as a direct `LevelRoot` child.
- [x] Author `LevelContext` and `LevelSpawn` for all nine levels.
- [x] Put fallback Camera/Light into an inactive `StandaloneRig`.
- [x] Ensure normal additive flow never enables duplicate Cameras or AudioListeners.
- [x] Generate the Chapter 1 `LevelCatalog` asset.
- [x] Configure MainMenu/GameShell references.
- [x] Rebuild Chapter 1 scenes 1–9 from the existing RCMap definitions.
- [x] Confirm skybox, lighting and shared environment ownership.

## 8. Documentation and Tests

- [x] Update `design/gdd/ui-start-screen.md`.
- [x] Update `design/gdd/ui-pause-menu.md`.
- [x] Document the seamless level-transition rules.
- [x] Add EditMode tests for catalog ordering and lookup.
- [x] Preserve direct-scene Chapter 1 completion tests.
- [x] Add GameShell additive-flow PlayMode coverage.
- [x] Add regressions for hidden incoming levels, ripple overlap ordering and sequential panel fades.
- [x] Replace private-field reflection with public readiness APIs where practical.

## 9. Unity Verification

- [x] Confirm Unity compilation succeeds.
- [x] Confirm Console has no new project errors.
- [x] Verify initial live preview and menu layout visually.
- [ ] Verify preview switching and one-second overlap visually.
- [x] Verify rapid selection resolves to the final request.
- [x] Verify Start hides UI and moves the camera without a second scene load.
- [x] Verify 1-1 completion transitions into 1-2.
- [x] Verify final-level completion returns to Main Menu.
- [x] Verify Pause, Resume, Reset and Return to Main Menu.
- [x] Verify every Chapter 1 scene can still be played directly.
- [x] Run relevant EditMode and PlayMode tests in the Unity Test Runner.
- [ ] Capture visual evidence for an in-progress overlap frame.

# Quick Design Spec: Level 03 Staggered Bridge Routes

**Type**: Tweak
**System**: Chapter 1 Level 03 / ElevatorSwitch
**GDD Reference**: `design/gdd/level-03.md`
**Date**: 2026-08-03

## Change Summary

Replace the flat two-platform bridge tutorial with a curved two-height route. The switch still
teaches the bridge mechanism, but the two bridge segments now rise in a visible near-to-far
sequence and the bridge exit offers two legitimate drops to the same goal.

## Motivation

The previous layout exposed the whole solution as two rectangles joined by a bridge. It taught the
mechanism but provided little exploration, turning, or spatial depth. The revision keeps the
teaching burden lighter than Level 02 while giving the player a small route-reading decision.

## Design Delta

Current Level 03 raises both bridge segments simultaneously and provides one straight route.

This revision:

1. keeps one main gameplay surface at `y=+0.00`;
2. uses `y=-0.50` for the lowered bridge and the goal routes;
3. reserves unreachable `y=+0.50` cliff blocks for visual depth only;
4. starts `bridge_west` immediately and `bridge_east` after `0.50s`;
5. provides `d1` and `d2` as two valid drops after the bridge;
6. gives both standard solutions 17 rolls and a rune-down arrival at `E`.

## New Rules / Values

| Rule | Value |
|------|-------|
| Switch hold | `1.00s` |
| West bridge delay | `0.00s` |
| East bridge delay | `0.50s` |
| Bridge move duration | `1.25s` |
| Bridge rise | `0.50` |
| Bridge reset | disabled |
| Primary drop | `d1`, short upper approach and turning lower route |
| Alternate drop | `d2`, longer upper approach and straight lower route |

## Affected Systems

| System | Impact | Action Required |
|--------|--------|-----------------|
| Level 03 GDD | RCMap, routes, rules, tuning and acceptance criteria change | Replace Level 03 layout |
| Chapter 1 scene builder | New geometry and per-target switch delays | Update `BuildLevel03` and `AddSwitch` |
| PlayMode tests | Old nine-step route is obsolete | Cover both routes and stagger timing |
| Elevator runtime | Already supports per-target delays | No runtime change |

## Acceptance Criteria

- [ ] Both bridge segments remain down until the switch is held for `1.00s`.
- [ ] `bridge_west` begins before `bridge_east`; the east segment remains still for `0.50s`.
- [ ] Both segments finish flush with the `y=+0.00` route and remain raised.
- [ ] `d1` and `d2` both land deterministically on the `y=-0.50` route.
- [ ] Both documented 17-roll solutions reach `E` with the rune face down.
- [ ] Decorative high blocks are unreachable and never required by a solution.
- [ ] No regression to other levels using zero-delay `AddSwitch` calls.

## GDD Update Required?

Yes. Replace `design/gdd/level-03.md` with the approved RCMap, two solutions, stagger rules and
matching acceptance criteria.

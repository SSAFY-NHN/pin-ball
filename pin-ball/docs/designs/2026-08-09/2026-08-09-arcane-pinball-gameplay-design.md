# Arcane Pinball Gameplay Board Design

## Goal

Replace the temporary pinball layout in `Assets/01. Scenes/02. Game.unity` with the approved arcane board and complete a mouse-only hybrid-physics play loop. The player pulls and releases a right-side plunger, then watches the ball play automatically while making occasional interventions by clicking either magnet. Existing battle, item, pooled-ball, goal, and UI flows remain connected.

## Confirmed player experience

- The player drags the right plunger handle downward and releases it to launch.
- Pull distance controls launch strength within a safe minimum/maximum range.
- After launch, Rigidbody2D physics controls the ball.
- The player may click the left or right magnet for a short attraction pulse.
- No keyboard input is required for normal play.
- When a ball enters a goal or an out lane, the next available ball loads automatically.
- Every loaded ball still requires a new mouse pull and release.
- Four goal pockets map to the existing `PinballGoal` unit-spawn flow.

## Chosen implementation approach

Use hybrid physics rather than a fully simulated spring assembly or scripted rail animation.

- The plunger's visible pieces move from pointer drag input.
- Release distance is converted into an upward launch velocity.
- The ball is held at a fixed load point until a valid release.
- Once released, the ball follows real 2D collision physics through the straight launch lane, connected top curve, and play field.
- Static board geometry uses scene-authored 2D colliders.

This keeps the traditional tactile interaction while avoiding unstable SpringJoint2D behavior in PC WebGL builds.

## Scene scope and hierarchy

Modify only `Assets/01. Scenes/02. Game.unity` and focused pinball prefabs/scripts. Preserve the existing game UI and battle hierarchy.

The pinball root will use the following conceptual structure:

```text
Pinball
├─ BoardVisual
├─ StaticColliders
│  ├─ OuterWalls
│  ├─ LaunchLane
│  ├─ TopEntryRail
│  └─ GoalDividers
├─ Launcher
│  ├─ LoadPoint
│  ├─ PlungerBase
│  ├─ PlungerPiston
│  ├─ PlungerSpring
│  └─ PlungerLever
├─ Obstacles
│  ├─ StandardBumpers (3)
│  ├─ SpecialBumper (1)
│  ├─ SmallPins (8)
│  ├─ AutoReflectors (2)
│  └─ Magnets (2)
├─ Goals (4)
├─ OutZones (2)
├─ Balls
└─ BoardVfx
```

Existing scene objects should be reused where their responsibilities already match. New runtime object creation is not the default; board devices and references are placed in the scene or prefabs and linked through the Inspector.

## Board layout

Positions are authored relative to the board root so the whole pinball area can be scaled to the existing right-side UI frame without changing gameplay proportions.

- The launch lane occupies the far-right edge.
- The straight lane and top curve form one continuous collider corridor with no gap or step.
- The top curve exits toward the upper-left side of the main field.
- Three standard triangular bumpers form an upper triangular cluster.
- One special circular bumper sits below the cluster on the board centerline.
- Eight small pins form a loose staggered field around and below the special bumper.
- Two magnets sit symmetrically in the lower-middle field.
- Two auto reflectors sit above the goal row and angle inward.
- Four equal goal pockets span the bottom center.
- Narrow out lanes sit only at the far-left and far-right ends of the goal row.

Collider gaps must be narrower than the ball diameter anywhere a ball could escape unintentionally. Goal openings remain at least one ball diameter wide, and adjacent goal trigger widths never overlap.

## Runtime components

### `PinballLauncherController`

- Owns pointer hit testing, drag clamping, plunger-piece transforms, and release strength.
- Accepts drag input only while the manager has a loaded ball and preparation actions are allowed.
- Uses scene references for the handle, piston, spring visual, load point, and launch direction.
- Resets visual parts after release or cancelled input.
- Reports normalized pull strength to `PinballManager`; it does not spend gold itself.

### `PinballManager` changes

- Replaces keyboard-based horizontal launcher movement with a loaded-ball state.
- Keeps available, loaded, and active balls distinct.
- Automatically loads the next available pooled ball after setup, goal completion, or failure.
- On valid plunger release, checks and spends the current launch cost, activates the loaded ball, and applies the calculated launch velocity.
- If payment fails or pull distance is below threshold, the ball remains loaded and the plunger resets.
- Existing item counters, goal registration, ball pooling, and battle-state reset behavior remain intact.

### `Pinball` changes

- Supports a loaded state with the object visible but Rigidbody2D simulation disabled.
- Launch accepts a velocity or direction-plus-speed supplied by the launcher instead of a single fixed downward speed.
- Active collision, item-hit counting, goal handling, and arcane VFX behavior remain unchanged.

### `PinballMagnetController`

- Each scene-placed magnet responds to a mouse click.
- A pulse affects active balls within a bounded radius for a short fixed duration.
- Force falls off with distance and is clamped so it redirects rather than teleports the ball.
- Each magnet has its own cooldown and visible ready/active/cooldown feedback.
- It queries the manager's active balls through a narrow read-only interface; it does not search the entire scene every frame.

### `PinballReflectorController`

- Uses a static collider for normal contact.
- On ball collision, applies a small deterministic impulse along the reflector's configured outward normal.
- The impulse supplements the physics material and prevents low-speed trapping without acting like a player-controlled flipper.

### Existing obstacles and goals

- Standard bumpers and the special bumper retain `EPinballObstacle.BigBumper` so existing bumper-related items continue to work.
- Small pins retain `EPinballObstacle.SmallPin`.
- All four goal triggers use `PinballGoal` and remain sorted left-to-right by world X.
- Out lanes use `PinballOutZone` and feed the existing refund/safety-net flow.

## Input and state flow

```text
Preparation state begins
→ manager loads one pooled ball at LoadPoint
→ player drags plunger with primary mouse button
→ player releases
→ manager validates pull and gold
→ ball enters Active state with upward velocity
→ automatic physics plus optional magnet clicks
→ goal or out-zone event
→ existing reward/spawn/refund logic
→ ball returns to pool
→ manager loads the next ball
```

Pointer cancellation, leaving the drag region, losing preparation permission, or disabling the launcher resets the plunger without launching.

## Physics policy

- Use Rigidbody2D continuous collision detection for balls.
- Use interpolated rendering for visible smoothness.
- Use a dedicated PhysicsMaterial2D with low friction and controlled bounciness for board walls.
- Bumper response may add an impulse but must preserve a configured maximum ball speed.
- Magnets and reflectors use force/impulse, never direct position teleportation.
- The connected launch rail is verified at minimum, nominal, and maximum pull strengths.
- A low-speed recovery rule may nudge a nearly stationary active ball only after a sustained timeout; it must not alter normal motion.

## Visual integration

- Use `pinball_board_arcane.png` as the board visual without modifying its pixels.
- Use the separated color sprites for visible devices.
- Use mask sprites only through the existing arcane additive/emissive material path.
- Reuse `PinballArcaneVfx` for ball trail and collision feedback.
- Magnet, bumper, reflector, goal, and plunger feedback remain local to the device; no full-screen flash or blur is added.
- Dynamic parts are separate SpriteRenderer children so piston, spring, lever, and effects can move independently.

## Error handling and safety

- Missing Inspector references disable only the affected controller and log one descriptive error.
- A missing loaded ball does not charge gold.
- Duplicate pointer releases cannot launch or charge twice.
- Balls outside all expected board bounds are routed through the same missed-ball flow as an out lane.
- Existing public APIs and unrelated scene objects are not renamed or moved.

## Verification criteria

### Edit-time

- Required sprite, prefab, collider, and controller references are present.
- Four goals register in stable left-to-right order.
- Device colliders do not overlap goals or the loaded ball.
- No unapproved keyboard input remains in the launcher flow.

### Play mode

- Minimum, medium, and maximum pulls all enter the top rail without escaping or stalling.
- Repeated releases charge and launch at most once per loaded ball.
- Both magnets visibly pulse and redirect nearby balls without teleportation.
- All bumpers, pins, and reflectors respond without trapping the ball.
- Every goal spawns its assigned ally and loads the next ball.
- Both out lanes trigger existing failure/refund/safety-net behavior.
- Ten consecutive launches complete without a stuck or escaped ball.

### WebGL

- Build succeeds for PC WebGL.
- The play loop remains usable with mouse-only input.
- Target is 60 fps at 1920×1080 with bounded particles and no new full-screen renderer feature.

## Out of scope

- Keyboard control as a primary interaction method.
- Fully physical SpringJoint2D plunger mechanics.
- Player-controlled flippers.
- Board tilting or 3D perspective changes.
- New external packages or a new save-data format.

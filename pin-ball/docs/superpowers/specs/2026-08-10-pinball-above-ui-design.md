# Pinball Above UI Design

## Goal

Render active pinballs and their attached visual effects above every UI element in the Game scene.

## Current State

- The Game scene Canvas uses Screen Space - Camera with sorting order 100.
- Ball SpriteRenderer instances currently use sorting orders around 20 to 30.
- Ball trail, glow, ring, and impact effects derive their sorting order relative to the ball renderer.
- Pinball board, obstacles, goals, and other world sprites have independent sorting orders.

## Design

Raise only the ball SpriteRenderer sorting order to a value above the UI Canvas, using 110 as the base order. Keep the existing relative offsets used by the attached trail, glow, ring, and impact renderers so the complete ball presentation appears above the UI while preserving its internal layering.

Do not change the Canvas render mode, Canvas sorting order, cameras, physics, UI input, pinball board, obstacles, goals, or unrelated world sprites.

Apply the base order to the reusable Ball prefab and any prefab-instance overrides that would otherwise retain a lower value. Update the existing editor setup utility's ball sorting value as well, so rerunning setup does not restore the old order.

## Verification

- Confirm all active ball bodies render above every Game scene UI panel, image, button, and text element.
- Confirm ball trail, glow, ring, and impact visuals remain correctly layered around the ball and also render above UI.
- Confirm board, obstacles, goals, and unrelated sprites retain their current rendering order.
- Run the relevant Unity EditMode tests and inspect the Game scene in Play Mode.

## AI Usage Record

- Tool/model: Codex, GPT-5.
- User request: Show pinball sprites above all UI.
- AI proposal: Raise the ball rendering order above the camera-space UI Canvas while retaining effect-relative offsets.
- AI modification area: Ball prefab sorting data, the existing editor setup default, and an EditMode rendering-order regression test.
- User decisions: All UI must remain below the balls; only balls and their attached effects move forward.
- Important instruction: Preserve the existing project structure and make the smallest verifiable change.
- Verification status: The rendering test failed before implementation with Ball `20` versus Canvas `100`, passed after setting Ball to `110`, and the full EditMode suite passed 46/46. Game view visual confirmation remains for the user.

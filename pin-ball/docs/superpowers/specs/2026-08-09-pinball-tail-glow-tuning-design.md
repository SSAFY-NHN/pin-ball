# Pinball Tail Glow Tuning Design

## Goal

Increase the overall scene glow slightly while making the pinball trail thicker and more luminous without obscuring the ball or board devices.

## Approved Visual Tuning

- Raise runtime Bloom intensity from `0.95` to `1.1`.
- Keep the current Bloom threshold and scatter unchanged so the adjustment affects strength, not the illuminated area.
- Raise the trail width range from `0.22` to `0.36` into `0.28` to `0.46`.
- Raise the trail material intensity from `1.35` to `1.8`.
- Preserve the existing cyan-to-purple speed color transition and trail lifetime.

## Scope

Only `ArcaneGameLook` and `PinballArcaneVfx` tuning values change. No scene layout, physics, collision, VFX catalog, shader structure, or public API changes are included.

## Verification

- Confirm C# compilation has no new errors.
- Confirm shader import has no errors.
- In Play mode, verify the ball remains readable, the tail is visibly thicker and brighter, and nearby board details remain distinguishable.

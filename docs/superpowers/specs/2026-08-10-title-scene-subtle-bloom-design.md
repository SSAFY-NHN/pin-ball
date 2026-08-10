# Title Scene Subtle Bloom Design

## Goal

Add restrained bloom across the title screen so only bright visual accents glow softly. Preserve the existing UI layout, button interaction, and entrance animation.

## Current State

- The title scene uses Unity 6 and URP with HDR support enabled.
- The main camera already allows HDR.
- The title Canvas uses Screen Space Overlay, so camera post-processing cannot affect its UI pixels.
- `TitleDirector` controls the existing logo and start-button sequence.

## Design

Render the title Canvas through the main camera by changing it to Screen Space Camera and assigning the existing main camera. Keep the Canvas hierarchy, RectTransform values, sorting behavior, and input components intact.

Enable URP post-processing on the title camera and add a title-scene global Volume with a dedicated profile. Configure Bloom with a relatively high threshold and low intensity so the bright portions of the background, logo, and button accents bloom without washing out dark areas or reducing text readability.

Use scene-authored components and serialized references rather than creating the camera, Canvas, or Volume at runtime. Keep `TitleDirector` focused on presentation timing and scene navigation; bloom configuration does not belong in that script.

## Initial Bloom Tuning

- Threshold: approximately `0.8–1.0`
- Intensity: approximately `0.25–0.45`
- Scatter: approximately `0.55–0.7`
- Clamp: conservative enough to prevent isolated UI pixels from flaring
- High-quality filtering: disabled for the PC WebGL target unless visual inspection shows unacceptable artifacts

These values are starting points. Final values should be chosen through visual inspection in Game view while preserving logo edges, button-label readability, and the dark fantasy background tone.

## Failure and Compatibility Considerations

- Preserve the CanvasScaler and RectTransform setup so changing render mode does not alter layout across aspect ratios.
- Keep the GraphicRaycaster and EventSystem path unchanged so the start button remains clickable.
- Ensure the camera's Volume layer mask includes the Volume layer.
- Avoid applying bloom through duplicated UI images or a custom shader; both add unnecessary asset and maintenance overhead for this scene.
- Do not reuse the gameplay runtime post-processing installer because the title needs independent, milder tuning.

## Verification

1. Open the title scene and confirm the Canvas renders through the main camera with unchanged placement.
2. Confirm the background, logo, and start button appear and animate in the same order as before.
3. Confirm the start button still receives clicks and loads the game scene.
4. Inspect bloom in Game view: bright accents should glow softly while dark regions remain dark and text stays crisp.
5. Check at representative 16:9 and wider aspect ratios.
6. Run Unity script compilation and verify the scene and Volume profile contain no missing references.

## Scope

This change is limited to the title scene's Canvas rendering and post-processing configuration. It does not change title artwork, UI layout, animation timing, gameplay post-processing, or navigation behavior.

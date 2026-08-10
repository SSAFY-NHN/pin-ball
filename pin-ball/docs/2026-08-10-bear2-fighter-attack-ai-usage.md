# Bear2 Fighter Energy Attack - AI Usage Record

- Date: 2026-08-10
- Tool: PixelLab MCP `animate_image`
- Mode: image-to-animation with the same source image supplied as the first and last frame
- Use case: `identity-preserve`
- Bear2 source mapping: `Assets/03. Images/Animals/Bear/Bear1_Fighter_Evolved_Tiny32.png`
- Job ID: `7c9d3867-4158-4e8f-be31-ec9309b6619d`
- Seed: `48232`
- Requested generated frames: `8`
- Delivered sheet cells: `9`
- Runtime animation keys: `10` (includes a final-pose hold key)
- Transparent background: `true`

## Prompt

> Create a clean non-looping attack for this exact 32x32 evolved bear fighter facing right. Preserve the brown bear, black headband, steel armor and gauntlets, red-brown vest, boots, proportions, palette, transparency, and grounded feet. Sequence: brace, bring BOTH forearms and BOTH hands together at the chest, lean slightly forward, then thrust BOTH hands forward simultaneously as if firing an energy blast. At full extension show a small pale-cyan/white flash directly between and just beyond both hands, then retract both arms and return to the exact start. Both arms stay attached and both hands remain visible and move together. Keep the legs stable. No one-handed punch, alternating hands, kick, jump, spin, weapon, extra limbs, large projectile, background, shadow, camera motion, or costume changes.

## Output

- Sprite sheet: `Assets/03. Images/Animals/Bear/Bear2_Fighter_Attack.png`
- Animation clip: `Assets/05. Animations/Bear/Bear2_Fighter_Attack.anim`
- Animator controller: `Assets/05. Animations/Bear/Bear2_Fighter_Attack.controller`
- Preview prefab: `Assets/04. Prefabs/Bear/Bear2_Fighter_AttackPreview.prefab`

## Unity Setup and Validation

- 9 sprites, each 32 x 32 pixels
- Sprite mode: Multiple
- Pixels per unit: 100
- Filter: Point
- Compression: Uncompressed
- Non-looping clip: 10 sprite keys at 12 fps, 0.833 seconds
- Controller default state: `Attack`
- Preview contains a `SpriteRenderer` and an `Animator` linked to the attack controller
- Unity console errors since the task baseline: 0

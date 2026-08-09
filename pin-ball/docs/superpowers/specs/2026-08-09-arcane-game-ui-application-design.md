# Arcane Game UI Application Design

## Goal

Apply the approved arcane HUD and bottom-panel art to `Assets/01. Scenes/02. Game.unity` while preserving existing gameplay and UI behavior. The implementation uses scene-authored UI objects and Inspector references; it does not create runtime fallback UI.

## Scope

In scope:

- Top HUD frame, HP, gold, ten-node wave progress, wave-start control, launch control, and settings decoration.
- Bottom shared frame for the owned-item and shop tabs.
- Minimal code changes required to drive dynamic wave-node states.
- Sprite import settings and scene serialization required by the approved images.

Out of scope:

- `Assets/04. Prefabs/UI.prefab`.
- Pinball, background, character, combat, economy, item, or wave-rule changes.
- New settings behavior, new shop behavior, new item slots, or new runtime-generated UI.
- Editing the approved source PNG artwork.

## Existing behavior to preserve

- `WavePanel` starts waves, launches balls, displays the dynamic launch cost, reports rejected preparation actions, and disables controls based on phase, ball state, roster, and gold.
- `BottomTabPanel` shows exactly one of the owned-item and shop contents.
- `ShopPanel`, `ItemPanel`, slots, purchase handling, reroll handling, and tooltips continue to work unchanged.
- The pinball panel continues to slide out when combat begins.

## Top HUD design

The approved `ui_hud_top_composite.png` supplies the visual frame and placement reference. Dynamic elements are scene-authored children layered above it:

- HP icon and TMP text showing current and maximum HP.
- Gold icon and TMP text showing the current gold value.
- Exactly ten wave-node Images and nine connector Images.
- Wave-start Button using the existing `WavePanel.startButton` action.
- Launch Button using the existing `WavePanel.launchButton` action and dynamic cost text.
- Settings artwork displayed as a non-interactable decorative control because no settings behavior is currently implemented.

The dynamic node Images fully cover the corresponding baked node regions of the composite reference. Connector completion artwork overlays the baked idle connectors.

### Wave states

- Nodes before the current wave use the completed Sprite.
- The current wave uses the current Sprite, except wave 5 uses elite 05, wave 9 uses elite 09, and wave 10 uses boss 10.
- Future waves use the idle Sprite.
- A locked Sprite is reserved for a globally unavailable state and is not used for ordinary future waves.
- Connectors before the current wave use the completed Sprite; remaining connectors use the idle Sprite.
- Wave numbers remain dynamic TMP text and are not baked into Sprite assets.

`StatusPanel` replaces the existing `Wave: n/10` output with serialized node, connector, and number-label references. A small `WaveNodeView` component may be added only if it materially simplifies per-node Image and TMP updates; otherwise the logic remains in `StatusPanel`.

## Button presentation

The existing Buttons remain the functional controls. Their `Selectable` Sprite Swap states use the approved Normal, Pressed, and Disabled Sprites. Hover uses the Normal Sprite unless a distinct approved hover asset exists. Dynamic text remains TMP:

- Wave-start wording is text, not part of the Sprite.
- Launch cost begins at 50G and continues to use the existing runtime value.
- Insufficient-gold cost text remains red through `WavePanel`.

The settings decoration is non-interactable. No button is wired to a missing function.

## Bottom-panel design

The bottom UI stays anchored to the lower-left safe area and uses:

- `ui_bottom_panel_frame.png` as the outer frame.
- `ui_bottom_panel_content.png` as the inner surface.
- Separate left and right gem Images.

Existing item and shop content objects become children of the shared visual region without changing their functional components. `BottomTabPanel` continues to activate exactly one content object. Existing preparation-phase restrictions and interactable states remain authoritative.

The redesign does not add item slots or create a second simultaneously visible expanded panel.

## Scene and code changes

Expected files:

- `Assets/01. Scenes/02. Game.unity`
- `Assets/02. Scripts/03. UI/StatusPanel.cs`
- Optional `Assets/02. Scripts/03. UI/WaveNodeView.cs` and its `.meta`, only if justified by the final hierarchy
- Approved UI Sprite `.meta` files if import settings require adjustment

No runtime `new GameObject()` or `AddComponent()` calls are permitted. All required UI objects are serialized in `02. Game.unity`.

## Error handling

- Missing required node, connector, label, icon, or button references produce explicit errors during UI initialization.
- A wave count other than ten produces an explicit error and prevents misleading progress rendering; gameplay data validation remains owned by the battle system.
- Missing optional settings decoration does not affect gameplay.

## Verification

Static and Unity batch-mode checks must verify:

- C# compilation with Unity `6000.0.79f1`.
- Successful import and save of `02. Game.unity`.
- Exactly ten serialized wave nodes and nine connectors.
- Correct special assets for waves 5, 9, and 10.
- Existing `WavePanel`, `BottomTabPanel`, `ShopPanel`, and `ItemPanel` references remain assigned.
- No duplicate scene fileIDs or asset GUIDs.
- No new runtime UI construction.
- No modifications to `Assets/04. Prefabs/UI.prefab`.

Visual play-mode verification remains required for final approval: preparation layout, dynamic HP/gold/cost text, wave progression, disabled controls, tab switching, shop purchase/reroll, tooltips, and the combat transition must be checked in Unity.

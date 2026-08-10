# Game Shop UI Resource Integration Design

## Goal

Apply the newly added shop and item UI sprites to `Assets/01. Scenes/02. Game.unity` while preserving the current shop, inventory, tooltip, and tab behavior.

## Scope

- Modify only the Game scene and the minimum runtime UI code required for category-dependent shop slot visuals.
- Apply the new side-tab backgrounds and shop/item tab icons.
- Apply all four reroll button state sprites.
- Apply the item tooltip background to both shop and owned-item tooltips.
- Select each shop slot's normal background from the item's `EItemCategory` (`Ball`, `Board`, or `Battle`).
- Display the highlighted slot sprite while hovered/selected by the Unity button transition.
- Display the sold-out slot sprite when a slot is unavailable after purchase or otherwise disabled.
- Preserve the current layout unless a RectTransform adjustment is required to fit the supplied artwork.

## Explicit Exclusions

- Do not modify `Assets/04. Prefabs/UI.prefab`.
- Do not apply `Galmuri11-Bold.ttf`; font integration will be handled separately by the user.
- Do not alter the user's existing evolution image or rabbit animation changes.
- Do not redesign shop behavior, pricing, item selection, or tooltip content.

## Architecture

Static visual assignments belong in `02. Game.unity`. Existing Unity `Button` sprite-swap transitions will represent tab and reroll states.

`ShopSlot` needs serialized references for the three category-specific normal sprites and the common highlighted and sold-out sprites. When `SetItem` receives an item, the slot selects the matching normal sprite from `Item.Category`. `RefreshState` continues to decide purchase availability and updates the button's visual state without changing purchase rules.

No new UI framework, prefab, or runtime asset loading layer is introduced.

## Component Mapping

- Item tab: `ui_side_tab_inactive`, `ui_side_tab_pressed`, `ui_side_tab_active`, and `ui_icon_item_tab`.
- Shop tab: `ui_side_tab_inactive`, `ui_side_tab_pressed`, `ui_side_tab_active`, and `ui_icon_shop_tab`.
- Reroll button: `ui_button_shop_reroll_normal`, `highlighted`, `pressed`, and `disabled`.
- Shop slots: category-specific normal sprite plus `ui_shop_slot_highlighted` and `ui_shop_slot_sold_out`.
- Shop and item tooltips: `ui_item_tooltip_background`.

## Data Flow

1. `ShopPanel` supplies an `Item` to each `ShopSlot`.
2. `ShopSlot.SetItem` maps `Item.Category` to the correct normal slot sprite.
3. `ShopSlot.RefreshState` preserves the existing purchase eligibility calculation.
4. Unity's `Button` transition renders hover, press, and disabled visuals from scene-assigned sprites.
5. `BottomTabPanel` continues toggling tab button interactability; the active tab therefore uses the assigned active sprite.

## Error Handling

- Missing optional visual references fall back without throwing, leaving the existing image usable.
- A missing item clears the slot and disables purchasing as it does today.
- Unsupported category values use a deterministic fallback normal sprite and may emit a warning in editor/development builds if useful.

## Testing and Verification

- Add an Edit Mode test that proves each `EItemCategory` maps to the intended slot sprite and that unavailable slots use the sold-out visual state.
- Run the focused Edit Mode test and confirm it fails before implementation, then passes afterward.
- Run the repository's relevant Edit Mode test suite.
- Validate scene YAML references for every new sprite GUID and ensure `UI.prefab` remains unchanged.
- If a Unity executable is available, open or batch-load the Game scene to catch import and serialization errors.

## Success Criteria

- The Game scene visibly uses every newly supplied shop/item sprite except the font.
- Tab, reroll, tooltip, hover, purchase, and sold-out behavior remains functional.
- Shop slot normal artwork matches the item's category.
- No changes are made to `UI.prefab` or unrelated user-owned files.

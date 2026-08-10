from pathlib import Path

from PIL import Image


SOURCE = Path(r"C:\Users\Home\AppData\Local\Temp\codex-clipboard-ad7ca1bd-dca6-44bc-91a1-d3d1cb2e2c95.png")
OUTPUT = Path(r"Assets\03. Images\UI\ArcaneHudV2")


# Inclusive-exclusive crop rectangles measured from the approved 1254x1254 RGBA sheet.
# The source already contains native transparency, so RGB pixels are never keyed out.
ASSETS = {
    "ui_hud_top_composite": (10, 28, 1248, 151),
    "ui_icon_hp": (26, 184, 153, 297),
    "ui_icon_gold": (197, 184, 308, 293),
    "ui_wave_node_idle": (16, 333, 166, 488),
    "ui_wave_node_current": (175, 326, 334, 494),
    "ui_wave_node_complete": (343, 331, 502, 489),
    "ui_wave_node_locked": (507, 330, 671, 490),
    "ui_wave_node_elite_05": (686, 311, 861, 497),
    "ui_wave_node_elite_09": (873, 305, 1054, 501),
    "ui_wave_node_boss_10": (1058, 296, 1248, 501),
    "ui_wave_connector_idle": (26, 518, 279, 574),
    "ui_wave_connector_complete": (310, 518, 570, 576),
    "ui_button_wave_start_normal": (14, 600, 317, 720),
    "ui_button_wave_start_pressed": (329, 600, 635, 721),
    "ui_button_wave_start_disabled": (648, 600, 951, 721),
    "ui_button_launch_normal": (12, 735, 317, 855),
    "ui_button_launch_pressed": (328, 736, 635, 855),
    "ui_button_launch_disabled": (648, 736, 951, 855),
    "ui_button_settings_normal": (14, 865, 168, 1020),
    "ui_button_settings_pressed": (178, 865, 330, 1020),
    "ui_button_settings_disabled": (341, 865, 493, 1020),
    "ui_button_battle_state_normal": (514, 865, 672, 1020),
    "ui_button_battle_state_pressed": (682, 865, 839, 1020),
    "ui_button_battle_state_disabled": (846, 865, 1000, 1020),
    "ui_mask_wave_complete": (0, 1025, 164, 1225),
    "ui_mask_wave_current": (160, 1025, 320, 1225),
    "ui_mask_wave_elite_05": (315, 1025, 482, 1225),
    "ui_mask_wave_elite_09": (475, 1025, 642, 1225),
    "ui_mask_wave_boss_10": (633, 1025, 808, 1225),
    "ui_mask_button_wave_start": (820, 1060, 1037, 1190),
    "ui_mask_button_launch": (1035, 1060, 1254, 1190),
}


def trim_transparent(image, padding=2):
    alpha_box = image.getchannel("A").getbbox()
    if alpha_box is None:
        raise ValueError("Crop contains no visible pixels")
    left = max(0, alpha_box[0] - padding)
    top = max(0, alpha_box[1] - padding)
    right = min(image.width, alpha_box[2] + padding)
    bottom = min(image.height, alpha_box[3] + padding)
    return image.crop((left, top, right, bottom))


def main():
    source = Image.open(SOURCE).convert("RGBA")
    if source.size != (1254, 1254):
        raise ValueError(f"Unexpected source size: {source.size}")

    OUTPUT.mkdir(parents=True, exist_ok=True)
    for name, bounds in ASSETS.items():
        result = trim_transparent(source.crop(bounds))
        result.save(OUTPUT / f"{name}.png")

        alpha = result.getchannel("A")
        if alpha.getbbox() is None:
            raise ValueError(f"Empty alpha in {name}")

    print(f"Exported {len(ASSETS)} native-alpha PNG files to {OUTPUT.resolve()}")


if __name__ == "__main__":
    main()

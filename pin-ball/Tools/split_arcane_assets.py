from pathlib import Path
from shutil import copy2

from PIL import Image


PROJECT_ROOT = Path(__file__).resolve().parents[1]
OUTPUT = PROJECT_ROOT / "Assets" / "03. Images" / "Pinball" / "Arcane"

DOWNLOADS = Path(r"C:\Users\Home\Downloads")
TEMP = Path(r"C:\Users\Home\AppData\Local\Temp")

SOURCES = {
    "board": DOWNLOADS / "AIDrawing_260809_b80d7faf-a1eb-4fb4-9cb8-a7e76e6e7630_0_MiriCanvas.png",
    "runes": DOWNLOADS / "ChatGPT Image 2026년 8월 9일 오후 03_35_30.png",
    "magnet": DOWNLOADS / "ChatGPT Image 2026년 8월 9일 오후 03_27_47.png",
    "bumper_standard": DOWNLOADS / "ChatGPT Image 2026년 8월 9일 오후 03_22_26.png",
    "bumper_special": DOWNLOADS / "ChatGPT Image 2026년 8월 9일 오후 03_22_56.png",
    "ball": TEMP / "codex-clipboard-247e1d65-ea69-4e8f-8d1b-6251bde4b633.png",
    "pin": TEMP / "codex-clipboard-e1007d83-06e4-4d33-b6db-3696928cc55f.png",
    "reflector": DOWNLOADS / "ChatGPT Image 2026년 8월 9일 오후 03_10_36.png",
    "plunger_parts": DOWNLOADS / "ChatGPT Image 2026년 8월 9일 오후 04_03_59.png",
    "spring": TEMP / "codex-clipboard-5f94f24b-b71c-490f-b6a7-808cd7110c4b.png",
    "magnet_vfx": TEMP / "codex-clipboard-83aec40c-8fc6-4803-843d-3f8c8c0df3b1.png",
    "core_vfx": TEMP / "codex-clipboard-6e7453df-c59c-4c45-b271-a7f6563476b6.png",
    "goal_vfx": TEMP / "codex-clipboard-2e6079cf-5baa-4307-8f03-884f14dba908.png",
}

# Bounds are half-open (left, top, right, bottom). They were measured from the
# alpha channel of the approved source images. Padding is added during export.
EXPORTS = {
    "runes": [
        ("rune_guardian.png", (98, 142, 319, 453)),
        ("rune_ranger.png", (454, 126, 748, 470)),
        ("rune_mage.png", (876, 141, 1047, 477)),
        ("rune_lancer.png", (1264, 100, 1403, 470)),
        ("rune_guardian_mask.png", (98, 574, 320, 887)),
        ("rune_ranger_mask.png", (451, 555, 746, 901)),
        ("rune_mage_mask.png", (875, 575, 1050, 908)),
        ("rune_lancer_mask.png", (1264, 537, 1403, 901)),
    ],
    "magnet": [
        ("magnet_device.png", (78, 248, 692, 748)),
        ("magnet_device_mask.png", (831, 249, 1438, 747)),
    ],
    "bumper_standard": [
        ("bumper_standard.png", (30, 335, 601, 853)),
        ("bumper_standard_mask.png", (653, 334, 1226, 853)),
    ],
    "bumper_special": [
        ("bumper_special.png", (54, 146, 713, 825)),
        ("bumper_special_mask.png", (824, 144, 1481, 825)),
    ],
    "ball": [
        ("ball_arcane.png", (79, 198, 690, 822)),
        ("ball_arcane_mask.png", (847, 197, 1457, 822)),
    ],
    "pin": [("pin_small.png", (506, 339, 748, 866))],
    "reflector": [
        ("reflector_auto.png", (143, 367, 725, 652)),
        ("reflector_auto_mask.png", (885, 368, 1465, 652)),
    ],
    "plunger_parts": [
        ("plunger_base.png", (66, 617, 455, 879)),
        ("plunger_piston.png", (489, 132, 782, 851)),
        ("plunger_lever.png", (1195, 469, 1504, 827)),
    ],
    "spring": [("plunger_spring.png", (482, 78, 771, 1188))],
    "magnet_vfx": [
        ("vfx_magnet_arc.png", (48, 398, 1089, 631)),
        ("vfx_magnet_spark.png", (1274, 414, 1432, 611)),
    ],
    "core_vfx": [
        ("vfx_ball_trail.png", (92, 452, 578, 562)),
        ("vfx_ball_impact.png", (709, 390, 942, 631)),
        ("vfx_ball_ring.png", (1037, 261, 1507, 739)),
    ],
    "goal_vfx": [
        ("vfx_goal_arc_top_left.png", (140, 118, 434, 431)),
        ("vfx_goal_arc_top_right.png", (597, 118, 891, 431)),
        ("vfx_goal_arc_bottom_left.png", (140, 593, 434, 905)),
        ("vfx_goal_arc_bottom_right.png", (597, 593, 891, 905)),
        ("vfx_goal_spark.png", (1102, 421, 1427, 601)),
    ],
}


def padded_bounds(bounds, image_size, padding=24):
    left, top, right, bottom = bounds
    width, height = image_size
    return (
        max(0, left - padding),
        max(0, top - padding),
        min(width, right + padding),
        min(height, bottom + padding),
    )


def export_crop(source: Path, filename: str, bounds):
    with Image.open(source).convert("RGBA") as image:
        crop = image.crop(padded_bounds(bounds, image.size))
        destination = OUTPUT / filename
        crop.save(destination, format="PNG", optimize=False)


def validate_outputs(expected_count: int):
    outputs = sorted(OUTPUT.glob("*.png"))
    if len(outputs) != expected_count:
        raise RuntimeError(f"Expected {expected_count} PNG files, found {len(outputs)}")

    for path in outputs:
        with Image.open(path) as image:
            if image.mode != "RGBA":
                raise RuntimeError(f"{path.name}: expected RGBA, found {image.mode}")
            alpha = image.getchannel("A")
            if alpha.getbbox() is None:
                raise RuntimeError(f"{path.name}: image is fully transparent")
            corners = (
                alpha.getpixel((0, 0)),
                alpha.getpixel((image.width - 1, 0)),
                alpha.getpixel((0, image.height - 1)),
                alpha.getpixel((image.width - 1, image.height - 1)),
            )
            if any(corners):
                raise RuntimeError(f"{path.name}: non-transparent corner detected {corners}")


def create_preview():
    paths = [path for path in sorted(OUTPUT.glob("*.png")) if "board" not in path.name]
    tile_width, tile_height = 240, 190
    columns = 5
    rows = (len(paths) + columns - 1) // columns
    preview = Image.new("RGBA", (columns * tile_width, rows * tile_height), (18, 20, 28, 255))

    for index, path in enumerate(paths):
        with Image.open(path).convert("RGBA") as image:
            image.thumbnail((tile_width - 24, tile_height - 36), Image.Resampling.NEAREST)
            x = (index % columns) * tile_width + (tile_width - image.width) // 2
            y = (index // columns) * tile_height + 8
            preview.alpha_composite(image, (x, y))

    preview_path = PROJECT_ROOT / "Temp" / "arcane_asset_preview.png"
    preview.save(preview_path, format="PNG")
    return preview_path


def main():
    missing = [str(path) for path in SOURCES.values() if not path.is_file()]
    if missing:
        raise FileNotFoundError("Missing approved sources:\n" + "\n".join(missing))

    OUTPUT.mkdir(parents=True, exist_ok=True)
    copy2(SOURCES["board"], OUTPUT / "pinball_board_arcane.png")

    for source_key, exports in EXPORTS.items():
        for filename, bounds in exports:
            export_crop(SOURCES[source_key], filename, bounds)

    expected_count = 1 + sum(len(items) for items in EXPORTS.values())
    validate_outputs(expected_count)
    preview_path = create_preview()
    print(f"Exported and validated {expected_count} files in {OUTPUT}")
    print(f"Preview: {preview_path}")


if __name__ == "__main__":
    main()

#!/usr/bin/env python3
"""Validate the serialized Moonlit Workshop pinball theme contract."""

from pathlib import Path
import re
import sys


ROOT = Path(__file__).resolve().parents[1]
ASSETS = ROOT / "Assets"
SCENE = ASSETS / "01. Scenes" / "02. Game.unity"
CATALOG = ASSETS / "Resources" / "ArcaneVFX" / "ArcaneVfxCatalog.asset"
ARCANE_ROOT = "Assets/03. Images/Pinball/Arcane/"
MOONLIT_ROOT = "Assets/03. Images/Pinball/MoonlitWorkshop/"


def asset_guid_paths(failures: list[str]) -> dict[str, str]:
    paths: dict[str, str] = {}
    for meta_path in ASSETS.rglob("*.meta"):
        text = meta_path.read_text(encoding="utf-8", errors="replace")
        match = re.search(r"(?m)^guid: ([0-9a-f]+)$", text)
        if match is None:
            continue
        asset_path = meta_path.with_suffix("")
        relative_path = asset_path.relative_to(ROOT).as_posix()
        guid = match.group(1)
        if guid in paths:
            failures.append(f"duplicate asset GUID {guid}: {relative_path}")
        paths[guid] = relative_path
    return paths


def guid_path(guid: str, paths: dict[str, str], failures: list[str]) -> str:
    path = paths.get(guid, "")
    if not path:
        failures.append(f"unknown asset GUID: {guid}")
    return path


def parse_scene_renderers(
    text: str,
    paths: dict[str, str],
    failures: list[str],
) -> tuple[dict[str, list[str]], dict[str, str], dict[str, str]]:
    documents = re.split(r"(?m)(?=^--- !u!)", text)
    names: dict[str, str] = {}
    transforms_by_object: dict[str, str] = {}
    objects_by_transform: dict[str, str] = {}
    transform_parents: dict[str, str] = {}

    for document in documents:
        game_object = re.match(r"--- !u!1 &(\d+)", document)
        name = re.search(r"(?m)^  m_Name: ([^\r\n]+)$", document)
        if game_object and name:
            names[game_object.group(1)] = name.group(1).strip("'\"")

        transform = re.match(r"--- !u!4 &(\d+)", document)
        transform_object = re.search(
            r"(?m)^  m_GameObject: \{fileID: (\d+)\}$", document
        )
        parent = re.search(r"(?m)^  m_Father: \{fileID: (\d+)\}$", document)
        if transform and transform_object and parent:
            transform_id = transform.group(1)
            object_id = transform_object.group(1)
            transforms_by_object[object_id] = transform_id
            objects_by_transform[transform_id] = object_id
            transform_parents[transform_id] = parent.group(1)

    board_ids = [object_id for object_id, name in names.items() if name == "ArcaneBoard"]
    if len(board_ids) != 1:
        failures.append(f"expected one ArcaneBoard object, found {len(board_ids)}")
        return {}, names, transforms_by_object
    board_transform = transforms_by_object.get(board_ids[0], "")

    def is_below_board(object_id: str) -> bool:
        transform_id = transforms_by_object.get(object_id, "")
        while transform_id and transform_id != "0":
            if transform_id == board_transform:
                return True
            transform_id = transform_parents.get(transform_id, "")
        return False

    board_renderers: dict[str, list[str]] = {}
    all_renderers: dict[str, str] = {}
    for document in documents:
        if re.match(r"--- !u!212 &\d+", document) is None:
            continue
        object_match = re.search(
            r"(?m)^  m_GameObject: \{fileID: (\d+)\}$", document
        )
        sprite_match = re.search(
            r"(?m)^  m_Sprite: \{fileID: [^,]+, guid: ([0-9a-f]+), type: 3\}$",
            document,
        )
        if object_match is None or sprite_match is None:
            continue
        object_id = object_match.group(1)
        path = guid_path(sprite_match.group(1), paths, failures)
        all_renderers[object_id] = path
        if is_below_board(object_id):
            board_renderers.setdefault(names.get(object_id, object_id), []).append(path)

    return board_renderers, all_renderers, names


def parse_catalog_fields(text: str) -> tuple[dict[str, str], dict[str, list[str]]]:
    singles: dict[str, str] = {}
    arrays: dict[str, list[str]] = {}
    current_array = ""
    for line in text.splitlines():
        single = re.match(
            r"^  ([A-Za-z][A-Za-z0-9]*): \{fileID: [^,]+, guid: ([0-9a-f]+), type: 3\}$",
            line,
        )
        if single:
            singles[single.group(1)] = single.group(2)
            current_array = ""
            continue
        array = re.match(r"^  ([A-Za-z][A-Za-z0-9]*):$", line)
        if array:
            current_array = array.group(1)
            arrays[current_array] = []
            continue
        element = re.match(
            r"^  - \{fileID: [^,]+, guid: ([0-9a-f]+), type: 3\}$", line
        )
        if element and current_array:
            arrays[current_array].append(element.group(1))
    return singles, arrays


def require_named_renderer(
    renderers: dict[str, list[str]],
    object_name: str,
    expected_path: str,
    failures: list[str],
) -> None:
    actual = renderers.get(object_name, [])
    if actual != [expected_path]:
        failures.append(f"{object_name}: expected {expected_path}, found {actual}")


def main() -> int:
    failures: list[str] = []
    if not SCENE.is_file():
        failures.append(f"missing scene: {SCENE.relative_to(ROOT).as_posix()}")
    if not CATALOG.is_file():
        failures.append(f"missing catalog: {CATALOG.relative_to(ROOT).as_posix()}")
    if failures:
        for failure in failures:
            print(f"- {failure}")
        return 1

    paths = asset_guid_paths(failures)
    scene_text = SCENE.read_text(encoding="utf-8")
    board_renderers, all_renderers, names = parse_scene_renderers(
        scene_text, paths, failures
    )

    for object_name, sprite_paths in board_renderers.items():
        for sprite_path in sprite_paths:
            if not sprite_path.startswith(MOONLIT_ROOT):
                failures.append(
                    f"{object_name}: board sprite is outside MoonlitWorkshop: "
                    f"{sprite_path}"
                )

    named_expectations = {
        "BoardVisual": MOONLIT_ROOT + "board_base.png",
        "BoardGlow": MOONLIT_ROOT + "board_base_mask.png",
        "SpecialBumper": MOONLIT_ROOT + "bumper_jackpot.png",
        "Magnet_Left": MOONLIT_ROOT + "Obstacles/obstacle_clockwork_spinner.png",
        "Magnet_Right": MOONLIT_ROOT + "Obstacles/obstacle_clockwork_spinner.png",
        "Reflector_Left": MOONLIT_ROOT + "deflector_bar.png",
        "Reflector_Right": MOONLIT_ROOT + "deflector_bar.png",
        "PlungerBase": MOONLIT_ROOT + "Obstacles/bumper_clockwork_gear.png",
        "PlungerLever": MOONLIT_ROOT + "guide_rail.png",
        "PlungerLeverGlow": MOONLIT_ROOT + "guide_rail_mask.png",
        "PlungerPiston": MOONLIT_ROOT + "deflector_bar.png",
        "PlungerSpring": MOONLIT_ROOT + "Obstacles/obstacle_spring_gate.png",
        "Plunger Spring Glow": (
            MOONLIT_ROOT + "Obstacles/obstacle_spring_gate.png"
        ),
    }
    for object_name, expected_path in named_expectations.items():
        require_named_renderer(
            board_renderers, object_name, expected_path, failures
        )

    for object_name, sprite_paths in board_renderers.items():
        if object_name.startswith("StandardBumper_"):
            expected = MOONLIT_ROOT + "bumper_standard.png"
            if sprite_paths != [expected]:
                failures.append(
                    f"{object_name}: expected {expected}, found {sprite_paths}"
                )
        if object_name.startswith("SmallPin_"):
            expected = MOONLIT_ROOT + "pin_small.png"
            if sprite_paths != [expected]:
                failures.append(
                    f"{object_name}: expected {expected}, found {sprite_paths}"
                )

    rune_paths = sorted(
        path for path in board_renderers.get("Rune", [])
    )
    expected_runes = sorted(
        [
            MOONLIT_ROOT + "Obstacles/bumper_clockwork_gear.png",
            MOONLIT_ROOT + "Obstacles/obstacle_clockwork_spinner.png",
            MOONLIT_ROOT + "Obstacles/obstacle_forge_cross.png",
            MOONLIT_ROOT + "Obstacles/obstacle_spring_gate.png",
        ]
    )
    if rune_paths != expected_runes:
        failures.append(f"Rune sprites: expected {expected_runes}, found {rune_paths}")

    ball_ids = [object_id for object_id, name in names.items() if name == "Ball"]
    ball_paths = [all_renderers.get(object_id, "") for object_id in ball_ids]
    expected_ball = ARCANE_ROOT + "ball_arcane.png"
    if ball_paths != [expected_ball]:
        failures.append(f"Ball: expected [{expected_ball}], found {ball_paths}")

    catalog_text = CATALOG.read_text(encoding="utf-8")
    singles, arrays = parse_catalog_fields(catalog_text)
    expected_singles = {
        "ballMask": ARCANE_ROOT + "ball_arcane_mask.png",
        "standardBumperMask": MOONLIT_ROOT + "bumper_standard_mask.png",
        "specialBumperMask": MOONLIT_ROOT + "bumper_jackpot_mask.png",
        "magnetMask": (
            MOONLIT_ROOT + "Obstacles/obstacle_clockwork_spinner_mask.png"
        ),
        "reflectorMask": MOONLIT_ROOT + "deflector_bar_mask.png",
        "guardianRuneMask": (
            MOONLIT_ROOT + "Obstacles/bumper_clockwork_gear_mask.png"
        ),
        "rangerRuneMask": (
            MOONLIT_ROOT + "Obstacles/obstacle_clockwork_spinner_mask.png"
        ),
        "mageRuneMask": MOONLIT_ROOT + "Obstacles/obstacle_forge_cross_mask.png",
        "lancerRuneMask": (
            MOONLIT_ROOT + "Obstacles/obstacle_spring_gate_mask.png"
        ),
    }
    for field, expected_path in expected_singles.items():
        actual_path = guid_path(singles.get(field, ""), paths, failures)
        if actual_path != expected_path:
            failures.append(f"{field}: expected {expected_path}, found {actual_path}")

    expected_arrays = {
        "ballTrail": [ARCANE_ROOT + "vfx_ball_trail.png"],
        "ballImpact": [ARCANE_ROOT + "vfx_ball_impact.png"],
        "ballRing": [ARCANE_ROOT + "vfx_ball_ring.png"],
        "magnetArc": [
            MOONLIT_ROOT + "Obstacles/obstacle_clockwork_spinner_mask.png"
        ],
        "magnetSpark": [MOONLIT_ROOT + "pin_small_mask.png"],
        "goalRing": [MOONLIT_ROOT + "bumper_jackpot_mask.png"],
        "goalArcTopLeft": [MOONLIT_ROOT + "guide_rail_mask.png"],
        "goalArcTopRight": [MOONLIT_ROOT + "guide_rail_mask.png"],
        "goalArcBottomLeft": [MOONLIT_ROOT + "guide_rail_mask.png"],
        "goalArcBottomRight": [MOONLIT_ROOT + "guide_rail_mask.png"],
        "goalSpark": [MOONLIT_ROOT + "pin_small_mask.png"],
    }
    for field, expected_paths in expected_arrays.items():
        actual_paths = [
            guid_path(guid, paths, failures) for guid in arrays.get(field, [])
        ]
        if actual_paths != expected_paths:
            failures.append(f"{field}: expected {expected_paths}, found {actual_paths}")

    if failures:
        print("Moonlit pinball theme validation failed:")
        for failure in failures:
            print(f"- {failure}")
        return 1

    print("Moonlit pinball theme validation passed (0 failures).")
    return 0


if __name__ == "__main__":
    sys.exit(main())

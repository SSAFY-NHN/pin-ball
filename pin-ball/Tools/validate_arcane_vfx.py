#!/usr/bin/env python3
"""Static contract checks for the arcane WebGL visual slice."""

from pathlib import Path
import sys


ROOT = Path(__file__).resolve().parents[1]


def require_file(relative_path: str, failures: list[str]) -> str:
    path = ROOT / relative_path
    if not path.is_file():
        failures.append(f"missing file: {relative_path}")
        return ""
    return path.read_text(encoding="utf-8")


def require_contains(text: str, needle: str, label: str, failures: list[str]) -> None:
    if needle not in text:
        failures.append(f"{label}: missing {needle!r}")


def main() -> int:
    failures: list[str] = []
    required_assets = (
        "Assets/02. Scripts/Visual/ArcaneGameLook.cs",
        "Assets/02. Scripts/Visual/PinballArcaneVfx.cs",
        "Assets/Resources/ArcaneVFX/ArcaneSprite.shader",
        "Assets/Resources/ArcaneVFX/ArcaneAdditive.shader",
    )

    contents = {}
    for relative_path in required_assets:
        contents[relative_path] = require_file(relative_path, failures)
        require_file(f"{relative_path}.meta", failures)

    pinball = require_file("Assets/02. Scripts/Pinball/Pinball.cs", failures)
    for needle in (
        "PinballArcaneVfx",
        "OnActivated()",
        "OnDeactivated()",
        "OnVelocityChanged(",
        "PlayCollision(",
    ):
        require_contains(pinball, needle, "Pinball.cs", failures)

    look = contents[required_assets[0]]
    for needle in (
        'TargetSceneName = "02. Game"',
        "renderPostProcessing = true",
        "profile.Add<Bloom>",
        "highQualityFiltering.Override(false)",
        "profile.Add<ColorAdjustments>",
        "profile.Add<Vignette>",
    ):
        require_contains(look, needle, "ArcaneGameLook.cs", failures)

    vfx = contents[required_assets[1]]
    for needle in (
        'Resources.Load<Shader>("ArcaneVFX/ArcaneSprite")',
        'Resources.Load<Shader>("ArcaneVFX/ArcaneAdditive")',
        "new ParticleSystem.Particle[",
        "position = _impact.transform.position",
        "trail.time",
    ):
        require_contains(vfx, needle, "PinballArcaneVfx.cs", failures)

    for relative_path in required_assets[2:]:
        shader = contents[relative_path]
        require_contains(shader, '"RenderPipeline"="UniversalPipeline"', relative_path, failures)
        require_contains(shader, '"LightMode"="Universal2D"', relative_path, failures)
        if "_CameraOpaqueTexture" in shader or "_CameraDepthTexture" in shader:
            failures.append(f"{relative_path}: screen texture sampling is forbidden")

    require_contains(
        contents["Assets/Resources/ArcaneVFX/ArcaneAdditive.shader"],
        "input.color.rgb * _Intensity",
        "ArcaneAdditive.shader",
        failures,
    )

    urp = require_file("Assets/Settings/UniversalRP.asset", failures)
    require_contains(urp, "m_RequireDepthTexture: 0", "UniversalRP.asset", failures)
    require_contains(urp, "m_RequireOpaqueTexture: 0", "UniversalRP.asset", failures)

    if failures:
        print("Arcane VFX validation failed:")
        for failure in failures:
            print(f"- {failure}")
        return 1

    print("Arcane VFX static validation passed (0 failures).")
    return 0


if __name__ == "__main__":
    sys.exit(main())

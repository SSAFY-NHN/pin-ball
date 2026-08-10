# Title Scene Subtle Bloom Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add restrained URP bloom to the title screen so its bright background, logo, and button accents glow without changing UI layout or interaction.

**Architecture:** Render the existing title Canvas through the existing main camera, enable that camera's URP post-processing, and add a scene-authored global Volume that references a dedicated title Volume Profile. Keep bloom data out of `TitleDirector`; the scene owns rendering configuration and the script continues to own only presentation timing and navigation.

**Tech Stack:** Unity 6, Universal Render Pipeline, Unity Volume framework, Unity UI, PC WebGL

## Global Constraints

- Preserve the existing title UI hierarchy, RectTransform values, CanvasScaler, GraphicRaycaster, EventSystem, button callback, and DOTween entrance sequence.
- Do not modify title artwork, animation timing, gameplay post-processing, or navigation behavior.
- Use a title-only Volume Profile with Bloom threshold `0.9`, intensity `0.35`, scatter `0.62`, clamp `8`, and high-quality filtering disabled.
- Keep the current main camera HDR setting enabled and ensure its Volume layer mask includes the title Volume.
- Do not add packages, custom shaders, duplicate glow images, or runtime post-processing installers.

---

## File Structure

- Create `pin-ball/Assets/Settings/TitleBloomProfile.asset`: title-only URP Volume Profile containing Bloom overrides.
- Create `pin-ball/Assets/Settings/TitleBloomProfile.asset.meta`: stable Unity GUID for the new profile reference.
- Modify `pin-ball/Assets/01. Scenes/01. Title.unity`: camera-backed Canvas, enabled camera post-processing, and scene-authored global Volume referencing the title profile.
- Do not modify `pin-ball/Assets/02. Scripts/TitleDirector.cs`: it remains responsible only for the existing entrance and start-button flow.

### Task 1: Configure title rendering and restrained bloom

**Files:**
- Create: `pin-ball/Assets/Settings/TitleBloomProfile.asset`
- Create: `pin-ball/Assets/Settings/TitleBloomProfile.asset.meta`
- Modify: `pin-ball/Assets/01. Scenes/01. Title.unity`

**Interfaces:**
- Consumes: the existing title `Canvas`, `Main Camera`, `CanvasScaler`, `GraphicRaycaster`, `EventSystem`, logo, and start button serialized in `01. Title.unity`.
- Produces: a Screen Space Camera Canvas rendered by `Main Camera`, a camera with URP post-processing enabled, and a global title Volume using `TitleBloomProfile`.

- [ ] **Step 1: Record the baseline title-scene invariants**

Run:

```powershell
rg -n "m_Name: (Canvas|Main Camera|Img_Background|Img_Logo|Btn_Start)|m_RenderMode:|m_RenderPostProcessing:|m_HDR:" "pin-ball/Assets/01. Scenes/01. Title.unity"
```

Expected baseline: all five named objects exist, Canvas has `m_RenderMode: 0`, and Main Camera has `m_HDR: 1`. Save the output for comparison after editing.

- [ ] **Step 2: Create the dedicated title Volume Profile**

Create `TitleBloomProfile.asset` as a Unity `VolumeProfile` with one active `UnityEngine.Rendering.Universal.Bloom` component. Set every listed property as an override:

```text
threshold = 0.9
intensity = 0.35
scatter = 0.62
clamp = 8.0
highQualityFiltering = false
```

Keep the component active. Create its `.meta` file with a new stable GUID and use that exact GUID in the scene's Volume profile reference.

- [ ] **Step 3: Route the existing Canvas through Main Camera**

In `01. Title.unity`, change the existing Canvas to Screen Space Camera (`m_RenderMode: 1`), set its world camera to the existing Main Camera component, and retain its current sorting, scaler, raycaster, hierarchy, and RectTransform values. Use a plane distance that keeps the UI inside the camera clipping range; use `100` unless the existing camera near/far planes require a smaller value.

- [ ] **Step 4: Enable post-processing and add the global Volume**

Set the existing Main Camera's URP additional camera data to `m_RenderPostProcessing: 1`. Add one active GameObject named `Title Bloom Volume` with an enabled `Volume` component configured as global, priority `10`, weight `1`, blend distance `0`, and the profile reference created in Step 2. Place it on a layer already included in the camera's Volume layer mask; use Default when the mask includes Default.

- [ ] **Step 5: Run structural validation**

Run:

```powershell
rg -n "m_Name: (Canvas|Main Camera|Img_Background|Img_Logo|Btn_Start|Title Bloom Volume)|m_RenderMode: 1|m_RenderPostProcessing: 1|m_HDR: 1|m_IsGlobal: 1|m_Priority: 10|m_Weight: 1" "pin-ball/Assets/01. Scenes/01. Title.unity"
rg -n "Bloom|0\.9|0\.35|0\.62|8" "pin-ball/Assets/Settings/TitleBloomProfile.asset"
git diff --check -- "pin-ball/Assets/01. Scenes/01. Title.unity" "pin-ball/Assets/Settings/TitleBloomProfile.asset" "pin-ball/Assets/Settings/TitleBloomProfile.asset.meta"
```

Expected: the original named objects remain, the new Volume exists, camera-backed rendering and post-processing are enabled, all bloom values appear in the profile, and `git diff --check` reports no errors introduced by these files. Existing whitespace in the user's uncommitted scene is not to be reformatted wholesale.

- [ ] **Step 6: Verify in Unity Game view**

Open `01. Title.unity`, enter Play Mode, and confirm:

```text
- Background, logo, and start button keep their pre-change positions.
- Logo and start button appear in the existing order and timing.
- Bright accents glow softly; dark background regions remain dark.
- Logo edges and button text remain readable without a milky full-screen haze.
- Start button receives the click and loads the Game scene.
- No missing-script, missing-profile, or render-pipeline errors appear in Console.
- Layout remains stable at 1920x1080 and one wider Game-view aspect ratio.
```

If the glow is too broad, increase threshold to `1.0` before reducing intensity. If it is too faint, increase intensity only as far as `0.45`. Record the final profile values in the implementation handoff.

- [ ] **Step 7: Commit the isolated bloom change**

Run:

```powershell
git add -- "pin-ball/Assets/01. Scenes/01. Title.unity" "pin-ball/Assets/Settings/TitleBloomProfile.asset" "pin-ball/Assets/Settings/TitleBloomProfile.asset.meta"
git diff --cached --check
git diff --cached --name-only
git commit -m "feat: add subtle bloom to title scene"
```

Expected: the staged file list contains exactly those three paths. If unrelated user changes already exist inside `01. Title.unity`, do not commit the scene wholesale; use a patch-level staging workflow or leave the implementation uncommitted and report the overlap.

## Completion Criteria

- The title Canvas is camera-rendered and visually retains its layout.
- Bright title-screen accents receive restrained bloom while dark areas and text remain clear.
- The start button and entrance sequence behave exactly as before.
- Gameplay rendering and post-processing remain untouched.
- Only the title scene and its dedicated Volume Profile are part of the implementation change.

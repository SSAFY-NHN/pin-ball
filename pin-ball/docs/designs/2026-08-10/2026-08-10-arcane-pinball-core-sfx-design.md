# Arcane Pinball Core SFX Design

## Goal

Create a prototype-ready core SFX set for the pinball game before deciding the BGM direction. The set must reinforce the current dark arcane pixel-fantasy presentation with mechanical metal impacts, cyan energy, and violet magical resonance while preserving the existing audio architecture and PC WebGL target.

## Approved scope

The deliverable contains 12 gameplay event categories and 18 mono WAV files. Frequently repeated collision categories receive three authored variants; all other categories receive one file.

- Pinball launch: 1
- General wall collision: 3
- Large bumper collision: 3
- Small pin collision: 3
- Reflector collision: 1
- Magnet activation: 1
- Goal entry: 1
- Out-zone failure: 1
- UI click: 1
- Successful purchase or upgrade: 1
- Victory result: 1
- Defeat result: 1

BGM, combat attack and hit sounds, unit voices, ambient loops, and third-party audio-generation services are outside this scope.

## Sound direction

Use layered procedural synthesis as the primary production method. Combine short metallic transients, spring or mechanism layers, filtered noise, tonal synth layers, and restrained magical tails. This approach provides a consistent identity, reproducible source material, and clear provenance without adding an external package or service.

The hierarchy is:

- Wall collisions are the thinnest and quietest repeated impacts.
- Small pins are brighter and more tonal than walls.
- Large bumpers are the strongest collision sound, but remain below goal and result cues.
- Reflectors use a sharp metallic snap and a directional sweep.
- The magnet uses a low suction onset and violet electronic swirl aligned to its approximately 0.4-second pulse.
- Goal and purchase cues use distinct positive rising gestures.
- Out and defeat cues use different descending gestures and must not sound excessively harsh.
- UI click is dry, short, and distinct from board collisions.
- Victory and defeat are short result stingers rather than BGM.

## File layout and names

Place the new files under the existing `Assets/06. Sounds/SFX/` path. This adds no new top-level project folder.

```text
SFX_Pinball_Launch.wav
SFX_Pinball_WallHit_01.wav
SFX_Pinball_WallHit_02.wav
SFX_Pinball_WallHit_03.wav
SFX_Pinball_BumperHit_01.wav
SFX_Pinball_BumperHit_02.wav
SFX_Pinball_BumperHit_03.wav
SFX_Pinball_PinHit_01.wav
SFX_Pinball_PinHit_02.wav
SFX_Pinball_PinHit_03.wav
SFX_Pinball_Reflector.wav
SFX_Pinball_Magnet.wav
SFX_Pinball_Goal.wav
SFX_Pinball_Out.wav
SFX_UI_Click.wav
SFX_UI_Purchase.wav
SFX_Result_Victory.wav
SFX_Result_Defeat.wav
```

## Audio specifications

- Container: WAV
- Sample rate: 44.1 kHz
- Channels: mono
- Looping: disabled
- Peak ceiling: no higher than approximately -3 dBFS
- Typical duration: 0.1 to 0.8 seconds
- Victory and defeat duration: approximately 1.5 to 2.5 seconds
- Remove unintended leading silence and silent tails.
- Apply short fades where required to prevent waveform clicks.
- Preserve headroom so overlapping impacts do not clip the SFX bus.

For Unity, short effects use `Decompress On Load` by default. Import settings may be adjusted only when an actual WebGL memory or latency measurement demonstrates a need.

## Existing architecture and minimal extension

Reuse `SoundManager`, its `AudioMixer`, its pooled SFX `AudioSource` objects, and its existing `PlaySFX(string)` entry point. Do not replace the manager or add an external audio package.

Add a focused overload or helper that selects one of several supplied SFX keys. Existing callers and exact-key playback continue to work unchanged. Repeated collision categories use authored variants plus a small runtime pitch offset. Goal, purchase, victory, and defeat cues do not use random pitch.

Apply a per-category interval of approximately 30 to 50 milliseconds to repeated wall, bumper, and pin sounds. The limiter suppresses only excessive retriggers in the same category and does not block unrelated SFX.

## Event integration

- Successful launch: play from `PinballManager` only after payment and launch validation succeed.
- General wall and small pin contact: classify and play from the existing pinball collision flow.
- Large bumper contact: play alongside the existing bumper response in `PinballObstacle`.
- Reflector contact: play after a valid ball collision in `PinballReflectorController`.
- Magnet: play only when `PinballMagnetController` accepts an activation outside cooldown.
- Goal: play once when `PinballGoal` accepts a ball.
- Out: play once when `PinballOutZone` accepts a ball.
- Purchase: play only when `ItemManager.TryPurchase` returns success; a rejected purchase remains silent.
- Victory and defeat: play once when `ResultPanel` receives the corresponding new battle state.
- UI click: connect only to approved primary progression buttons in the first slice. Do not globally instrument every button.

Each accepted gameplay event produces at most one category playback request. Sound playback must not change gameplay state or decide whether an event succeeds.

## Error handling

- Missing sound keys retain the existing descriptive `SoundManager` error behavior.
- A missing `SoundManager` reference disables only the affected audio call and must not interrupt gameplay.
- Empty variant lists do not attempt playback and report one descriptive configuration error.
- Cooldown and variant selection do not allocate per collision frame.
- Existing BGM playback, mute controls, mixer routing, and SFX pooling remain functional.

## Verification

### Audio asset checks

- Confirm all 18 expected files exist.
- Confirm WAV decoding, 44.1 kHz sample rate, mono channel count, duration, and peak ceiling.
- Detect unintended leading or trailing silence, clipping, and waveform discontinuities.

### Unity integration checks

- Confirm every file is assigned to the expected SFX key in the scene-hosted `SoundManager`.
- Confirm exact-key playback remains compatible with existing callers.
- Confirm each repeated category can select all three variants.
- Confirm same-category rate limiting does not suppress other categories.
- Confirm BGM, mute toggles, mixer routing, and pooled-source return behavior are unchanged.

### Play-mode checks

- Perform ten consecutive launches.
- Exercise repeated wall, bumper, and pin collisions.
- Activate both magnets.
- Enter all four goals and both out zones.
- Complete one successful purchase and one rejected purchase.
- Trigger victory and defeat once each.
- Confirm that repeated impacts remain readable without harsh stacking or obvious repetition.

### WebGL checks

- Confirm the PC WebGL build succeeds.
- After the browser's required initial user interaction, confirm SFX begins without abnormal latency.
- Confirm rapid collision playback does not produce audible breakup or pool starvation.

## Completion criteria

The work is complete when all 18 WAV files are present, the 12 event categories are audibly distinguishable, repeated impacts avoid obvious repetition and harsh stacking, every approved event plays at most once per acceptance, and existing BGM, mute, mixer, and SFX-pool behavior remains intact.

## AI usage record

- AI tool/model: Codex, GPT-5-based coding agent
- User request: Create SFX before BGM, using a magical-mechanical direction and a core 12-category scope.
- AI proposal: Layered procedural synthesis, 18 WAV files including collision variants, minimal extension of the existing `SoundManager`, and event-specific integration.
- AI changes in this phase: This design document only.
- User decisions: Deferred BGM; selected magical-mechanical style; selected the core 12 categories; approved collision variants, file layout, integration, quality criteria, and verification criteria.
- Important instruction: Preserve the existing project structure and behavior, make no implementation change before approval, and target Unity 6 PC WebGL.
- Verification result: Design checked against the existing `SoundManager`, mixer asset, scene BGM keys, pinball controllers, shop flow, battle state flow, and current project design documents. Audio generation and runtime verification have not started.

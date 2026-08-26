# 범용 전투 이펙트 제작 AI 사용 기록

## 사용한 AI 도구/모델

- Codex
- PixelLab MCP `create_image_pixflux`
- PixelLab MCP `edit_image_pixen`
- PixelLab MCP `animate_image`

## 사용자의 요청

- 날아가는 총알, 포격 폭발, 하늘에서 내리는 화살비, 힐 대상 표시, 지면 소환 마법진 이펙트 제작
- PixelLab MCP를 사용하고 기존 프로젝트의 픽셀 아트 스타일에 맞춰 Unity에서 사용할 수 있게 구성

## AI 제안 내용

- 물리 공격 이펙트는 기존 프로젝트의 흰색·노란색·주황색 계열을 사용
- 힐은 에메랄드·옅은 금색, 소환 마법진은 보라색·금색 계열로 구분
- 각 이펙트를 투명 배경의 9프레임 원샷 애니메이션으로 제작
- 총알은 우측 비행과 잔상 소멸, 포격은 화염에서 연기로 전환, 화살비는 낙하와 지면 충돌, 힐과 소환진은 지면에 고정된 마법 구조를 강조

## AI 실제 수정 영역

- `Assets/03. Images/Effects/ArtilleryExplosionEffect.png` 및 Unity 슬라이스 메타데이터 생성
- `Assets/03. Images/Effects/ArrowRainEffect.png` 및 Unity 슬라이스 메타데이터 생성
- `Assets/03. Images/Effects/HealTargetEffect.png` 및 Unity 슬라이스 메타데이터 생성
- `Assets/03. Images/Effects/SummoningCircleEffect.png` 및 Unity 슬라이스 메타데이터 생성
- PixelLab이 생성한 마지막 프레임의 픽셀 형태는 유지하고 알파만 단계적으로 낮춰 원샷 소멸을 정리
- 후속 요청에 따라 `BulletFlightEffect.png`, 메타데이터와 미리보기를 삭제하고 재생성 대상에서도 제외

## 사용자가 직접 결정/수정할 필요가 있는 영역

- 실제 전투 타이밍에 맞는 Animation Clip의 프레임 간격
- 발사체 속도, 포격 피격 판정 시점, 화살비 범위와 반복 횟수
- 힐 대상의 발밑 크기와 소환진의 월드 좌표 오프셋
- Sorting Layer와 Order in Layer
- 총알 비행 이펙트는 후속 요청에 따라 최종 결과물에서 제외

## 중요 프롬프트/지시

### 총알 시작 프레임

```text
Isolated pixel-art projectile VFX start frame: a small brass bullet with a white-hot tip and a short orange muzzle trail, positioned on the left side and pointing east with clear empty space ahead. Fast readable silhouette, compact sparks, transparent background, no gun, no character, no ground, no text, no border.
```

방향 보정:

```text
Flip and correct the projectile direction. The bullet nose must point clearly to the right/east and sit in the left third of the canvas, with its orange-white flame and speed trail extending behind it to the left/west. Leave empty transparent travel space ahead on the right. Keep the same compact pixel-art style and colors. No gun, character, ground, text or border.
```

애니메이션:

```text
The bullet flies extremely fast to the right across the canvas. Its orange-white trail stretches and flickers with small sparks while the projectile advances east, then the bullet exits the right edge and the remaining trail fades to transparency. Fixed camera, no gun or character.
```

### 포격 폭발

```text
Isolated pixel-art artillery impact start frame: a tiny white-hot shell impact point at the bottom center with a compact orange flash, a few upward sparks and almost no smoke yet. Strong anticipation for a large explosion, transparent background, no projectile weapon, no character, no landscape, no text, no border.
```

```text
The compact artillery impact violently detonates upward and outward into a large orange-white fireball. The blast reaches a wide peak, throws sparks and dark debris, becomes rolling gray-brown smoke, then collapses and fades to transparency by the final frame. Fixed impact point, no character.
```

### 화살비

```text
Isolated pixel-art arrow rain VFX start frame: three slim arrows have just entered from the upper area, falling diagonally downward toward the lower center. Small gold-white arrowheads, dark shafts, faint speed streaks, lots of empty transparent space below for the fall. No archer, no character, no scenery, no ground plane, no text, no border.
```

방향 보정:

```text
Make every arrow clearly fall from the top toward the bottom-right. Put arrowheads at the lower ends and fletching at the upper ends. Keep three separated arrows entering from the upper half with long downward speed streaks and open transparent space below for travel. Same pixel-art style and palette. No archer, character, landscape, text or border.
```

애니메이션:

```text
The arrows plunge rapidly downward in staggered waves while more arrows enter from the top. Long speed streaks emphasize the fall. Arrowheads reach the lower area with several small gold-white dust impact flashes, then the shafts and impact motes fade. Fixed camera, no archer, character or landscape.
```

### 힐 대상 표시

```text
Isolated pixel-art healing VFX start frame: a faint emerald and pale-gold circular healing rune on the ground below an empty target space, with a small luminous plus-shaped spark and a few soft green motes beginning to rise. Benevolent magical energy, transparent background, no character, no scenery, no text, no border.
```

```text
The emerald healing rune brightens and rotates gently on the ground. Green and pale-gold motes spiral upward around an empty target space, small plus-shaped sparks pulse, a soft vertical halo reaches peak brightness, then the ring and particles fade cleanly to transparency.
```

### 소환 마법진

```text
Isolated pixel-art summoning VFX start frame: a faint violet elliptical magic circle lying on the ground in the lower half, with dim arcane runes, segmented rings and a dark center. The circle is only beginning to appear, with tiny purple sparks. Transparent background, no creature, no caster, no scenery, no text outside the runes, no border.
```

```text
The violet ground summoning circle draws itself in glowing segments. Concentric rings rotate in opposite directions, arcane runes ignite one after another, purple wisps rise vertically from the center, the portal flashes at full power, then collapses into fading violet sparks. No summoned creature appears.
```

## PixelLab 작업 식별자

- 총알 시작/보정/애니메이션: `6b49675d-bb77-4d57-894f-4a0372938b1a`, `91e3572b-6a87-4d20-af0d-553ff51b55ba`, `d41634fd-46ba-4845-9a84-b03e88d5de7b`
- 포격 시작/애니메이션: `ee858419-745d-4652-9600-27337db653b8`, `0c6f9c95-e188-486e-8d7e-5d0f2cbe9488`
- 화살비 시작/보정/애니메이션: `313ab73c-1850-4bdd-b948-af1918f19b8a`, `160ebfd8-ecd0-470d-bedb-0ed4eba153cd`, `8348ac1b-6c03-4689-90cd-f99c77f1c94b`
- 힐 시작/애니메이션: `0a08b8e4-350d-45cf-8b86-9e675d4f729b`, `5dd80c75-b9b8-4fde-939d-314bfc86e907`
- 소환진 시작/애니메이션: `9eed1a88-f57e-46c8-b710-91ea9d610f7a`, `22d6cd09-a59d-4201-aa24-5ca193182129`

## 테스트/검증 결과

- 포격·힐 `1152x128`, 화살비 `1440x160`, 소환진 `1440x128` 크기 확인
- 최종 유지된 4개 시트 모두 투명 알파와 9개 비어 있지 않은 프레임 확인
- Unity Sprite Multiple 메타데이터의 프레임 이름·개수·전체 셀 좌표 확인
- 재생성 전후 메타데이터 해시가 유지되고, 최종 유지된 4개 에셋 GUID가 프로젝트 안에서 중복되지 않음을 확인
- 실제 Unity Editor 임포트 및 게임 화면 재생은 수행하지 않음

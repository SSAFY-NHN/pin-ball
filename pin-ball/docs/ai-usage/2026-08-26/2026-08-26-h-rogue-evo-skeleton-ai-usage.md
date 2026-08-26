# H_Rogue 해골 상위 형태 제작 AI 사용 기록

## 사용한 AI 도구/모델

- Codex
- PixelLab MCP `edit_image`

## 사용자 요청

- `Assets/03. Images/Humans/Rogue`의 `H_Rogue`를 상위 형태의 해골 캐릭터로 제작
- 기존 `H_Rogue`의 픽셀 아트 스타일과 질감 유지

## AI 제안 내용

- 원본 `H_Rogue.png`를 편집 대상이자 스타일 기준 이미지로 사용
- 기존 84×84 캔버스, 정면 대기 자세, 비율, 후드와 갈색 가죽 장비, 짧은 칼, 픽셀 밀도와 음영 방향을 유지
- 노출된 얼굴과 손을 낡은 상아색 두개골과 뼈로 변경
- 상위 형태가 읽히도록 낡은 천 질감과 작은 어깨 보강구를 추가하되 과도한 무기, 이펙트, 배경은 제외

## AI 실제 수정 영역

- 신규 이미지 `Assets/03. Images/Humans/Rogue/H_Rogue_EvoSkeleton.png` 생성
- 신규 9프레임 공격 시트 `Assets/03. Images/Humans/Rogue/H_Rogue_EvoSkeleton_Attack.png` 생성
- 신규 9프레임 걷기 시트 `Assets/03. Images/Humans/Rogue/H_Rogue_EvoSkeleton_Walk.png` 생성
- 기존 `H_Rogue` 및 걷기/공격 스프라이트 시트는 수정하지 않음

## 사용자가 직접 결정/수정할 필요가 있는 영역

- 정지 캐릭터 원화와 9프레임 공격/걷기 스프라이트 시트를 포함
- 실제 전투 사용 시 프리팹/Animation Clip 연결은 별도 작업 필요
- Unity가 신규 에셋을 처음 임포트할 때 생성하는 `.meta`와 Sprite Import 설정은 Editor에서 확인 필요

## 중요 프롬프트/지침

```text
Transform only this rogue into its visibly stronger, elite skeletal evolution.
Replace the living head and exposed flesh with an aged ivory skull and subtle skeletal hands, while keeping the exact standing pose, body proportions, canvas placement, silhouette readability, and transparent background.
Keep the original rogue identity through the same brown hooded leather-and-cloth outfit, belts, boots, small blade/gear shapes, warm muted palette, restrained highlights, dark selective outline, native pixel grid, pixel density, shading direction, handmade texture, and low-detail 2D game-sprite finish.
Make the upper form more formidable using slightly more weathered/torn fabric and modest reinforced armor details, but do not add wings, aura, text, background, extra characters, oversized weapons, smooth painting, anti-aliasing, or high-resolution detail.
The result must remain an 84x84 transparent Unity-ready pixel sprite with clean alpha edges.
```

- PixelLab job ID: `db452282-01da-4661-a597-56f36410da54`
- Seed: `348217`

## 테스트/검증 결과

- PNG 크기: 84×84
- 픽셀 포맷: 32-bit ARGB
- 알파 값: 0 또는 255만 사용하여 투명 경계에 중간 알파 없음
- 원본과 10배 최근접 보간 확대 비교 완료
- 해골 얼굴, 뼈 손, 보강 장비가 식별되며 원본의 후드, 갈색 계열 장비, 짧은 칼, 픽셀 아트 질감이 유지됨
- Unity Editor 임포트와 실제 게임 화면 검증은 수행하지 않음

## 후속 작업: 공격 모션

- 원본 `H_Rogue_Attack.png`의 84×84 프레임 9개를 편집 대상으로 사용
- `H_Rogue_EvoSkeleton.png`를 외형 참조로 사용하여 PixelLab MCP `edit_image` 참조 편집 3회를 수행
- 원본 프레임이 `0-1-2-3-4-3-2-1-0` 완전 대칭 구조임을 픽셀 해시로 확인
- 생성 결과도 같은 역순 구조로 정리하고, AI 편집 중 흔들린 프레임의 수평 기준점을 원본에 맞게 보정

```text
Apply the exact character appearance from the reference image to each input attack frame.
Change only the living rogue into the same elite skeletal rogue: aged ivory skull, skeletal hands, brown hooded leather-and-cloth gear, modest reinforced shoulder armor, same muted warm palette, selective dark outline, native low-detail pixel texture, and the same body scale as the reference.
Preserve every input frame's exact attack pose, body orientation, weapon position and slash trajectory, foot placement, canvas coordinates, framing, timing relationship, 84x84 size, transparent background, and hard pixel edges.
Keep the nine-frame animation visually consistent across batches.
Do not redesign the motion, move or enlarge the weapon, add effects, aura, text, background, extra objects, anti-aliasing, or smooth painted detail.
```

- PixelLab job IDs: `2366bf5a-b08a-44c4-915f-14d223231c5e`, `66349c0d-047a-4f5a-a411-117573661ad2`, `2a716922-24ec-48b2-ac3e-93a63662b285`
- Seed: `741903`
- 최종 시트 크기: 756×84, 9프레임
- 모든 프레임의 알파 값은 0 또는 255이며, 각 프레임이 비어 있지 않음을 확인
- 프레임 0/8, 1/7, 2/6, 3/5의 픽셀 해시가 각각 동일하고 4번 프레임이 공격 정점임을 확인

## 후속 작업: 걷기 모션

- 원본 `H_Rogue_Walk.png`의 84×84 프레임 9개를 편집 대상으로 사용
- `H_Rogue_EvoSkeleton.png`를 외형 참조로 사용하여 PixelLab MCP `edit_image` 참조 편집 3회를 수행
- 원본의 각 보행 자세, 팔 흔들림, 다리 보폭, 발 접지 높이와 캔버스 기준점을 유지하도록 정렬
- 시작/종료 프레임은 기존 `H_Rogue_EvoSkeleton.png`와 픽셀 단위로 동일하게 고정하여 자연스러운 루프를 구성
- PixelLab MCP `reduce_colors`로 9프레임 전체를 기존 캐릭터의 13색 팔레트에 함께 양자화

```text
Apply the exact character appearance from the reference image to each input walking frame.
Change only the living rogue into the same elite skeletal rogue: aged ivory skull, skeletal hands, brown hooded leather-and-cloth gear, modest reinforced shoulder armor, same muted warm palette, selective dark outline, native low-detail pixel texture, and the same body scale as the reference.
Preserve every input frame's exact walking pose, arm swing, leg stride, foot contact, body bob, body orientation, dagger and gear position, canvas coordinates, framing, timing relationship, 84x84 size, transparent background, and hard pixel edges.
Keep the nine-frame walk cycle visually consistent across batches and suitable for a seamless loop.
Do not redesign the gait, turn it into an attack or idle pose, add effects, aura, text, background, extra objects, anti-aliasing, or smooth painted detail.
```

- PixelLab edit job IDs: `9f9c504a-fcd1-4fe6-8814-e7a630b38e76`, `cf581ed9-c791-435d-aff1-8ba4286d5e0d`, `d96acadc-a22f-42f0-abf5-2ef572190394`
- PixelLab palette job ID: `651c1eb2-8141-429b-a9e5-5eed0307200a`
- Seed: `194628`
- 최종 시트 크기: 756×84, 9프레임
- 알파 값은 0 또는 255이며, 각 프레임이 비어 있지 않음을 확인
- 프레임 0과 8의 픽셀 해시가 동일하며 `H_Rogue_EvoSkeleton.png`와도 픽셀 차이가 0임을 확인
- 9프레임 전체가 기존 캐릭터의 13색 팔레트를 공유함을 확인

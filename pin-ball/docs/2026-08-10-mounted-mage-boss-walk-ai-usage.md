# H_MountedMageBoss 말 걷기 애니메이션 AI 사용 기록

- 작업일: 2026-08-10
- 사용자 요청: `H_MountedMageBoss`가 탄 말의 걷기 모션 제작.
- 사용 도구: Codex, PixelLab MCP(`animate_image`, `get_image`), Unity MCP/CLI(`import_asset`, `eval`, 에셋·애니메이션·콘솔 검증).

## 제작 내용

- 오른쪽을 향한 기마 Boss의 제자리 걷기 루프를 제작했다.
- 말의 앞·뒤 네 다리가 교차하는 작은 보폭의 걷기 동작으로 구성했다.
- 말 몸통과 머리에는 작은 상하 움직임만 적용했다.
- 기수는 안장에 안정적으로 앉아 말의 움직임을 작게 따라가도록 했다.
- 지팡이는 원래의 세운 휴대 자세를 유지하고 공격·섬광·마법 효과는 제외했다.
- PixelLab 결과 9프레임 중 마지막은 시작 자세와 같은 루프 보조 프레임이며, Unity Clip은 앞의 8프레임을 사용한다.

## PixelLab 생성 기록

- 작업 ID: `18232095-9e60-419a-9ae0-78c6c9d66a43`
- seed: `48226`
- 최종 프롬프트:

  > A simple natural horse walk cycle in place while facing right, carrying the mounted mage. Move all four horse legs in a clear alternating walking rhythm with small steps: one front leg and the opposite hind leg advance, pass through the standing pose, then the other pair advance. Lift the hooves only slightly and keep them near the same ground line with no sliding or movement across the canvas. Add a small natural up-and-down bob to the horse body and head. Keep the rider seated securely, following the horse with only a slight bob. Keep the magic staff upright in the original relaxed position. No casting, no flash, no attack, no running, no jumping, and no camera movement. Return smoothly to the exact original pose for a seamless loop. Preserve the exact rider, dark armor, brown horse, saddle, staff, purple crystal, palette, pixel-art detail, alignment, and transparent background.

## 생성 파일

- `Assets/03. Images/Humans/Boss/H_MountedMageBoss_Walk.png`
- `Assets/03. Images/Humans/Boss/H_MountedMageBoss_Walk.png.meta`
- `Assets/05. Animations/Humans/Boss/H_MountedMageBoss_Walk.anim`
- `Assets/05. Animations/Humans/Boss/H_MountedMageBoss_Walk.controller`
- `Assets/04. Prefabs/Humans/Boss/H_MountedMageBoss_WalkPreview.prefab`

## 검증 결과

- 스프라이트 시트: 1152x128, 128x128 Sprite 9개.
- 임포트: Multiple Sprite, Point 필터, 무압축, 100 PPU, 밉맵 비활성, Clamp.
- AnimationClip: 유효한 Sprite 키 8개, 12fps, 0.667초, 루프 활성.
- AnimatorController: 기본 상태 `Walk`, Motion 연결 정상.
- Preview 프리팹: 시작 Sprite와 AnimatorController 연결 정상.
- 작업 이후 신규 Unity 콘솔 오류 없음.

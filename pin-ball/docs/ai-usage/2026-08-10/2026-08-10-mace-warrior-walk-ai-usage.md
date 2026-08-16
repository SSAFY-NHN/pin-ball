# H_MaceWarrior 걷기 애니메이션 AI 사용 기록

- 작업일: 2026-08-10
- 사용자 요청: `H_MaceWarrior`의 걷기 모션 제작.
- 사용 도구: Codex, PixelLab MCP(`animate_image`, `get_image`), Unity MCP/CLI(`import_asset`, `eval`, 에셋·애니메이션·콘솔 검증).

## 제작 내용

- 오른쪽을 바라보는 제자리 걷기 루프를 제작했다.
- 작은 보폭으로 한쪽 발 전진 → 중앙 통과 → 반대 발 전진 순서를 구성했다.
- 상체는 중앙에 유지하고 작은 상하 움직임만 사용했다.
- 노란 방패는 몸 앞에 유지하고 메이스는 허리 높이의 수평 휴대 자세로 고정했다.
- 메이스 휘두르기, 방패 공격, 달리기, 점프는 제외했다.
- PixelLab 결과 9프레임 중 마지막은 시작 자세와 같은 루프 보조 프레임이며, Unity Clip은 앞의 8프레임을 사용한다.

## PixelLab 생성 기록

- 작업 ID: `f5916116-150c-4771-867a-20f06d769a69`
- seed: `48224`
- 최종 프롬프트:

  > A simple clean walk cycle in place while facing right. Alternate the legs with small readable steps: one foot forward and the other back, pass through the center, then switch. Add only a slight natural up-and-down body bob and keep the torso centered. Keep the yellow shield held steadily in front of the torso. Keep the same golden-handled mace with gray metal head held horizontally at waist height and connected to the hand throughout the entire walk. Do not raise, swing, or attack with either weapon. Keep both feet near the same ground line with no sliding, jumping, running, or movement across the canvas. Return smoothly to the exact original pose for a seamless loop. Preserve the exact character, armor, helmet, shield, mace, palette, crisp pixel art, alignment, and transparent background.

## 생성 파일

- `Assets/03. Images/Humans/MaceWarrior/H_MaceWarrior_Walk.png`
- `Assets/03. Images/Humans/MaceWarrior/H_MaceWarrior_Walk.png.meta`
- `Assets/05. Animations/Humans/MaceWarrior/H_MaceWarrior_Tiny32_Walk.anim`
- `Assets/05. Animations/Humans/MaceWarrior/H_MaceWarrior_Tiny32_Walk.controller`
- `Assets/04. Prefabs/Humans/MaceWarrior/H_MaceWarrior_Tiny32_WalkPreview.prefab`

## 검증 결과

- 스프라이트 시트: 756x84, 84x84 Sprite 9개.
- 임포트: Multiple Sprite, Point 필터, 무압축, 100 PPU, 밉맵 비활성, Clamp.
- AnimationClip: 유효한 Sprite 키 8개, 12fps, 0.667초, 루프 활성.
- AnimatorController: 기본 상태 `Walk`, Motion 연결 정상.
- Preview 프리팹: 시작 Sprite와 AnimatorController 연결 정상.
- 작업 이후 신규 Unity 콘솔 오류 없음.

# H_Archer 걷기 애니메이션 AI 사용 기록

- 작업일: 2026-08-10
- 사용자 요청: `H_Archer`의 걷기 애니메이션 제작.
- 사용 도구: Codex, PixelLab MCP(`animate_image`, `get_image`), Unity MCP/CLI(`import_asset`, `eval`, 에셋·애니메이션·콘솔 검증).

## 제작 내용

- 오른쪽을 바라보는 제자리 걷기 루프를 제작했다.
- 작은 보폭으로 한쪽 발 전진 → 중앙 통과 → 반대 발 전진 순서가 읽히도록 구성했다.
- 상체는 중앙에 유지하고 작은 상하 움직임만 사용했다.
- 활은 휴대 자세로 유지하며 조준·당기기·발사 동작과 화살은 제외했다.
- PixelLab 결과 9프레임 중 마지막 프레임은 시작 자세와 동일한 루프 보조 프레임이다.
- Unity AnimationClip은 중복 정지 구간을 피하기 위해 앞의 8프레임을 사용하고, 12fps·0.667초·루프로 설정했다.

## PixelLab 생성 기록

- 작업 ID: `b4a2ae0d-9a51-43d1-8be8-d4b08de02a34`
- seed: `48222`
- 최종 프롬프트:

  > A simple clean walk cycle in place while facing right. Alternate the legs with small readable steps: one foot forward and the other back, pass through the center, then switch. Add only a slight natural up-and-down body bob. Keep the torso steady and centered. Carry the bow securely in the same relaxed position; the free arm may swing only a little. Do not raise, draw, or fire the bow, and do not create an arrow. Keep both feet near the same ground line with no sliding, jumping, running, or forward movement across the canvas. Return smoothly to the exact original pose for a seamless loop. Preserve the exact character, face, hat, clothing, bow, palette, crisp pixel art, alignment, and transparent background.

## 변경 파일

- `Assets/03. Images/Humans/Archer/H_Archer_Walk.png`
- `Assets/03. Images/Humans/Archer/H_Archer_Walk.png.meta`
- `Assets/05. Animations/Humans/Archer/H_Archer_Tiny32_Walk.anim`
- `Assets/04. Prefabs/Humans/Archer/H_Archer_Tiny32_WalkPreview.prefab`

## 검증 결과

- 스프라이트 시트: 756x84, 84x84 Sprite 9개.
- 임포트: Multiple Sprite, Point 필터, 무압축, 100 PPU, 밉맵 비활성, Clamp.
- AnimationClip: 유효한 Sprite 키 8개, 12fps, 0.667초, 루프 활성.
- AnimatorController: 기본 상태 `Walk`, Motion 연결 정상.
- Preview 프리팹: `H_Archer_Walk_0` 연결 정상.
- 작업 이후 신규 Unity 콘솔 오류 없음.

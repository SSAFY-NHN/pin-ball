# H_MaceWarrior 메이스 공격 애니메이션 AI 사용 기록

- 작업일: 2026-08-10
- 사용자 요청: `H_MaceWarrior`가 보유한 무기로 공격하는 모션 제작.
- 사용 도구: Codex, PixelLab MCP(`animate_image`, `get_image`), Unity MCP/CLI(`create_folder`, `import_asset`, `eval`, 에셋·애니메이션·콘솔 검증).

## 제작 내용

- 원본 캐릭터는 오른쪽을 향하며, 왼손 방패와 오른손의 긴 황금 손잡이·회색 철퇴 머리 메이스를 사용한다.
- 공격 순서는 수평 준비 자세 → 몸 뒤로 감아 올리기 → 머리 위 준비 → 오른쪽 전방 내려치기 → 낮은 타격 자세 → 반동 → 준비 자세 복귀의 9프레임이다.
- 방패는 방어 자세로 유지하고 메이스만 휘두르도록 했다.
- 발은 고정하고 작은 상체 기울기만 사용했다.
- 추가 무기, 마법 효과, 잔상, 점프는 제외했다.

## PixelLab 생성 기록

- 작업 ID: `0fdce2dc-2f15-4af4-a37c-bdcd6468efe6`
- seed: `48223`
- 최종 프롬프트:

  > A simple one-handed mace attack facing right. Keep the yellow shield held steadily in front of the torso for defense. With the other hand, lift the same long golden-handled mace and gray metal mace head from the horizontal ready position, raise it above the shoulder, then swing it in one clear compact overhead arc down toward the ground in front on the right. Show a brief impact pose, a small recoil, and return the mace to the exact original horizontal ready position. Keep the mace connected to the hand and preserve its exact size, colors, handle, and round metal head. Keep both feet planted with only a small body lean. No extra weapons, no shield attack, no magic effects, no motion trails, no jumping, and no camera movement. Preserve the exact character, armor, helmet, shield, palette, crisp pixel art, alignment, and transparent background.

## 생성 파일

- `Assets/03. Images/Humans/MaceWarrior/H_MaceWarrior_Attack.png`
- `Assets/03. Images/Humans/MaceWarrior/H_MaceWarrior_Attack.png.meta`
- `Assets/05. Animations/Humans/MaceWarrior/H_MaceWarrior_Tiny32_Attack.anim`
- `Assets/05. Animations/Humans/MaceWarrior/H_MaceWarrior_Tiny32_Attack.controller`
- `Assets/04. Prefabs/Humans/MaceWarrior/H_MaceWarrior_Tiny32_AttackPreview.prefab`

## 검증 결과

- 스프라이트 시트: 756x84, 84x84 Sprite 9개.
- 임포트: Multiple Sprite, Point 필터, 무압축, 100 PPU, 밉맵 비활성, Clamp.
- AnimationClip: 유효한 Sprite 키 10개, 12fps, 0.833초, 비루프.
- AnimatorController: 기본 상태 `Attack`, Motion 연결 정상.
- Preview 프리팹: 시작 Sprite와 AnimatorController 연결 정상.
- 작업 이후 신규 Unity 콘솔 오류 없음.

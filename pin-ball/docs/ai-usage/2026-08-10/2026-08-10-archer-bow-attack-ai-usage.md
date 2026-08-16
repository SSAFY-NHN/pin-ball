# H_Archer 활 공격 애니메이션 AI 사용 기록

- 작업일: 2026-08-10
- 사용자 요청: `Assets/03. Images/Humans/Archer/H_Archer.png` 캐릭터가 오른쪽을 향해 활을 당기고 화살을 발사한 뒤 준비 자세로 돌아오는 공격 모션 제작.
- 사용 도구: Codex, PixelLab MCP(`animate_image`, `get_image`), Unity MCP/CLI(`import_asset`, `eval`, 에셋·애니메이션·콘솔 검증).

## 생성 및 적용 내용

- 원본 캐릭터의 84x84 투명 캔버스, 픽셀 아트 비율, 복장, 모자, 활, 색상과 오른쪽 방향을 유지했다.
- 공격 순서는 준비 자세 → 활 올리기 → 화살 메기기 → 시위 당기기 → 완전 조준 → 발사 → 반동 → 활 내리기 → 준비 자세 복귀의 9프레임으로 구성했다.
- 첫 생성 결과는 초반 프레임에서 활이 사라지고 발사 자세로 끝나 채택하지 않았다.
- 개선 생성에서는 원본 이미지를 시작·종료 키프레임으로 모두 고정해 활이 계속 보이고 마지막 자세가 원본으로 복귀하도록 했다.
- 최종 9프레임을 756x84 가로 스프라이트 시트로 결합했다.
- Unity에서 84x84 크기의 Sprite 9개로 슬라이스하고 Point 필터, 무압축, 100 PPU, 밉맵 비활성, Clamp 설정을 적용했다.
- 기존 `H_Archer_Tiny32_Attack.anim`의 끊긴 스프라이트 참조를 새 시트로 교체하고 12fps, 비루프, 10개 키(마지막 프레임 유지 포함), 0.833초로 구성했다.
- 기존 공격 Preview 프리팹의 시작 Sprite 참조도 새 시트의 첫 프레임으로 복구했다.

## PixelLab 생성 기록

- 미채택 초안: 작업 ID `b147ad59-2dc1-49fb-b102-0ec9ddfe9946`, seed `48217`.
- 최종 결과: 작업 ID `63a55268-cf52-4451-9bb3-b0e67064e28c`, seed `48218`.
- 최종 프롬프트:

  > Perform one complete bow-shot cycle while facing right: keep the bow continuously visible; lift and aim the bow, nock one arrow, pull the bowstring smoothly back to the cheek, hold full draw for a beat, release the arrow to the right with a small recoil, then lower the bow and return to the exact original ready pose. Keep both feet fixed, body centered, and preserve identity, outfit, hat, bow design, palette, crisp pixel scale, and transparency. No magic effects, no extra weapons, no camera movement.

## 변경 파일

- `Assets/03. Images/Humans/Archer/H_Archer_Attack.png`
- `Assets/03. Images/Humans/Archer/H_Archer_Attack.png.meta`
- `Assets/05. Animations/Humans/Archer/H_Archer_Tiny32_Attack.anim`
- `Assets/04. Prefabs/Humans/Archer/H_Archer_Tiny32_AttackPreview.prefab`

## 검증 결과

- 최종 시트 크기: 756x84.
- Unity Sprite: 9개, 모두 84x84.
- AnimationClip: 12fps, 비루프, 10개 Sprite 키, 0.833초.
- 시작 프레임은 원본과 픽셀 단위로 동일하며 종료 프레임도 보이는 픽셀 기준으로 원본 자세와 동일하다.
- 각 프레임의 불투명 픽셀 바운드는 84x84 셀 내부에 있고 프레임 간 캐릭터 중심과 발 위치가 유지된다.
- 작업 중 새 Unity 콘솔 오류는 발생하지 않았다. 콘솔에는 이번 작업 이전부터 존재하던 씬 PPtr, Manager 등록, BGM 누락 오류만 남아 있다.

## 사용자 확인 권장 항목

- 실제 게임 공격 타이밍과 비교해 12fps 속도 및 완전 조준 프레임의 체감 길이가 적절한지 확인한다.
- 화살 발사 판정/투사체 생성 시점은 중앙의 발사 전환 프레임(4~5번)에 맞추는 것을 권장한다.

## 2026-08-10 두 손 사격 교정

- 사용자 피드백: 기존 조준 프레임에서 시위를 당기는 뒤팔과 뒤손이 몸통에 묻혀 한 손으로 활을 쏘는 것처럼 보임.
- 수정 내용:
  - 시작과 종료 프레임은 원본 자세 그대로 유지했다.
  - 조준 구간에서 앞팔은 오른쪽으로 뻗고 앞손이 활 손잡이를 계속 잡도록 했다.
  - 뒤팔꿈치는 몸통 왼쪽으로 명확히 돌출되고, 굽힌 뒤팔과 뒤손이 뺨까지 이어져 시위를 당기도록 했다.
  - 발사 뒤에도 앞손은 활을 유지하고 뒤손만 뺨 뒤로 반동하도록 했다.
- 최종 PixelLab 작업 ID: `f53eaa02-2b95-4123-8eef-928ccebc51f8`, seed `48220`.
- 중간 7프레임 직접 편집 작업 `e13b41b6-e34b-4215-ba1b-5eaa27755835`은 서버 대기가 길어 채택하지 않았다.
- 최종 프롬프트:

  > Perform one anatomically correct TWO-HANDED bow shot facing right. Keep both arms separated and visible. The front arm extends right and its hand stays wrapped around the bow's center grip. The rear arm bends clearly: its elbow projects backward to the LEFT of the torso, the forearm runs to the face, and the rear hand visibly grips the bowstring and pulls it to the jaw/cheek. At full draw show the triangle formed by the extended front arm, string, and bent rear arm. Release one arrow right; the rear hand recoils behind the cheek while the front hand still holds the bow, then return to the exact original pose. Never hide either arm, never shoot one-handed, and never float the bow or arrow. Keep feet fixed and preserve identity, hat, clothes, bow, palette, crisp pixels, alignment, and transparency. No effects or camera movement.
- Unity 재검증: GUID 유지, 84x84 Sprite 9개, 유효한 AnimationClip Sprite 키 10개, 12fps, 0.833초, Preview 시작 Sprite 연결 정상.

## 2026-08-10 단순 동작으로 재정리

- 사용자 피드백: 팔 동작을 지나치게 세세하게 표현하지 말고, 활을 드는 순간 반대 손도 바로 활 쪽으로 이동해야 함.
- 최종 동작: 준비 → 활과 반대 손을 함께 올림 → 두 손이 활 중앙에 모임 → 짧게 당김 → 발사 → 두 손과 활을 함께 내림 → 준비 자세.
- 팔꿈치 과장과 큰 상체 이동을 제거하고 손과 활의 접촉만 읽히도록 단순화했다.
- 최종 PixelLab 작업 ID: `f0cd1be8-47e3-4208-bbff-f24f7ba8c4b1`, seed `48221`.
- 최종 프롬프트:

  > A simple, clean bow attack facing right. As soon as the archer starts lifting the bow, the free hand moves to the bow at the same time. Both hands visibly meet at the bow: one hand holds the grip and the other hand touches the string beside it, then makes one short compact pull to aim. Keep both hands close to the bow throughout the raise and aim; never leave the free hand hanging at the side and never make the bow float. Release one arrow, then lower the bow and both hands together back to the exact original pose. Use small readable arm movements, no wide arm spread, no exaggerated elbow pose, and almost no body movement. Keep the feet fixed and preserve the character, clothes, hat, bow, palette, crisp pixel art, alignment, and transparent background.
- Unity 재검증: 기존 GUID 유지, 84x84 Sprite 9개, AnimationClip 키 10개 모두 유효, 12fps, 0.833초, Preview 연결 정상, 신규 콘솔 오류 없음.

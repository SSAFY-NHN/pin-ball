# 전투 달빛 연출 AI 활용 기록

- 사용한 AI 도구/모델: OpenAI Codex (GPT-5 계열)
- 사용자 요청: 웨이브 진행 중 달빛 아래에서 싸우는 듯한 전장 연출 구현. 설계 승인 절차를 생략하고 정적 확인만 수행.
- AI 제안 내용: 유닛별 Light2D나 반복 오버레이 대신 전장 전체에 하나의 불규칙한 광량 마스크를 배치하고 웨이브 상태에 따라 페이드 처리.
- AI 실제 수정 영역:
  - 달빛 마스크 이미지와 TextureImporter 메타데이터 추가
  - 그레이스케일과 알파를 광량으로 사용하는 URP 2D 전용 Additive 셰이더 및 머티리얼 추가
  - `BattleMoonlightController` 추가
  - `02. Game` 씬의 `Battle` 하위에 `BattleMoonlight` SpriteRenderer 사전 배치 및 Inspector 참조 연결
- 사용자 직접 결정/수정 필요 영역: Unity Game View에서 색상, 강도, 위치, 스케일 및 페이드 시간의 최종 미감 조정.
- 중요한 프롬프트/지시: 기존 Unlit 캐릭터 유지, 유닛별 광원 사용 금지, 씬 사전 배치 우선, 구현 후 정적 확인만 수행.
- 테스트/검증 결과: Unity 실행 및 빌드는 사용자 지시에 따라 수행하지 않음. 파일 존재, GUID 참조, 씬 fileID 참조, C#/셰이더 괄호 균형 및 `git diff --check`를 정적으로 확인.

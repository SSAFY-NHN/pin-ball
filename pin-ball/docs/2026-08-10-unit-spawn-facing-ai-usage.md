# 유닛 생성 방향 수정 AI 활용 기록

- 사용한 AI 도구/모델: Codex, GPT-5 계열 모델
- 사용자 요청: 맵에 생성된 유닛이 오른쪽을 바라보는 문제를 수정하고 왼쪽을 바라보도록 변경
- AI 제안 내용: 생성 위치 배치를 실제 이동으로 오인하지 않도록 방향 추적 기준점을 생성 및 준비 위치 복원 직후 초기화하고, 실제 원본 방향과 반대로 등록된 아군 애니메이션 방향 설정을 교정하며 전투 중 이동 방향 전환은 유지
- AI 실제 수정 영역: `BattleUnitVisual.ResetFacing` 추가, `UnitBase.Initialize` 및 `RestoreForPreparation`에서 방향 초기화 호출, `AllyUnit.prefab`의 기본 및 직업별 `sourceFacesRight` 설정 교정, 관련 EditMode 회귀 테스트 추가
- 사용자 직접 결정/수정 필요 영역: Unity Game 뷰에서 아군 소환 직후 및 웨이브 종료 후 복원 직후 왼쪽 방향을 최종 육안 확인
- 중요한 프롬프트/지시: 기존 구조와 전투 중 이동 방향 전환을 유지하고 새 생성 및 풀링 재사용 경로에 동일하게 적용
- 테스트/검증 결과: 최초 코드 수정은 EditMode 테스트를 통과했으나 실제 Play Mode에서 방향이 반대인 것을 사용자와 런타임 `SpriteRenderer.flipX=true` 값으로 확인하여 프리팹 원본 방향 설정을 추가 교정. Unity 재컴파일 성공 및 `BattleUnitVisualTests` 9개 최종 통과. 앞서 실행한 전체 EditMode 테스트는 67개 중 66개가 통과했고, 요청과 무관한 기존 자석 테스트 1개가 실패

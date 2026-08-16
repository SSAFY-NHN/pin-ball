# 적군 웨이브 외형 적용 AI 활용 기록

- 사용한 AI 도구/모델: Codex, GPT-5 계열 모델
- 사용자 요청: 1~10웨이브에 로그, 궁수, 메이스 전사, `H_Warrior`, 보스를 순차적으로 추가하고 너무 큰 임시 `H_Warrior`를 가장 큰 기본 아군과 비슷한 크기로 조정
- AI 제안 내용: 기존 적 전투 ID와 스탯 구조는 유지하면서 Human 스프라이트 애니메이션을 ID별 프로필로 연결하고, 승인된 최대 5기 편성으로 웨이브 구성을 변경
- AI 실제 수정 영역: 적 데이터 표시명, 10개 웨이브 편성, `EnemyUnit.SetData`의 시각 프로필 선택, 적 프리팹의 6개 ID별 애니메이션 프로필, 일반 적 스케일 및 체력바 역보정, EditMode 회귀 테스트
- 사용자 직접 결정/수정 필요 영역: 실제 플레이에서 웨이브별 난이도와 적 사이 간격, 보스의 상대 크기 최종 확인
- 중요한 프롬프트/지시: 메이스 전사는 `H_MaceWarrior`, `H_Warrior`는 7~9웨이브에만 사용하고 10웨이브에는 보스를 배치
- 테스트/검증 결과: Unity 컴파일 성공. `EnemyWaveVisualTests` 3개와 `BattleUnitVisualTests` 9개 통과. 전체 EditMode 테스트 70개 중 69개 통과했으며, 요청과 무관한 기존 `PinballMotionTests.Magnet_IsActiveOnlyWhileMouseIsHeld` 1개 실패

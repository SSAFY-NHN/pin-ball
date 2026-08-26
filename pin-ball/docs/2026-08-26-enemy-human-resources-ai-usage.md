# 적 인간형 캐릭터 리소스 적용 AI 활용 기록

- 사용한 AI 도구/모델: OpenAI Codex, GPT-5 계열 모델
- 사용자 요청: `03. images 리소스 추가` 커밋의 인간/적 캐릭터 리소스를 모두 전투에 적용하고 암살자를 실제 웨이브에 출전시키며 보스 Skill 애니메이션까지 포함
- AI 제안 내용: 기존 `EnemyUnit.prefab`의 `unitId`별 프로필 구조를 유지하고 6종 캐릭터의 Idle, Walk, Attack 프레임을 교체하며 선택형 Skill 프레임 재생만 추가
- AI 실제 수정 영역: `BattleUnitVisual`의 Skill 프레임 재생, 적 Skill 피드백 연결, 적 프리팹 6종 프로필, 8웨이브 암살자 1기 추가, 집중 EditMode 테스트
- 사용자 직접 결정/수정 필요 영역: 실제 Game View에서 캐릭터별 크기, 프레임 속도, 8웨이브 난이도 체감 확인
- 중요한 프롬프트/지시: 추가된 인간형 캐릭터 리소스를 전부 사용, 암살자 출전, 보스 Skill 포함, 테스트 최소화, 기존 구조와 사용자 변경 보존
- 테스트/검증 결과: 관련 `EnemyWaveVisualTests` 4개 통과. 전체 EditMode 178개 중 176개 통과했으며, 수정 후 관련 테스트는 모두 통과했다. 요청과 무관한 기존 `SoundManagerTests.DeveloperScene_RegistersStartupBgmAndEverySfxClip`은 기대 17개와 실제 19개 불일치로 실패했다.

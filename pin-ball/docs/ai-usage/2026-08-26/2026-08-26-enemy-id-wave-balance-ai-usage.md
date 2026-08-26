# 적 ID 개편 및 1~10 웨이브 밸런스 AI 사용 기록

## 사용한 AI 도구/모델

- OpenAI Codex
- Unity MCP 및 Unity 6 EditMode 배치 테스트

## 사용자 요청

- 현재 전투가 고정 웨이브인지 무작위인지 확인
- 적 표시 이름과 내부 ID를 다음과 같이 변경
  - `goblin` → `PatrolMan`
  - `goblin_archer` → `Archer`
  - `shield_guard` → `MaceWarrior`
  - `orc_warrior` → `Knights`
  - `goblin_king` → `DarkMageBoss`
- 기존 리소스를 재사용해 근접형 `Rogue` 추가
- 근접 전투력 체감을 `PatrolMan < Rogue < MaceWarrior < Knights`로 구성
- Rogue는 후열 공격 능력을 사용하지 않도록 구성
- 1~10 웨이브 편성을 균형 있게 전면 개편

## AI 제안 내용

- 전투가 무작위 추첨이 아니라 `BattleWaveData.json`에 정의된 고정 10웨이브임을 확인
- 기존 `assassin` 데이터와 Rogue 애니메이션 프로필을 근접형 `Rogue`로 재사용
- Rogue 능력치를 HP 120, 공격 16, 방어 4, 이동속도 1.6, 공격속도 0.9, 사거리 1.1, 방어선 피해 2로 제안
- Rogue의 `shadow_leap` 스킬을 제거해 후열 공격을 하지 않도록 제안
- MaceWarrior의 공격력을 18, 공격속도를 0.7로 조정해 승인된 근접 공격력 순서를 맞춤
- 5·9웨이브는 정예, 10웨이브는 보스로 유지하면서 신규 적을 단계적으로 소개하는 고정 편성을 제안
- DarkMageBoss의 75/50/25% 체력 소환군을 각 단계마다 PatrolMan 2기와 Rogue 1기로 제안

## AI 실제 수정 영역

- `Assets/Resources/Data/EnemyUnitData.json`: 표시 이름/내부 ID, Rogue 및 MaceWarrior 능력치, Rogue 스킬, 보스 소환 데이터 수정
- `Assets/Resources/Data/BattleWaveData.json`: 1~10웨이브 이름·편성·정예/보스 구성 수정
- `Assets/04. Prefabs/EnemyUnit.prefab`: 적 애니메이션 프로필 ID와 공격 효과 ID 수정, PatrolMan 프로필 추가
- 코드 리뷰에서 발견된 Knights 걷기 프레임 파일 ID 오타 수정
- `Assets/02. Scripts/02. Data/TitleData.cs`, `BattleDataTypes.cs`, `BattleManager.cs`, `SummonMinionsSkill.cs`: 런타임 내부 ID 참조 수정
- 관련 Editor 테스트 및 기존 테스트 픽스처의 ID 수정, 보스 소환 회귀 테스트 추가
- 현재 지침서의 최종 보스 ID 표기 수정

## 사용자가 직접 결정/승인한 영역

- 적 이름과 내부 ID 변경 목록
- 기존 리소스를 변경해 재사용하는 방향
- Rogue가 후열을 공격하지 않는 일반 근접형이라는 성격
- 근접 전투력 순서 `PatrolMan < Rogue < MaceWarrior < Knights`
- AI가 제안한 능력치, 보스 소환군, 1~10웨이브 편성안 전체 승인

## 중요 프롬프트/지시

- “내부 id까지 다 변경해줘.”
- “goblin_archer -> Archer 이렇게 바꿔주고 Rogue를 새로 추가해줘.”
- “Rogue는 후열 공격은 하지 않고 그냥 가장 약한 Patrolman 보다 강한 느낌이야.”
- “근접 공격 기준 Patrolman < Rogue < MaceWarrior < Knights”

## 테스트 및 검증 결과

- 변경 전 정적 RED 검사에서 신규 ID 부재를 확인
- Unity 원본 프로젝트 재컴파일 성공: 오류 0
- Knights 프레임 회귀 테스트 RED 확인: 수정 전 `Knights move frame 4` null 참조로 1개 실패
- Unity 6 임시 프로젝트 EditMode 최종 대상 테스트: 11개 통과, 실패 0, 건너뜀 0
- UTF-8 JSON 파싱, ID 중복/잔존 ID, 능력치 순서, 10개 웨이브 구성, 보스 소환 수, 프리팹 프로필 및 공격 효과 매핑 정적 검사 통과
- 이번 작업 범위 파일의 `git diff --check` 및 신규 파일 후행 공백 검사 통과
- 저장소 전체 검사는 별도 사용자 변경인 `Assets/01. Scenes/02. Game.unity`의 후행 공백을 보고했으며 해당 파일은 수정하지 않음

## 비고

- 기존 `wolf` 및 기타 미사용 적 데이터는 요청 범위 밖이므로 유지했다.
- 과거 설계 문서와 과거 AI 사용 기록은 이력 보존을 위해 이전 ID 표기를 변경하지 않았다.

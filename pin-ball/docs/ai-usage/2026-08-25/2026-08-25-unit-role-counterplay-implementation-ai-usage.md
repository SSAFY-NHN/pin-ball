# 유닛 역할 상성 강화 구현 AI 활용 기록

## 사용 도구/모델

- Codex (GPT-5 계열)
- 로컬 Git·코드·JSON·Unity scene/prefab 검색
- Unity 6.0.79f1 batchmode EditMode Test Runner

## 사용자 요청

- 기존 전사·궁수 역할과 가장 가까운 적 타겟 규칙을 유지
- 마법사 계열 기본 공격에 반경 1.5, 최대 2명, 60% 보조 피해 추가
- 창병 계열 기본 공격에 유닛 대상 방어력 40% 무시 추가
- 구매 카드 역할 문구와 10웨이브 역할 학습 조합 변경
- 기존 방어선, 진화 스킬, 공세 시간표, 구매·경제·풀링 보존

## AI 제안 내용

- `UnitBase` 기본 공격에 방어 관통 비율 훅 하나만 추가
- `AllyBasicAttackController`가 ID 기반 계열 판정, 마법사 보조 대상과 창병 관통을 담당
- 보조 피해는 기존 `UnitBase.TakeDamage()`를 사용
- 마법사 동시 3대상 VFX를 기존 프리팹의 `SetActive` 풀로 재생
- 기존 웨이브 데이터 계약과 60/90초 공세 로직은 유지하고 조합만 교체

## AI 실제 수정 영역

- 아군 기본 공격 계열 컨트롤러와 EditMode 테스트 추가
- `UnitBase` 유닛 기본 공격 관통 훅과 `AllyUnit` 계열 후속 처리 연결
- 공격 이펙트 프리팹별 최대 3개 풀과 빙결술사 기존 마법사 이펙트 매핑
- 네 구매 카드 역할 문구 변경
- 10웨이브 초기·기본·강화·최후 조합과 관련 데이터 테스트 변경
- 상세 구현 계획과 이 기록 작성

## 사용자 직접 결정/수정 필요 영역

- 실제 플레이에서 웨이브 수량·간격 난이도 조정
- 빙결술사가 기존 마법사 화염 투사체를 공유하는 임시 표현의 최종 승인
- WebGL 실기에서 동시 VFX와 전투 가독성 확인

## 중요한 프롬프트/지시

- 별도 속성 태그와 상성 추가 피해 표 금지
- 전사·궁수 특수 기본 공격 금지
- 진화 스킬과 방어선 공격 규칙 변경 금지
- 사용자 소유 애니메이션·VFX·ProjectSettings 변경 보존
- 승인 후 TDD로 최소 범위 구현

## 테스트/검증 결과

- TDD Red: `AllyBasicAttackControllerTests`가 타입 미존재 `CS0246`으로 실패함을 확인
- TDD Green: 핵심 `AllyBasicAttackControllerTests` 10/10 통과
- 관련 회귀: `UnitAttackEffectPlayerTests` 4/4, `BattleWaveScheduleDataTests` 1/1, `EnemyWaveVisualTests` 3/3 통과
- Unity C# 컴파일: Tundra build success, 신규 compiler error 0건
- JSON 정적 검사: 10웨이브, 보스 1회, 모든 단계 조합 8명 이하, 모든 적 ID 유효
- prefab/UI 정적 검사: `frost` VFX 매핑과 네 역할 문구 확인
- `git diff --check`: 오류 없음

## 제한점/직접 확인 필요

- 전체 EditMode 회귀 suite는 234개 중 229개 통과, 5개 실패했다. 실패는 이번 변경 파일 밖의 기존 적 성장 기대값 2건, 기존 방어선 테스트 setup 1건, 기존 scene 참조 1건, 기존 SFX 개수 1건이다.
- PlayMode와 WebGL 실기 검증은 수행하지 않았다.
- Unity 검증 중 자동 변경된 `ProjectSettings/ProjectSettings.asset`의 WebGL define 한 줄은 즉시 원복했다.
- 사용자 소유 `Rabbit1_Mage_Attack.anim`, `ArcaneVfxCatalog.asset` 변경은 수정하지 않았다.

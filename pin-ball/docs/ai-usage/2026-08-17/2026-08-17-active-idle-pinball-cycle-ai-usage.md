# 능동형 방치 핀볼 자동 순환 AI 활용 기록

## 사용한 AI 도구/모델

- Codex
- GPT-5 계열 모델

## 사용자 요청

- 능동형 방치 핀볼 디펜스 기획의 구현 1단계만 진행
- 상단 자동 공 생성, 하단 회수, 공별 재생성 대기, SetActive 풀링 유지
- 일반 범퍼 기본 골드 유지
- Goal에서 기존 아군 생성 제거
- 생산 업그레이드 UI, 공 구매, 황금 공, 잭팟, 자동전투 개편 등 후속 범위 제외
- 불필요한 검사와 검사를 위한 검사 생략

## AI 제안 내용

- `PinballManager`는 Unity 생명주기와 Inspector 참조를 조율
- `PinballBallPool`은 available/active 상태를 유지
- `PinballAutoCycleController`가 공별 재생성 만료 시각을 관리
- Goal 연출과 튜토리얼 알림은 유지하되 실제 아군 생성 보상 호출은 제거

## AI 실제 수정 영역

- 공별 자동 재생성 예약 Controller 추가
- 회수된 특정 공을 Pool에서 재활성화하는 상태 전이 추가
- 새 런 시작 시 공 한 개를 즉시 상단에서 활성화
- OutZone 또는 Goal 회수 후 3초 뒤 같은 공 재활성화
- `PinballRewardController.ApplyGoalReward`와 UnitManager 의존성 제거
- Game 씬에 `AutoBallSpawnPoint` 사전 배치 및 Inspector 연결
- 자동 순환 예약 핵심 테스트 추가

## 사용자 직접 결정/수정 필요 영역

- Goal의 아군 생성은 보존하지 않고 제거
- 현재 `temp` 브랜치에서 직접 작업
- 불필요한 반복 검사 생략
- 직접 플레이로 spawn 위치와 3초 재생성 체감 확인 필요

## 중요한 프롬프트/지시

- 기존 작업 트리를 임의로 되돌리지 않음
- 씬 사전 배치, Inspector 참조, SetActive 풀링 유지
- `[SerializeField]` underscore 금지
- 구현 1단계 밖의 개편 금지

## 테스트/검증 결과

- `git diff --check`: 공백 오류 없음
- Unity EditMode 기준선 및 대상 테스트 실행을 시도했으나 Unity Licensing Client IPC 재연결에서 정지하여 결과 XML이 생성되지 않음
- 동일 실패를 반복하지 않고 사용자 지시에 따라 추가 재시도 및 불필요한 전체 검사를 생략
- 정적 참조와 씬 직렬화 범위는 확인했으며 Unity 컴파일 및 직접 플레이 검증은 필요

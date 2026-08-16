# World Health Bar Implementation Plan

**Goal:** 아군과 적군 프리팹에 팀별 월드 체력바와 지연 피해 게이지를 적용한다.

## Task 1: 게이지 로직

- [ ] 실패하는 `WorldHealthBarControllerTests` 작성 및 확인
- [ ] `WorldHealthBarController` 최소 구현
- [ ] 집중 테스트 통과 확인

## Task 2: 프리팹 적용

- [ ] 아군 프리팹에 공통 배경과 아군 전용 지연/현재/프레임 연결
- [ ] 적군 프리팹에 공통 배경과 적군 전용 지연/현재/프레임 연결
- [ ] 프리팹 연결 테스트 및 전체 EditMode 테스트

## Constraints

- `UnitBase` 전투 로직과 공개 API는 변경하지 않는다.
- 체력바 오브젝트는 프리팹에 사전 배치하고 Inspector 참조를 사용한다.
- 풀 재활성화 뒤 첫 `LateUpdate`에서 게이지를 동기화한다.
- 관련 없는 기존 변경사항은 수정하거나 스테이징하지 않는다.

# 핀볼 황금 공·잭팟 구현 계획

## 목표

현재 자동 생산 흐름에 황금 공, 콤보 골드 배수와 활성 주기당 1회 잭팟을 최소 변경으로 연결한다.

## 작업

- [x] `Pinball`: 황금·잭팟 상태 소유, 활성화/회수 초기화, obstacle 인스턴스 전달
- [x] `PinballManager`: Inspector 설정, 영구 공 독립 추첨, 콤보 선등록, 잭팟 판정과 이벤트
- [x] `PinballComboController`: 단계형 배수 계산
- [x] `PinballRewardController`: 정상·레거시·잭팟 계산 분리와 단일 `AddGold`
- [x] `PinballObstacle`: `isJackpotBumper`와 잭팟 강조
- [x] `PinballArcaneVfx`, `PinballGoldPopup`: 황금 외형과 잭팟 전용 피드백
- [x] `PinballComboDisplay`: 실제 콤보 배수 표시
- [x] `02. Game.unity`: 기존 SpecialBumper 지정과 Manager 임시값 직렬화
- [x] 정적 확인과 AI 활용 기록 작성

## 검증 제한

테스트 파일을 작성·수정·삭제하거나 EditMode, PlayMode, 전체 테스트를 실행하지 않는다. 코드와 씬 직렬화를 읽어 상태 초기화, 계산 순서, 잭팟 세 조건, 단일 지급과 기존 흐름 보존만 확인한다.

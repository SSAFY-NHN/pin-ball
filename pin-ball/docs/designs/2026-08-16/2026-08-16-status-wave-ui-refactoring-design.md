# 상태·웨이브 UI 후속 리팩터링 설계

> 구현 상태: 완료  
> 설계 커밋: `d6343b5`  
> 구현 커밋: `5582928`

## 목표

`StatusPanel`과 `WavePanel`의 이벤트 연결 및 실제 UI 표시 책임은 유지하면서 피드백 애니메이션, 웨이브 HUD 표시, 버튼 상태 계산을 분리한다. 기존 `WaveHudState`는 변경하거나 다시 추상화하지 않는다.

## StatusFeedbackController

HP, 골드, 아군 수 Text 참조와 피드백 설정을 받아 DOTween 애니메이션을 담당한다.

- HP: 색상 플래시, 크기 펀치, 위치 흔들림
- 골드: 색상 플래시, 크기 펀치
- 아군 수: 위치 흔들림, 크기 펀치
- 종료: 실행 중 Tween을 중단하고 HP/골드의 기본 색상·크기를 복구

`StatusPanel`은 값 변경 여부만 판단하고 Controller에 강조를 요청한다.

## StatusWaveHudController

Wave Node/Connector 이미지와 상태 Sprite를 소유하고, 기존 `WaveHudState`를 사용해 표시 Sprite를 결정한다. 다음 두 경계를 분리한다.

- 초기화 검증: 노드 10개, 커넥터 9개, null 참조, 모든 상태 Sprite 확인
- 실제 표시: 검증 성공 후 현재 웨이브에 맞춰 Sprite 적용

`StatusPanel`은 BattleManager의 웨이브 이벤트를 전달할 뿐 HUD 배열이나 Sprite를 직접 다루지 않는다.

## 10웨이브 정책

현재 정책을 유지한다. `TitleData`가 BattleWaveCollection을 정확히 10웨이브로 검증하고, HUD도 5·9 엘리트와 10 보스 전용 에셋을 사용한다. 따라서 현재 최종 난이도 구조에서 10웨이브는 UI 임시 상수가 아니라 게임 데이터 계약이다.

향후 가변 웨이브 난이도를 도입할 때는 TitleData 계약과 특수 웨이브 메타데이터를 함께 변경한 뒤 HUD를 데이터 기반으로 재설계한다. 이번 범위에서는 `WaveHudState`와 10웨이브 계약을 유지한다.

## WaveButtonStateController

Battle·Pinball 상태 값을 받아 다음 결과를 하나의 `WaveButtonState`로 계산한다.

- 시작 버튼 표시 여부
- 시작 버튼 interactable
- 발사 버튼 interactable
- 현재 발사 비용
- 비용 지불 가능 여부

`WavePanel.IsLaunchAvailable()` 공개 정적 API는 유지하고 새 계산 객체의 동일 규칙에 위임한다. `WavePanel`은 결과를 Button과 TMP에 적용한다.

## Scene 참조 검증 경계

- `StatusWaveHudController`: 웨이브 HUD 전용 참조 검증
- `WavePanel.ValidateReferences()`: startButton, launchButton, launchCostText 확인
- 표시 메서드: 검증과 오류 메시지를 수행하지 않고 계산 결과 적용에만 집중

Scene 직렬화 필드와 기존 Inspector 참조는 변경하지 않는다.

## 보존 사항

- HP/골드/아군 수 문자열과 색상 규칙
- 기존 DOTween 시간·강도·색상
- 10개 Wave Node와 9개 Connector
- WaveHudState 단계 판정
- WavePanel 공개 `StartButton`, `RefreshTutorialState()`, `IsLaunchAvailable()`
- 시작 및 발사 버튼의 기존 활성 조건
- 발사 비용 텍스트와 색상

## 확인 범위

사용자 지시에 따라 테스트, 빌드, Unity 실행, Test Runner, 정적 분석은 수행하지 않는다. 이벤트 전달, 상태 계산식, Controller 위임, 기존 직렬화 필드 보존을 코드 읽기로만 확인한다.

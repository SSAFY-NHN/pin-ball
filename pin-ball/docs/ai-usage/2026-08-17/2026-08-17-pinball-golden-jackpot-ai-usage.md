# 핀볼 황금 공·잭팟 마일스톤 AI 활용 기록

## 사용한 AI 도구/모델

- Codex 기반 코드 분석 및 구현 도구
- 로컬 파일 검색, 패치 적용, Git diff 정적 확인

## 사용자 요청

기존 자동 핀볼 생산에 황금 공, 콤보 골드 배수, 지정 특수 범퍼 잭팟과 원인을 읽을 수 있는 피드백을 추가한다. 테스트는 작성하거나 실행하지 않고 코드와 씬을 정적으로 확인한다.

## AI 제안 내용

- 황금·잭팟 활성 주기 상태를 개별 `Pinball`이 소유
- 기존 `SpecialBumper`에 bool 플래그를 추가해 BigBumper 물리 재사용
- 현재 충돌의 콤보를 먼저 등록한 뒤 보상과 UI에 같은 값 사용
- 정상 보상, 기존 아이템 추가 보상과 잭팟 추가 보상을 분리 계산하고 총액을 한 번 지급
- 기존 Arcane VFX, 골드 팝업, glow와 Tween 재사용

## AI 실제 수정 영역

- `Pinball`, `PinballManager`, `PinballComboController`, `PinballRewardController`, `PinballObstacle`
- `PinballArcaneVfx`, `PinballGoldPopup`, `PinballComboDisplay`
- Game 씬의 `SpecialBumper` 지정과 Manager 임시 밸런스 값
- 기술 설계와 구현 계획 문서

## 사용자 직접 결정/수정 필요 영역

- 황금 확률 5%, 황금 배수 3배, 콤보 단계와 잭팟 보상 수치의 실제 플레이 밸런스
- 전용 잭팟 SFX 에셋 선정과 연결
- 플레이 모드에서 여러 공이 겹칠 때 황금 공과 잭팟 원인의 가독성 확인

## 중요한 프롬프트/지시

- 물리, 생성 위치, 재생성 시간, 생산·전투 업그레이드를 변경하지 않는다.
- 복제 공은 황금 상태를 상속하거나 추첨하지 않는다.
- 한 황금 공 활성 주기당 잭팟은 1회다.
- 신규 외부 에셋·패키지와 런타임 UI 생성을 사용하지 않는다.
- 테스트 파일 작성·수정·삭제와 테스트 실행을 금지한다.

## 테스트/검증 결과

- 테스트는 사용자 지시에 따라 작성·수정·실행하지 않았다.
- 코드와 Game 씬 직렬화의 상태 초기화, 계산 순서, 잭팟 조건, 풀 분리와 Inspector 연결을 정적으로 확인했다.
- `dotnet build Assembly-CSharp.csproj --no-restore`를 한 번 실행했으나, 생성된 csproj에 기존 전투 업그레이드 타입이 포함되지 않은 상태라 `EBattleUpgrade`, `BattleUpgradeSettings`, `BattleUpgradeController` 참조 오류 11개로 종료됐다. 이번 변경 파일을 가리키는 컴파일 오류는 출력되지 않았다.
- Unity 플레이 검증은 실행하지 않았다.

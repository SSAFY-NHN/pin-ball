# 시간표 증원·출격 쿨다운·창병 구현 AI 활용 기록

## 사용 도구/모델

- Codex (GPT-5 계열)
- 로컬 파일 검색, Git diff/status 검사
- Unity 6.0.0.79f1 batchmode EditMode Test Runner

## 사용자 요청

- 초기 시간표 이후 방어선 파괴까지 계속되는 적 증원
- 60초 강화 증원, 90초 최후 공세와 생존 적 8명 상한
- 전사 4초, 궁수 5초, 마법사 7초, 창병 5초 독립 출격 쿨다운
- 기존 `spearman`을 35골드·1.4배 비용 증가의 네 번째 구매 유닛으로 연결
- 전투 중 공세 카운트다운 HUD 표시
- 기존 방어선 결과, 핀볼 골드·콤보·잭팟, 전술 증원권과 아군 5명 상한 보존

## AI 제안 내용

- 순수 C# `BattleAssaultController`가 공세 시간·예약·단계·반복 시점을 소유
- `BattleManager`가 Unity `Time.deltaTime`과 기존 `UnitManager` 풀링 생성 경로를 연결
- `UnitPurchaseController`가 4종 구매 횟수와 독립 쿨다운을 함께 소유
- 기존 10웨이브 조합은 초기 공세로 보존하고 현재 적 데이터를 증원 단계에 순차 배치
- 씬 배치 4카드, 초상화, 쿨다운 마스크와 상태 HUD 카운트다운 사용

## AI 실제 수정 영역

- 웨이브 시간표 및 반복 증원 데이터 타입과 로딩 검증
- 공세 스케줄러, `BattleManager` 공세 수명주기, `UnitManager` 예약 생성 진입점
- 4종 구매 설정과 성공 후 쿨다운 시작·감소·조회
- 창병 구매 버튼, 네 카드 초상화·쿨다운 표시
- 60초·90초 카운트다운과 단계 강조·90초 경고음
- `BattleWaveData.json` 10웨이브 초기·기본·강화·최후 조합
- 공세, 구매, 데이터, formatter와 scene reference EditMode 테스트
- `02. Game.unity` Inspector 참조와 씬 배치 UI

## 사용자 직접 결정 영역

- 구현 계획과 웨이브별 밸런스 초깃값 승인
- 현재 checkout에서 직접 구현하도록 결정
- 사용자 소유 asset 변경은 작업에서 제외

## 중요 지시

- 적 전멸만으로 웨이브를 끝내지 않음
- 출현 상한으로 누락된 적을 대기열에 누적하지 않음
- 실패한 구매에서 골드, 구매 횟수, 증원권과 쿨다운 불변
- 전술 증원권도 쿨다운을 우회하지 않음
- Unity 게임 시간, 씬 배치·Inspector 참조, `SetActive` 풀링 우선
- 전선 위험 UI, 외부 패키지, 런타임 UI 자동 생성과 관련 없는 리팩터링 제외

## 테스트/검증 결과

- focused 공세·데이터·구매·formatter 테스트: 32/32 통과
- 관련 공세·구매·씬·웨이브·방어선 scene·핀볼 보상 회귀군: 69/69 통과
- 구매 UI scene test에서 4개 카드, 초상화, 마스크, 쿨다운 텍스트와 4종 Inspector 설정 참조 통과
- 상태 HUD scene test의 새 `assaultCountdownText` 참조 assertion 통과
- Unity C# compile: 오류 0건, 기존 `PinballManager` 미사용 경고 3건
- 전체 EditMode: 223개 중 218개 통과, 기존 불일치 5개 실패
  - 적 웨이브 성장 반올림 기대값 2건
  - null `BattleAreaBounds`를 넘기는 방어선 test 1건
  - 기존 `PlungerLever/LaunchCost` 계층과 충돌하는 UI test 1건
  - 등록 SFX 수 기대값 불일치 1건
- `git diff --check`: Unity가 새 UI object를 직렬화한 빈 YAML 값의 trailing whitespace를 보고함. C#·JSON 오류는 없음.

## 제한점/직접 확인 필요

- batchmode EditMode와 scene reference 검증까지 수행했다. 실제 PlayMode 60초·90초 체감, 일시정지, 2배속과 WebGL 프레임은 직접 확인이 필요하다.
- 웨이브 9~10의 정예 반복 조합은 적 웨이브 배율과 함께 플레이 테스트 후 반복 간격 조정이 필요할 수 있다.
- 전체 EditMode 기존 실패 5개는 요청 범위 밖 파일과 기대값 문제라 수정하지 않았다.
- 사용자 소유 `Rabbit1_Mage_Attack.anim`, `ArcaneVfxCatalog.asset` 변경은 수정하지 않았다.

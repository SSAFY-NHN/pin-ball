# 전투 배속·핀볼 콤보 UI 설계

## 배경

현재 `BattleManager`는 `EWaveState`로 준비, 전투, 결과 처리와 최종 결과를
구분하고, `WavePanel`은 전투 시작 버튼과 핀볼 발사 버튼을 관리한다.
`PinballManager`는 핀볼 충돌 결과와 범퍼 보상을 총괄하며
`Pinball.OnCollisionEnter2D`에서 전달받은 `EPinballObstacle` 종류를 이미 구분한다.

이번 작업은 기존 상태와 충돌 흐름을 재사용해 다음 세 기능을 추가한다.

- 사용자가 선택한 `1×/2×` 전투 배속
- 전투 시작 버튼의 맵 상단 중앙 배치
- 큰 범퍼 연속 충돌 콤보와 남은 2초의 시각적 표시

발사된 핀볼이 발사 레인으로 되돌아와 낙하는 문제는 사용자가 직접 수정하므로
이번 작업 범위에 포함하지 않는다.

## 고정 제약

- Unity 6, C#, PC WebGL 대상과 현재 프로젝트 구조를 유지한다.
- `App`, `AppService`, `BattleManager`, `PinballManager`, `WavePanel`의 기존 공개
  API와 책임을 유지한다.
- 핵심 컴포넌트와 UI는 Game 씬에 미리 배치하고 Inspector 참조를 사용한다.
- 런타임에서 콤보 TMP 텍스트, 배속 버튼 또는 보조 UI를 생성하지 않는다.
- `[SerializeField]` 이름에는 underscore를 사용하지 않는다.
- 누락된 Inspector 참조를 Find 계열 API나 런타임 생성으로 대체하지 않는다.
- 기존 DOTween 패키지를 재사용하고 새 외부 패키지는 설치하지 않는다.
- 핀볼 물리 수치, 발사 방향, 범퍼 보상과 기존 충돌 효과는 변경하지 않는다.

## 사용자 확정 사항

- 배속 선택은 준비와 전투 상태가 바뀌어도 UI에 유지한다.
- 실제 `Time.timeScale = 2` 적용은 `EWaveState.Active` 전투 중에만 허용한다.
- 준비, 결과 처리, 승리와 패배 상태의 실제 배속은 항상 `1`이다.
- 전투 시작 버튼은 전투 맵 상단 중앙으로 이동한다.
- 콤보는 `EPinballObstacle.BigBumper` 충돌만 집계한다.
- 마지막 큰 범퍼 충돌 후 실제 시간 2초가 지나면 콤보를 0으로 초기화한다.
- 콤보 텍스트는 배경 텍스트와 화려한 전경 텍스트를 겹쳐 표현한다.
- 전경 텍스트의 보이는 영역은 2초 동안 오른쪽에서 왼쪽으로 줄어든다.
- 콤보가 이어질 때마다 텍스트에 DOTween 스케일 애니메이션을 재생한다.
- 콤보 관련 TMP 텍스트와 마스크는 Game 씬에 사전 배치한다.

## 선택한 접근

기존 Manager에 UI 책임을 넣지 않고 다음 집중형 컴포넌트를 사용한다.

```text
BattleManager
└─ OnStateChanged
   └─ GameSpeedController

PinballManager
├─ PinballComboController
├─ OnComboChanged
└─ PinballComboDisplay
```

`WavePanel`에 배속과 콤보를 모두 넣는 방식은 전투 시작·발사 버튼 책임과
시간·핀볼 피드백 책임이 섞이므로 채택하지 않는다. 새 통합 Manager나 이벤트
버스를 만드는 방식은 기능 규모에 비해 구조가 커지므로 채택하지 않는다.

## 전투 배속

### GameSpeedController

Game 씬에 배치되는 UI Controller다. 다음 상태를 구분해 소유한다.

- 선택 배속: 사용자가 버튼으로 선택한 `1×` 또는 `2×`
- 실제 배속: 현재 `BattleManager.State`에 따라 `Time.timeScale`에 적용되는 값

배속 버튼의 TMP 텍스트는 선택 배속을 표시한다. 준비 중 `2×`를 선택해도 UI는
`2×`를 유지하지만 실제 `Time.timeScale`은 `1`이다. `Active` 진입 시 선택 배속을
실제 배속으로 적용한다. `Active` 중 버튼을 누르면 선택값과 실제 배속을 함께
변경한다.

`Active`에서 벗어나 `Resolving`, `Pending`, `Victory`, `Defeat` 중 하나가 되면
실제 배속을 즉시 `1`로 되돌린다. 선택 배속은 바꾸지 않으므로 다음 전투에서도
버튼은 이전 선택을 유지한다. Controller가 파괴될 때도 다른 씬에 영향을 주지
않도록 `Time.timeScale`을 `1`로 복구한다.

`Time.fixedDeltaTime`은 변경하지 않는다. Unity의 고정 게임 시간 간격을 유지해
2배속 전투에서도 현재 물리 계산 정밀도를 보존한다.

### UI 배치

기존 전투 시작 버튼의 클릭 연결과 `WavePanel.startButton` 참조는 유지한다.
버튼 RectTransform만 전투 맵 상단 중앙 기준으로 이동한다. 배속 버튼은 전투
시작 버튼 가까이에 배치하되 별도 `GameSpeedController`가 클릭과 텍스트를
관리한다.

## 핀볼 콤보

### PinballComboController

`PinballManager` 아래에서 콤보 수와 만료 시각만 관리하는 일반 C# Controller다.

- 큰 범퍼 충돌 시 현재 콤보를 1 증가시킨다.
- 마지막 충돌 시각을 `Time.unscaledTime` 기준으로 저장한다.
- 각 프레임 현재 비배속 시각과 비교해 2초 경과 여부를 판정한다.
- 만료되면 콤보를 0으로 바꾼다.
- 새 런 초기화 시 콤보를 즉시 0으로 초기화한다.

실제 시간 기준을 사용하므로 배속 상태와 관계없이 시각적 제한 시간은 정확히
2초다. 핀볼은 준비 상태에서만 발사할 수 있어 실제 `timeScale`도 1이지만, UI
시간의 의도를 코드에 명확히 보존한다.

`PinballManager.OnBallHit`은 기존 사운드, 타격 횟수와 보상 처리를 유지한다.
`SmallPin` 분기는 콤보를 변경하지 않는다. `BigBumper` 처리 시에만 Controller를
갱신하고 `OnComboChanged`를 발행한다. 분열된 여러 활성 핀볼의 충돌은 하나의
Manager가 받으므로 공용 콤보에 합산한다.

### PinballComboDisplay

Game 씬의 사전 배치 참조만 사용하는 UI 컴포넌트다. 권장 계층은 다음과 같다.

```text
ComboDisplay
└─ ComboTextGroup
   ├─ ComboBackgroundText
   └─ ComboFillMask (RectMask2D)
      └─ ComboForegroundText
```

- `ComboDisplay`에는 구독을 유지하는 Controller 컴포넌트를 둔다.
- `ComboTextGroup`은 표시·숨김과 DOTween 스케일 연출 대상이다.
- 두 TMP 텍스트는 항상 동일한 `N COMBO` 문자열을 사용한다.
- 배경 텍스트는 어둡고 낮은 채도의 전체 글자 형태를 유지한다.
- 전경 텍스트는 기존 아케인 UI와 어울리는 밝은 청록·금색 계열을 사용한다.
- `ComboFillMask`의 왼쪽 경계는 고정하고 오른쪽 경계를 왼쪽으로 이동시켜
  전경 텍스트의 보이는 폭을 100%에서 0%로 줄인다.
- 콤보가 0이면 `ComboTextGroup`만 숨기고 구독 컴포넌트는 활성 상태를 유지한다.

큰 범퍼 충돌이 발생하면 두 텍스트의 숫자를 갱신하고 마스크 폭을 즉시 100%로
복구한다. 이후 `Time.unscaledDeltaTime` 기준 2초 동안 폭을 선형으로 0%까지
줄인다. 2초 안에 다시 충돌하면 현재 감소 진행을 취소하고 100%에서 새로
시작한다.

### DOTween 스케일 연출

콤보가 증가할 때마다 `ComboTextGroup`의 기존 스케일 Tween을 종료하고 기준
스케일을 복구한 뒤 `DOPunchScale`을 재생한다. 배경과 전경이 같은 RectTransform
아래에 있으므로 두 텍스트와 마스크가 어긋나지 않고 함께 튄다.

기본 연출값은 다음과 같이 시작한다.

- 기준 스케일: `(1, 1, 1)`
- punch 크기: `(0.18, 0.18, 0)`
- 지속 시간: `0.22초`
- vibrato: `5`
- elasticity: `0.5`
- 배속 영향: `SetUpdate(true)`로 비배속 시간 사용

연속 충돌 때 Tween이 누적되어 크기가 틀어지지 않도록 새 연출 전에 Tween을
종료하고 기준 스케일을 명시적으로 복구한다. 컴포넌트 비활성화 또는 파괴 시
Tween을 정리한다.

## 데이터 흐름

### 배속

1. 사용자가 배속 버튼을 누른다.
2. `GameSpeedController`가 선택 배속을 `1×`와 `2×` 사이에서 전환한다.
3. 버튼 텍스트는 항상 선택 배속으로 갱신된다.
4. 현재 상태가 `Active`이면 실제 배속도 즉시 적용한다.
5. 현재 상태가 `Active`가 아니면 실제 배속은 `1`을 유지한다.
6. `BattleManager.OnStateChanged`가 발생할 때 같은 규칙으로 실제 배속을 다시 계산한다.

### 콤보

1. `Pinball.OnCollisionEnter2D`가 기존 경로로 장애물 충돌을 알린다.
2. `PinballManager.OnBallHit`이 `BigBumper`인지 확인한다.
3. 기존 범퍼 보상과 피드백을 처리하면서 공용 콤보를 증가시킨다.
4. `OnComboChanged`가 새 수치와 100% 남은 시간을 UI에 전달한다.
5. `PinballComboDisplay`가 두 텍스트, 마스크와 DOTween 스케일 연출을 갱신한다.
6. Manager와 Display가 같은 비배속 시간을 사용해 2초 진행을 계산한다.
7. 2초 안에 다음 충돌이 없으면 Manager가 콤보를 0으로 만들고 UI를 숨긴다.

## 오류 처리와 수명주기

- 배속 버튼, 배속 TMP 텍스트 또는 BattleManager가 없으면 명확한 오류를 남기고
  배속 입력을 연결하지 않는다.
- 콤보 배경 TMP, 전경 TMP, 마스크 RectTransform 또는 텍스트 그룹이 없으면
  명확한 오류를 남기고 표시 갱신을 중단한다.
- 누락 참조를 런타임 생성이나 Find 호출로 자동 보완하지 않는다.
- `OnDestroy`에서 BattleManager와 PinballManager 이벤트를 해제한다.
- 배속 Controller 파괴 시 `Time.timeScale = 1`을 보장한다.
- 콤보 Display 비활성화·파괴 시 DOTween을 종료하고 기준 스케일을 복구한다.

## 파일 범위

### 신규 파일

- `Assets/02. Scripts/03. UI/GameSpeedController.cs`
- `Assets/02. Scripts/Pinball/PinballComboController.cs`
- `Assets/02. Scripts/03. UI/PinballComboDisplay.cs`
- 필요한 Editor 테스트와 각 Unity `.meta`

### 수정 파일

- `Assets/02. Scripts/Pinball/PinballManager.cs`
- `Assets/01. Scenes/02. Game.unity`
- 작업 종료 시 해당 날짜의 AI 활용 기록

### 의도적으로 수정하지 않는 파일

- `BattleManager`, `Pinball`, `PinballObstacle`의 기존 동작
- 핀볼 발사기, 레일, OutZone과 물리 Material
- Prefab, JSON 데이터와 외부 패키지
- 핀볼 발사 후 되돌아오는 낙하 문제

## 검증

### EditMode

- 선택 배속 `2×`가 준비 상태에서 실제 배속 `1`로 해석되는지 확인한다.
- 선택 배속 `2×`가 `Active`에서만 실제 배속 `2`로 해석되는지 확인한다.
- `Active` 종료 후 선택값은 유지되고 실제 배속만 `1`이 되는지 확인한다.
- 큰 범퍼 연속 입력이 콤보를 증가시키고 만료 시각을 다시 2초로 미루는지 확인한다.
- 정확히 2초 전에는 유지되고 2초 이상이면 0으로 초기화되는지 확인한다.
- 마스크 진행률이 남은 시간 `2초 -> 0초`를 `1 -> 0`으로 변환하는지 확인한다.
- Game 씬에 두 TMP 텍스트, RectMask2D, 배속 버튼과 Inspector 참조가 사전
  배치됐는지 확인한다.

### 직접 플레이

1. 준비 중 `2×`를 선택해도 핀볼 속도가 빨라지지 않는지 확인한다.
2. UI가 `2×`를 유지한 채 전투 진입 시 유닛과 전투 시간이 2배로 진행되는지 확인한다.
3. 결과 처리와 다음 준비 진입 즉시 실제 속도가 1배로 복구되는지 확인한다.
4. 큰 범퍼만 콤보를 올리고 작은 핀·벽·레일은 올리지 않는지 확인한다.
5. 전경 텍스트가 2초 동안 오른쪽부터 줄어들며 배경 텍스트가 드러나는지 확인한다.
6. 연속 범퍼 충돌마다 마스크가 복구되고 스케일 punch가 새로 재생되는지 확인한다.
7. 2초 만료 후 콤보 표시가 숨겨지는지 확인한다.
8. 전투 시작 버튼이 맵 상단 중앙에 있고 기존 시작 조건과 튜토리얼 연결이 유지되는지 확인한다.

## 완료 기준

- 사용자 선택 배속은 UI에 유지되지만 `Active` 이외 상태의 실제 배속은 항상 1이다.
- 준비 상태의 핀볼 물리는 배속 선택의 영향을 받지 않는다.
- 전투 시작 버튼이 맵 상단 중앙에 배치된다.
- 큰 범퍼 충돌만 공용 콤보를 증가시킨다.
- 콤보 제한 시간은 실제 2초이며 연속 충돌 시 처음부터 다시 시작한다.
- 씬 배치된 배경·전경 TMP와 RectMask2D가 남은 시간을 시각화한다.
- 연속 콤보마다 DOTween 스케일 punch가 누적 오차 없이 재생된다.
- 런타임 UI 생성, 새 패키지, 물리와 발사 레인 변경이 없다.

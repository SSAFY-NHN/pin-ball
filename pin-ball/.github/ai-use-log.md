# AI 활용 기록

## 2026-08-10 아군 공격력·체력 1.50배 상향

- 사용한 AI 도구/모델: Codex, GPT-5 계열 모델
- 사용자 요청: 아군 캐릭터의 기본 공격력과 체력을 원본 대비 1.50배로 상향
- AI 제안 내용: 아군 스탯 생성 단계의 공통 배율을 1.50으로 변경해 기본값과 레벨 성장분에 동일하게 적용
- AI 실제 수정 영역: `AllyUnitData` 전투 스탯 배율과 관련 특성·생성 서비스 테스트 기대값
- 사용자 직접 결정/수정 필요 영역: 실제 전투 플레이를 통한 적 난이도와 웨이브 밸런스 체감 확인
- 중요한 프롬프트/지침: 공격력·체력만 상향하고 방어력·공격 속도·사거리 및 장비 보정 순서는 유지
- 테스트/검증 결과: Unity 재컴파일 오류 0건. 에디터 직접 계산에서 3레벨 기본 직업은 HP 180·공격력 36, 진화 직업 시작 수치는 HP 150·공격력 30으로 1.50배 적용됨을 확인했고 `git diff --check`를 통과했다.

## 2026-08-10 아군 소환 겹침 허용 fallback

- 사용한 AI 도구/모델: Codex, GPT-5 계열 모델
- 사용자 요청: 다른 아군을 피할 자리가 없어도 유닛이 소환되며 맵 밖으로 나가지 않도록 수정
- AI 제안 내용: 기존 빈 격자 우선 배치를 유지하고, 모든 유효 격자가 점유됐을 때만 아군 배치 영역 내부의 첫 격자를 겹침 허용 fallback으로 사용
- AI 실제 수정 영역: `UnitPlacementService`의 점유 시 fallback 배치와 모든 격자 점유 회귀 테스트
- 사용자 직접 결정/수정 필요 영역: 실제 플레이에서 여러 유닛이 한 위치에 겹쳤을 때 시각적 가독성 확인
- 중요한 프롬프트/지침: 소환 실패를 방지하되 `BattleAreaBounds`가 제공한 영역 내부 좌표만 사용하고 기존 드래그·합성·전투 동작은 유지
- 테스트/검증 결과: Unity 재컴파일 오류 0건. Unity 테스트 러너 호출은 도구 응답 제한으로 결과를 받지 못했으나, 에디터에서 28개 격자를 모두 점유한 시나리오를 직접 실행해 소환 성공, 첫 격자 fallback 사용, 배치 영역 내부 유지를 확인했다. `git diff --check`도 통과했다.

## 2026-08-10 게임 소개 플레이 영상 스크립트 작성

- 사용한 AI 도구/모델: Codex, GPT-5 계열 모델
- 사용자 요청: 현재 게임을 분석해 게임 소개 플레이 영상에 사용할 스크립트를 작성하고 저장
- AI 제안 내용: 실제 구현된 핀볼 소환, 배치·합성·진화, 아이템, 최대 5인 자동 전투, 영구 사망, 10웨이브 보스전을 약 90초의 소개 영상 흐름으로 구성
- AI 실제 수정 영역: 타임코드별 화면 지시·내레이션·자막·편집 포인트, 연속 녹음용 원고, 필수 촬영 컷 체크리스트 문서 작성
- 사용자 직접 결정/수정 필요 영역: 최종 게임명과 로고 삽입, 성우 속도에 따른 구간별 ±1초 편집, 실제 촬영 클립 선정
- 중요한 프롬프트/지침: 코드와 데이터 및 현재 Game View에서 확인한 실제 구현 기능만 소개하고 미구현 기능은 홍보 문구에서 제외
- 테스트/검증 결과: 게임 코드, 전투·핀볼 데이터, 현재 Game View를 대조해 기능 표현을 확인했고 문서 타임라인 합계가 90초임을 확인했다.

## 2026-08-10 핀볼 레버 당김 SFX 연결

- 사용한 AI 도구/모델: Codex, GPT-5 계열 모델
- 사용자 요청: 새로 추가한 `spring_sound2`를 플레이어가 레버를 당길 때 재생
- AI 제안 내용: 공이 장전된 레버를 실제로 움직이기 시작할 때 드래그당 한 번 `spring_sound2`를 재생하고 기존 `spring_sound` 발사음은 유지
- AI 실제 수정 영역: SoundManager 키와 Developer 씬 SFX 등록, `PinballLauncherController`의 당김 시작 감지, 오디오 설정 테스트
- 사용자 직접 결정/수정 필요 영역: 실제 플레이 청감에 따른 당김음 볼륨 미세 조정 가능
- 중요한 프롬프트/지침: 클릭만 하고 당기지 않을 때는 재생하지 않으며 기존 BGM·SFX 및 사용자 변경 보존
- 테스트/검증 결과: Unity 재컴파일이 오류 없이 완료됐고 레버 당김 경계 테스트 3/3과 전체 EditMode 테스트 167/167이 통과했다. Developer 씬에서 `spring_sound2` 오디오 참조가 유효함을 확인했으며 `git diff --check`도 통과했다.

## 2026-08-10 BGM 및 게임 이벤트 SFX 연결

- 사용한 AI 도구/모델: Codex, GPT-5 계열 모델
- 사용자 요청: `06. Sounds/BGM`의 음악을 게임 시작부터 반복 재생하고 `SFX` 폴더의 파일을 제목에 맞는 게임 이벤트에 배치
- AI 제안 내용: 기존 `SoundManager`와 오디오 소스 풀을 유지하면서 `main2`를 시작 BGM으로 등록하고, 핀볼·전투·회복·유닛 생성·진화·구매·웨이브 이벤트를 16개 SFX에 연결
- AI 실제 수정 영역: Developer 씬의 SoundManager 클립 배열 및 BGM 반복 설정, `SoundManager`의 시작 BGM과 사운드 키, `SceneManager`의 공통 BGM 키, 전투·핀볼·아이템·웨이브 이벤트 호출, 오디오 설정 EditMode 테스트
- 사용자 직접 결정/수정 필요 영역: 실제 플레이 청감에 따른 BGM·SFX 볼륨과 동시 재생 밀도 미세 조정 가능
- 중요한 프롬프트/지침: 사용자 승인 매핑만 구현, BGM/SFX 폴더 밖 notification 파일 제외, 기존 Animals·Rabbit1·ArcaneVFX 변경 보존
- 테스트/검증 결과: Unity 리컴파일이 오류 없이 완료됐고 `SoundManagerTests` 9/9와 전체 EditMode 테스트 164/164가 통과했다. Play Mode에서 `main2`가 loop 상태로 실제 재생되고 `spring_sound`가 단발 SFX로 재생되는 것도 확인했으며 `git diff --check`를 통과했다.

## 2026-08-10 Rabbit 진화 캐릭터 이미지 연결

- 사용한 AI 도구/모델: Codex, GPT-5 계열 모델
- 사용자 요청: Dog 완료 후 허락에 따라 마지막 Animals 캐릭터 Rabbit 연결 진행
- AI 제안 내용: 기본 `mage`의 자동 진화 결과인 `pyromancer`가 Rabbit2 healer 외형을 사용하도록 애니메이션 프로필 추가
- AI 실제 수정 영역: `AllyUnit.prefab`의 Rabbit2 idle·walk·attack 프레임 연결, `BattleUnitVisualTests`의 pyromancer 프로필 및 Rabbit2 애니메이션 검증 케이스
- 사용자 직접 결정/수정 필요 영역: 없음
- 중요한 프롬프트/지침: Rabbit만 처리하고 기존 Bear·Cat·Dog·Rabbit1·ArcaneVFX 변경 보존
- 테스트/검증 결과: 데이터의 `mage` 진화 후보를 정렬하면 `frost`, `pyromancer` 순서이며 자동 진화가 두 번째 후보인 `pyromancer`를 선택함을 확인했다. Unity 리컴파일은 오류 없이 완료됐고 Rabbit1·Rabbit2와 기존 Bear·Cat·Dog 검증을 포함한 `BattleUnitVisualTests` 21/21이 통과했으며 `git diff --check`도 통과했다.

## 2026-08-10 Dog 진화 캐릭터 이미지 연결

- 사용한 AI 도구/모델: Codex, GPT-5 계열 모델
- 사용자 요청: Cat 완료 후 허락에 따라 다음 Animals 캐릭터 연결 진행
- AI 제안 내용: 기본 `warrior`의 자동 진화 결과인 `knight`가 Dog2 warrior 외형을 사용하도록 애니메이션 프로필 추가
- AI 실제 수정 영역: `AllyUnit.prefab`의 Dog2 idle·walk·attack 프레임 연결, `BattleUnitVisualTests`의 knight 프로필 및 Dog2 스프라이트 시트 검증 케이스
- 사용자 직접 결정/수정 필요 영역: 마지막 Animals 캐릭터 Rabbit 연결 진행 여부
- 중요한 프롬프트/지침: Dog만 처리하고 완료 후 사용자 허락 전에는 Rabbit으로 넘어가지 않음, 기존 Bear·Cat·Rabbit·ArcaneVFX 변경 보존
- 테스트/검증 결과: 데이터의 `warrior` 진화 후보를 정렬하면 `berserker`, `knight` 순서이며 자동 진화가 두 번째 후보인 `knight`를 선택함을 확인했다. Unity 리컴파일은 오류 없이 완료됐고 Dog1·Dog2와 기존 Bear·Cat·Rabbit 검증을 포함한 `BattleUnitVisualTests` 18/18이 통과했으며 `git diff --check`도 통과했다.

## 2026-08-10 Cat 진화 캐릭터 이미지 연결

- 사용한 AI 도구/모델: Codex, GPT-5 계열 모델
- 사용자 요청: Bear 완료 후 허락에 따라 다음 Animals 캐릭터 연결 진행
- AI 제안 내용: 기본 `archer`의 자동 진화 결과인 `ranger`가 Cat2 gunslinger 외형을 사용하도록 애니메이션 프로필 추가
- AI 실제 수정 영역: `AllyUnit.prefab`의 Cat2 idle·walk·attack 프레임 연결, `BattleUnitVisualTests`의 ranger 프로필 및 Cat2 애니메이션 검증 케이스
- 사용자 직접 결정/수정 필요 영역: 다음 Animals 캐릭터 연결 진행 여부
- 중요한 프롬프트/지침: Cat만 처리하고 완료 후 사용자 허락 전에는 다음 캐릭터로 넘어가지 않음, 기존 Bear·Rabbit·ArcaneVFX 변경 보존
- 테스트/검증 결과: Cat의 자동 진화 결과가 `ranger`임을 데이터 기준으로 확인했고 Unity 리컴파일도 오류 없이 완료됐다. Cat1·Cat2와 기존 Bear·Rabbit 검증을 포함한 `BattleUnitVisualTests` 15/15가 통과했으며 `git diff --check`도 통과했다.

## 2026-08-10 Bear 진화 캐릭터 이미지 연결

- 사용한 AI 도구/모델: Codex, GPT-5 계열 모델
- 사용자 요청: Animals 캐릭터의 진화 이미지를 Bear부터 연결하고, 완료 후 허락을 받은 다음 캐릭터로 진행
- AI 제안 내용: 직업 데이터의 진화 관계에 맞춰 기본 `spearman`은 Bear1, 자동 진화 결과 `lancer`는 Bear2 외형을 사용하도록 애니메이션 프로필 분리
- AI 실제 수정 영역: Game 씬 Bear 슬롯의 기본 직업 수정, `AllyUnit.prefab`의 Bear 기본/진화 idle·walk·attack 프레임 연결, `BattleUnitVisualTests`의 프로필 및 Bear2 애니메이션 검증 케이스
- 사용자 직접 결정/수정 필요 영역: 다음 Animals 캐릭터 연결 진행 여부
- 중요한 프롬프트/지침: Bear만 우선 처리, 완료 후 사용자 허락 전에는 다음 캐릭터로 넘어가지 않음, 기존 Rabbit 변경 보존
- 테스트/검증 결과: Unity 강제 리컴파일이 오류 없이 완료됐고 Bear 기본형·진화형 프로필과 Bear1·Bear2 애니메이션을 포함한 `BattleUnitVisualTests` 12/12가 통과했다. Game 씬 Bear 슬롯이 `spearman`으로 시작하고 자동 진화 결과가 `lancer`임을 데이터 기준으로 확인했으며 `git diff --check`도 통과했다. 별도 `dotnet build`는 기존 DOTween 프로젝트 참조 오류로 실패했지만 Unity 컴파일과 테스트에는 영향이 없었다.

## 2026-08-10 전투 상태 기반 카메라 슬라이드

- 사용한 AI 도구/모델: Codex, GPT-5 계열 모델
- 사용자 요청: 준비 상태에는 맵 옆 핀볼이 보이도록 카메라를 이동하고, 전투 시작 시 기존 전투 화면으로 부드럽게 복귀
- AI 제안 내용: `BattleManager` 상태 이벤트를 구독하는 씬 배치형 `BattleCameraController`와 0.5초 cubic ease-out 슬라이드
- AI 실제 수정 영역: `BattleCameraController`, EditMode 테스트, Game 씬 Main Camera 연결, 설계·구현 계획 및 AI 활용 기록
- 사용자 직접 결정/수정 필요 영역: 사용자가 전용 컴포넌트 방식과 부드러운 슬라이드를 결정했으며, 최종 핀볼 구도와 이동 시간은 Inspector에서 조정 가능
- 중요한 프롬프트/지시: 기존 구조 보존, Inspector 참조 우선, `[SerializeField]` underscore 금지, 최소 변경, 외부 패키지 금지
- 테스트/검증 결과: 구현 전 `BattleCameraController` 타입 부재로 `CS0246` 실패를 확인하고, 구현 후 `dotnet build Assembly-CSharp-Editor.csproj --no-restore`가 오류 0개로 성공했다. Unity 배치 EditMode 테스트는 라이선스 클라이언트 연결 실패로 결과가 생성되지 않았으며 Play Mode와 WebGL 실행 검증은 수행하지 못했다.

## 2026-08-10 핀볼 배치 복구

- 사용한 AI 도구/모델: Codex, GPT-5 계열 모델
- 사용자 요청: `feat: refine evolution ui and mask glow` 커밋에서 사라진 기존 핀볼 배치 복구
- AI 제안 내용: 커밋 전체를 되돌리지 않고 `3cd17e2^`의 핀볼 Transform 배치만 현재 Game 씬에 병합
- AI 실제 수정 영역: `Assets/01. Scenes/02. Game.unity`의 핀볼 오브젝트 위치·회전·스케일
- 사용자 직접 결정/수정 필요 영역: 사용자가 기존 Evolution UI와 카메라 변경을 유지하는 선택적 복구를 승인
- 중요한 프롬프트/지시: 기존 변경 보존, 관련 없는 UI 변경 유지, 최소 범위 복구
- 테스트/검증 결과: 공통으로 대응되는 핀볼 오브젝트의 Transform을 커밋 직전 값과 대조해 복원했다. 완전히 삭제된 중복 범퍼 1개와 Goal Guide 4개도 컴포넌트 및 부모 계층 참조와 함께 복원했다. 씬 전체 검사 결과 중복 fileID 0개, 미해결 로컬 참조 0개이며 `git diff --check`를 통과했다. Unity 실행 검증은 라이선스 클라이언트 제한으로 수행하지 못했다.

## 2026-08-10 아군 배치 수 제한

- 사용한 AI 도구/모델: Codex, GPT-5 계열 모델
- 사용자 요청: 아군 6마리부터 웨이브 시작을 차단하고 7마리부터 핀볼 발사를 차단하며, Status UI에 현재 수/5를 표시하고 6마리 이상을 빨간색으로 표시
- AI 제안 내용: `UnitManager`를 배치 수와 제한 규칙의 단일 기준점으로 사용하고, Manager 방어 검증과 이벤트 기반 UI 갱신을 적용
- AI 실제 수정 영역: `UnitManager`, `BattleManager`, `PinballManager`, `WavePanel`, `StatusPanel`, EditMode 경계값 테스트, Game 씬의 `AllyCountText` 및 Inspector 참조
- 사용자 직접 결정/수정 필요 영역: 사용자가 정확히 6마리일 때 핀볼 발사를 허용하고 표시 형식을 `5/5`로 결정했으며, 실제 Game View에서 최종 텍스트 위치 미세 조정 가능
- 중요한 프롬프트/지시: 기존 구조 보존, 씬 배치와 Inspector 참조 우선, `[SerializeField]` underscore 금지, 최소 변경, 외부 패키지 금지
- 테스트/검증 결과: 구현 전 두 규칙 메서드 부재로 `CS0117` RED 실패를 확인했다. 구현 후 5·6·7 경계를 포함한 focused EditMode 테스트와 전체 EditMode 테스트가 Unity 로그 기준 code 0으로 완료됐고 C# 및 씬 역직렬화 오류가 없었다. 프로젝트에 WebGL 배치 빌드 진입점이 없어 WebGL 빌드는 수행하지 않았으며, 5·6·7마리 실제 배치에 대한 Play Mode 시각 확인은 사용자 직접 확인 항목으로 남았다.

## 2026-08-10 아군 준비 배치 제한 및 복원

- 사용한 AI 도구/모델: Codex, GPT-5 계열 모델
- 사용자 요청: 아군을 맵 오른쪽 절반에만 배치하고 오른쪽 끝까지 사용할 수 있게 하며, 소환 시 가로 우선 격자로 배치하고 웨이브 종료 후 기존 배치를 복원
- AI 제안 내용: 전체 전투 경계와 아군 준비 배치 경계를 분리하고 `UnitManager`가 캐릭터별 준비 위치를 런타임 동안 보존
- AI 실제 수정 영역: `BattleAreaBounds`, `UnitSpawner`, `UnitManager`, `AlllyUnit`, EditMode 배치 테스트, Game 씬의 `Panel_BattleArea`, AI 사용 기록
- 사용자 직접 결정/수정 필요 영역: 사용자가 오른쪽 절반, 가로 우선 격자, 캐릭터별 위치 저장 방식을 결정했으며 최종 배치 간격과 체감은 Game View에서 확인 가능
- 중요한 프롬프트/지시: 기존 구조와 적/전투 이동 보존, 최소 수정, Inspector 참조 유지, SetActive 풀링 유지, `[SerializeField]` underscore 금지
- 테스트/검증 결과: 새 테스트는 구현 전 7개와 3개가 각각 의도대로 실패했고 구현 후 배치 테스트 10/10, 전체 EditMode 45/45가 통과했다. `dotnet build Assembly-CSharp-Editor.csproj --no-restore`는 오류 0개로 완료됐으며 기존 패키지 참조 경고 9개가 남았다. 프로젝트에 WebGL 배치 빌드 진입점이 없어 실제 WebGL 빌드와 Game View 체감 확인은 수행하지 못했다.

## 2026-08-10 게임플레이 피드백 마일스톤

- 사용한 AI 도구/모델: Codex, GPT-5 계열 모델
- 사용자 요청: 아군 보유 제한 해제와 5명 웨이브 참가 제한, 아군 영구 사망, 2초 결과 대기 상태, 강화된 HP/골드 피드백, 발사 비용 유지, 보드·손잡이 발광
- AI 제안 내용: `UnitManager` 영구 사망 정리, 명시적 `Resolving` 상태와 결과 이벤트, UI 비귀속 결과 배너, 기존 mask/additive/Bloom을 재사용하는 발광 상태 컴포넌트
- AI 실제 수정 영역: 유닛 roster/발사 조건, `BattleManager`와 resolution 도메인, `WaveResultPanel`·`StatusPanel`, 보드/손잡이 발광 코드와 Game 씬 배선, EditMode 테스트
- 사용자 직접 결정/수정 필요 영역: 사용자가 전용 종료 상태와 2초 대기, 안내 문구 없는 손잡이 발광을 선택했으며 튜토리얼·금색 유닛 조명·접지 그림자는 이번 작업에서 제외했다. 최종 발광 강도와 배너 위치는 Game View에서 미세 조정 가능하다.
- 중요한 프롬프트/지시: 기존 구조·물리·콜라이더 보존, Inspector 참조 우선, 외부 패키지 금지, `발사 {비용}G` UI 유지, 범위 밖 기능 제외
- 테스트/검증 결과: Task 1 집중 테스트 18/18 통과. 최종 핵심 EditMode 묶음은 최초 36/37 통과 후 BoardGlow 스프라이트 fileID를 수정했고, 해당 씬 회귀 테스트 1/1이 통과했다. 구현 전 전체 기준선의 기존 `EnemyWaveVisualTests` 3개 실패는 사용자 승인 아래 범위 밖으로 유지했다. Unity 컴파일은 EditMode 실행에서 완료됐고, 별도 `dotnet build`는 설치된 .NET SDK가 없어 실행할 수 없었다. 사용자 요청에 따라 반복 전체 테스트, Play Mode 및 WebGL 빌드는 생략했다.
## 2026-08-10 전투 캐릭터 공격·진화·그림자 이펙트 연결

- 사용한 AI 도구/모델: Codex, GPT-5 계열 모델
- 사용자 요청: 화살·불·진화 이펙트를 캐릭터 공격과 진화에 적용하고, 그림자를 하체 위치로 조정하며 Cat2 진화형에 총구 화염 이펙트 추가
- AI 제안 내용: Cat1 `archer`와 `marksman`은 아군 화살, Cat2 진화형 `ranger`는 총구 화염, `mage`·`pyromancer`는 불 투사체, `goblin_archer`는 적 화살을 사용하고 진화 시 공용 광채를 재생하도록 연결
- AI 실제 수정 영역: `UnitAttackEffectPlayer` 및 전투 명중 호출 연결, 아군·적 프리팹 이펙트 에셋 참조, Game 씬 `EvolutionGlowEffect`와 `UnitManager` 참조, 아군·적 `GroundShadow` 위치, EditMode 연결 검증 테스트
- 사용자 직접 결정/수정 필요 영역: 실제 Game View에서 Cat2 총구 위치와 투사체 속도, 진화 광채 크기, 하체 그림자 높이를 취향에 맞게 미세 조정 가능
- 중요한 프롬프트/지침: 기존 에셋 재사용, Inspector 참조 유지, 공격 중 반복 생성 대신 캐릭터별 이펙트 인스턴스 재사용, 기존 작업 변경 보존
- 테스트/검증 결과: Unity 강제 리컴파일 오류 0건, `UnitAttackEffectPlayerTests` 5/5 통과. 사용자 요청에 따라 전체 EditMode 및 최종 플레이 체감 검증은 실행하지 않고 사용자 확인으로 인계
## 2026-08-10 고블린 킹 공격 이펙트·보스 및 UI 클릭 SFX 연결

- 사용한 AI 도구/모델: Codex, GPT-5 계열 모델
- 사용자 요청: `H_MountedMageBoss`로 연결된 고블린 킹 공격에 보라색 회오리와 새 `boss_wind` 효과음을 적용하고, 새 UI 버튼 클릭음도 전체 버튼에 적용
- AI 제안 내용: `goblin_king` 기본 공격 시 피격 대상 위치에서 `EnemyBossPurpleTornadoEffect`를 0.75초 재생하고 `boss_wind`를 함께 출력하며, 씬 로드마다 실제 Unity UI Button의 `onClick`에 공통 클릭음을 등록
- AI 실제 수정 영역: `UnitAttackEffectPlayer`의 대상 위치 애니메이션 이펙트 재생, `EnemyUnit.prefab` 보스 이펙트 참조와 ID 매핑, `SoundName` 키, Developer 씬 SoundManager의 두 AudioClip 등록, SoundManager의 씬별 버튼 클릭 리스너 관리
- 사용자 직접 결정/수정 필요 영역: 실제 플레이에서 회오리 크기·위치·재생 시간과 `boss_wind`·버튼 클릭음 볼륨 체감 조정 가능
- 중요한 프롬프트/지침: 기존 보스 이펙트 프리팹 재사용, 반복 생성 대신 캐릭터별 인스턴스 재사용, Title·Game 씬 버튼 공통 적용, 기존 변경 보존
- 테스트/검증 결과: 사용자 요청에 따라 리컴파일·자동 테스트·플레이 검증을 실행하지 않고 구현 상태로 인계

## 2026-08-16 핀볼 객체지향 리팩터링

- 사용한 AI 도구/모델: Codex, GPT-5 계열 모델
- 사용자 요청: 기존 핀볼 동작과 프로젝트 고정 구조를 유지하면서 `PinballManager`에 집중된 책임을 별도 워크트리에서 객체 합성으로 분리
- AI 제안 내용: `PinballManager`를 기존 공개 Facade로 유지하고 공 풀, 발사 상태, 아이템 보정, 골 선택 상태를 각각 `PinballBallPool`, `PinballLaunchState`, `PinballItemModifiers`, `PinballGoalController`로 분리
- AI 실제 수정 영역: `PinballManager`, `Pinball`, `PinballReflectorController`, 신규 일반 C# 책임 객체 4개와 설계 문서
- 사용자 직접 결정/수정 필요 영역: 사용자가 별도 워크트리 방식과 전체 핀볼 내부 책임 분리를 승인했으며, `Pinball` 초기화 시 Manager를 명시적으로 전달하지 않고 `App.Get<PinballManager>()`를 사용하도록 결정
- 중요한 프롬프트/지시: 기존 Scene·Prefab·공개 API·이벤트 순서·SetActive 풀링 보존, 새 interface 및 전역 상태 금지, 테스트·빌드·Unity·정적 분석 실행 금지
- 테스트/검증 결과: 사용자 지시에 따라 테스트, 빌드, Unity Editor, 정적 분석과 별도 validation을 실행하지 않았으며 코드 수정과 커밋만 수행

## 2026-08-16 아이템 객체지향 리팩터링

- 사용한 AI 도구/모델: Codex, GPT-5 계열 모델
- 사용자 요청: 핀볼 작업과 분리된 새 워크트리에서 `ItemManager`의 남은 책임을 기존 동작과 공개 API를 유지한 채 리팩터링
- AI 제안 내용: `ItemManager`를 구매와 Unity 생명주기 조정 Facade로 유지하고 카탈로그, 보유 상태, 구독·이벤트 큐를 각각 `ItemCatalogController`, `ItemInventoryController`, `ItemEventController`로 분리
- AI 실제 수정 영역: `ItemManager`와 신규 Controller 3개 및 Unity meta 파일
- 사용자 직접 결정/수정 필요 영역: 사용자가 별도 워크트리를 선택했고, 구현 후 보고된 책임 분리안을 승인된 설계로 간주하기로 결정
- 중요한 프롬프트/지시: 외부 호출부와 이벤트 순서 보존, 기존 App 구조 유지, 범위 밖 UI·전투·튜토리얼 수정 금지, 테스트·빌드·Unity·정적 분석 실행 금지
- 테스트/검증 결과: 사용자 지시에 따라 테스트, 빌드, Unity Editor, 정적 분석과 별도 validation을 실행하지 않았으며 코드 수정과 커밋만 수행

## 2026-08-16 리팩터링 M0 브랜치 통합

- 사용한 AI 도구/모델: Codex, GPT-5 계열 모델
- 사용자 요청: 별도 승인 절차 없이 M0를 바로 수행해 아이템 리팩터링을 커밋하고 핀볼·아이템 브랜치를 `Dev`에 통합
- AI 제안 내용: 아이템 변경만 먼저 독립 커밋한 뒤 핀볼, 아이템 순서로 비강제 병합하고 작업 브랜치와 워크트리는 삭제하지 않고 보존
- AI 실제 수정 영역: `codex/item-oop-refactor` 구현 커밋, `codex/pinball-oop-refactor`와 `codex/item-oop-refactor`의 `Dev` 병합, 본 AI 사용 기록
- 사용자 직접 결정/수정 필요 영역: 사용자가 로컬 `Dev` 통합을 직접 지시했으며 후속 M1 범위와 시작 시점은 별도 결정 필요
- 중요한 프롬프트/지시: 설계 승인 없이 M0 즉시 수행, 기존 변경 보존, 테스트·빌드·Unity·정적 분석 실행 금지
- 테스트/검증 결과: 두 브랜치는 Git 충돌 없이 `Dev`에 병합됐으며 사용자 지시에 따라 테스트, 빌드, Unity Editor, 정적 분석과 별도 validation은 실행하지 않음

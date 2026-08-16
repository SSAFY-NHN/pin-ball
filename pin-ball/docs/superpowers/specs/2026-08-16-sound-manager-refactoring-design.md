# SoundManager 리팩터링 설계

## 목표

`SoundManager` 클래스와 모든 공개 API를 유지하면서 BGM, SFX 풀, Mixer, UI 클릭음 등록 책임을 하위 Controller로 분리한다. 기존 Scene 연결과 사운드 동작을 보존한다.

## 구성 요소

### SoundBgmController

BGM 이름과 AudioClip 사전을 소유한다. `PlayBGM`, `StopBGM`, `FadeBGM`, `FadeInBGM`, `FadeOutBGM`에 해당하는 재생·DOTween 페이드 동작을 담당한다. `SoundManager` 공개 메서드는 Controller에 그대로 위임한다.

### SoundSfxPoolController

SFX 이름과 AudioClip 사전, 사용 가능 Queue, 활성 List를 소유한다. 초기 AudioSource 생성, 부족 시 확장, 재생, 이름별 정지, 전체 정지, 풀 반납을 담당한다. AudioSource는 기존처럼 지정된 SFX GameObject에 `AddComponent`하여 생성한다.

### SoundMixerController

Master/BGM/SFX 음소거 상태를 소유하고 AudioMixer 파라미터를 설정한다. 기존 `ToggleMute(EVolumeType)`와 `IsMuted()`는 이 Controller에 위임한다. 기존 `ToggleMute(bool)`는 BGM AudioSource 볼륨을 직접 토글하는 공개 동작이므로 BGM Controller에 위임한다.

### SoundButtonClickController

Scene의 Button 검색, 클릭 리스너 등록, 이전 Scene 리스너 해제를 담당한다. Scene 로드 이벤트 구독은 Unity 수명주기상 `SoundManager`가 유지하고, 로드 시 Controller에 갱신을 요청한다.

## Button 명시 등록 검토

`FindObjectsByType<Button>()`를 즉시 제거하고 모든 Scene Button을 배열로 연결하면 Developer/Title/Game Scene의 다수 버튼에 수동 참조가 필요하며 누락 시 기존 클릭음이 사라진다. 이번 단계에서는 동작 보존을 우선해 검색 책임만 Controller로 격리한다.

후속 Scene UI 정비 시 각 Scene의 UI Controller가 자신이 소유한 Button을 `SoundManager`에 명시 등록하는 API로 전환할 수 있다. 해당 API와 Scene 변경은 이번 범위에 추가하지 않는다.

## 활성 SFX 갱신 결정

현재 `Update()`는 실제 활성 SFX List만 역순으로 순회하며 초기 풀 크기는 5개다. 음원별 반환 코루틴은 수동 정지, DOTween 페이드, 루프 음원의 취소와 중복 반환을 별도로 조율해야 한다. 현재 비용보다 복잡도가 크므로 매 프레임 순회를 유지한다. 단, 순회 구현은 `SoundSfxPoolController.UpdateActiveSources()`로 이동한다.

## 직렬화 필드 이름

underscore가 있는 `[SerializeField]` 필드를 프로젝트 규칙에 맞춰 underscore 없는 camelCase로 변경한다. 각 필드에 `[FormerlySerializedAs("기존이름")]`를 추가해 Developer Scene의 직렬화 연결을 보호한다.

대상은 BGM/SFX Clip 배열, BGM/SFX Player, AudioMixer와 Group, 두 볼륨, 초기 풀 크기다. `startupBgmName`은 이미 규칙에 맞으므로 유지한다.

## SoundName.GetAttack 검토

유닛 ID별 공격 사운드를 TitleData/JSON로 이동하려면 아이템·유닛 데이터 형식과 로딩 규약 변경이 필요하다. 이번 SoundManager 고정 API 리팩터링 범위를 넘으므로 기존 `SoundName.GetAttack(string)` 분기를 유지한다. 데이터 스키마 리팩터링 시 별도 마일스톤으로 처리한다.

## 보존 사항

- `SoundManager` 클래스와 공개 메서드 시그니처
- `SoundName` 상수와 `GetAttack()`
- 시작 BGM, 볼륨, 페이드 시간과 정지 동작
- SFX 반환, 이름별 페이드 정지, 전체 정지
- 모든 Scene Button의 자동 클릭음
- Scene 직렬화 연결

## 확인 범위

사용자 지시에 따라 테스트, 빌드, Unity 실행, Test Runner, 정적 분석은 수행하지 않는다. 공개 API 위임, Controller 수명, 직렬화 이름 호환을 코드 읽기로만 확인한다.

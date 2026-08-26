# MoonlitWorkshop 핀볼 테마 교체 AI 사용 기록

## 사용한 AI 도구/모델

- OpenAI Codex (GPT-5 기반)
- PowerShell 및 Unity 프로젝트 정적 검증 도구

## 사용자 요청

- `Assets/03. Images/Pinball/Arcane`을 사용하던 핀볼 보드와 보드 오브젝트를 `MoonlitWorkshop` 리소스로 교체
- 핀볼 공과 공 전용 효과만 기존 Arcane 리소스 유지

## AI 제안 내용

- 기존 물리 오브젝트, 충돌체, 배치와 게임 규칙은 유지하고 시각 리소스만 교체
- MoonlitWorkshop의 보드, 범퍼, 핀, 디플렉터, 기어, 스피너, 포지 크로스, 스프링 게이트, 가이드 레일을 기존 역할에 맞게 매핑
- 새 이미지 크기에 맞춰 씬 오브젝트의 표시 크기와 런처 방향을 조정
- VFX 카탈로그가 Unity 재로드 시 Arcane 보드 마스크를 다시 연결하지 않도록 빌더 매핑도 함께 변경

## AI 실제 수정 영역

- `Assets/01. Scenes/02. Game.unity`
  - 보드 배경과 글로우, 범퍼, 핀, 자석, 반사판, 결과 슬롯 오브젝트, 런처 스프라이트를 MoonlitWorkshop으로 교체
  - 새 스프라이트 비율에 맞춰 크기와 런처 회전을 조정
- `Assets/Resources/ArcaneVFX/ArcaneVfxCatalog.asset`
  - 공 관련 항목을 제외한 마스크와 보드 오브젝트 효과를 MoonlitWorkshop 리소스로 교체
- `Assets/02. Scripts/Pinball/Editor/ArcaneVfxCatalogBuilder.cs`
  - 공 관련 항목은 Arcane, 보드 관련 항목은 MoonlitWorkshop에서 다시 생성하도록 경로 매핑 변경
- `Assets/02. Scripts/Visual/ArcaneVfxCatalog.cs`
  - 골 흡수 링과 버스트용 MoonlitWorkshop 전용 `goalRing` 항목 추가
- `Assets/02. Scripts/Pinball/PinballGoal.cs`
  - 골 링 연출이 공 전용 Arcane 링 대신 MoonlitWorkshop 골 링을 사용하도록 변경
- `Assets/02. Scripts/03. UI/Editor/GameplayFeedbackSceneTests.cs`
  - MoonlitWorkshop 보드·골 연출과 Arcane 공의 리소스 경계를 검사하는 EditMode 테스트 추가
  - 테스트 도중 카탈로그 에셋을 재생성하지 않고 체크인된 에셋 자체를 검증하도록 구성
- `Tools/validate_moonlit_pinball_theme.py`
  - 씬 계층, 스프라이트 GUID, VFX 카탈로그 매핑을 검사하는 정적 검증기 추가

## 사용자 직접 결정/수정 필요 영역

- 실제 플레이 화면에서 오브젝트 크기와 위치의 미세 조정이 필요한지 최종 확인
- MoonlitWorkshop 보드 하단 슬롯 그림과 기존 4개 결과 콜라이더의 시각적 정렬 확인

## 중요한 프롬프트/지시

- 공을 제외한 핀볼 보드 전체와 보드에 들어가는 오브젝트를 MoonlitWorkshop 테마로 변경
- 기존 물리, 위치, 게임 기능은 유지
- 핀볼 공과 공 전용 효과만 Arcane 유지

## 테스트 및 검증 결과

- `Tools/validate_moonlit_pinball_theme.py`: 실패 상태 확인 후 최종 0 failures 통과
- MoonlitWorkshop GUID와 Sprite subasset fileID 일치 검사: 0 failures
- Unity 에디터 스크립트 재컴파일: 오류 0개
- 전체 솔루션 `dotnet build`: 기존 DOTween 프로젝트 참조 문제로 실패했으며 이번 변경 파일의 오류는 아님
- Unity EditMode `GameplayFeedbackSceneTests`: Moonlit/Arcane 경계 관련 테스트 3개 통과
- 같은 테스트 클래스의 기존 런처 UI 검사 1개는 원래 씬에도 존재하는 `LaunchCost` 텍스트 때문에 실패했으며, 이번 테마 교체 diff에는 해당 오브젝트나 계층 변경이 없음

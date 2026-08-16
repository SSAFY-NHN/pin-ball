# 전투 배속·핀볼 콤보 UI AI 활용 기록

- 사용한 AI 도구/모델: OpenAI Codex (GPT-5 계열)
- 사용자 요청: 전투 중에만 실제 적용되는 1×/2× 선택 배속, 전투 시작 버튼의 맵 상단 중앙 이동, 큰 범퍼 충돌 콤보와 2초 제한 시간 시각화, 연속 콤보 DOTween 스케일 연출 추가. 핀볼 발사 후 되돌아오는 낙하 문제는 사용자 직접 수정으로 범위 제외.
- AI 제안 내용: 선택 배속과 실제 `Time.timeScale` 분리, `Active` 상태에만 선택 배속 적용, `PinballManager` 하위 콤보 상태 Controller, 씬 사전 배치 배경·전경 TMP와 `RectMask2D`를 이용한 오른쪽부터 감소하는 텍스트 fill, `DOPunchScale` 피드백.
- AI 실제 수정 영역: `GameSpeedController`, `PinballComboController`, `PinballComboDisplay`, `PinballManager`, Game 씬의 배속·콤보 UI와 전투 시작 버튼 위치, 기술 설계 문서와 본 AI 활용 기록.
- 사용자 직접 결정/수정 필요 영역: 핀볼 발사 레인 복귀 낙하 문제는 사용자가 직접 수정. 배속 버튼과 콤보 텍스트의 최종 시각 강도·위치는 Game View에서 필요할 경우 조정.
- 중요한 프롬프트/지시: 준비 상태 실제 배속은 항상 1로 유지하되 UI 선택값은 유지, 콤보 TMP 런타임 생성 금지, 2초 fill 감소 표시, 연속 콤보 DOTween scale 애니메이션, 상세 구현 계획 생략, 불필요한 검사와 검사를 위한 검사 금지.
- 테스트/검증 결과: 사용자 지시에 따라 진행 중이던 Unity 검사를 중단하고 추가했던 테스트 파일을 제거함. 이후 테스트, 빌드, Unity 실행과 정적 분석을 수행하지 않았으며 변경 코드와 Game 씬 직렬화 참조를 직접 읽어 확인한 범위만 반영함.

- Follow-up fix: Scene UI file IDs beginning with `9300...` exceeded signed 64-bit range and collided with Unity's `SceneRoots` max identifier. Reassigned only the added UI IDs to the unused `9190...` range. No Unity run, build, or test was performed per user instruction.

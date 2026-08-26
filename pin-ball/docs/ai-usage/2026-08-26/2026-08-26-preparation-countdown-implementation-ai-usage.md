# 60초 전투 준비 제한 AI 활용 기록

- 요청: 기존 기획과 Gold·핀볼 동작은 유지하고 준비 단계에 60초 제한과 자동 시작을 추가한다.
- 결정: `Pending`에서만 순수 C# 카운트다운을 진행하고, 0초에 기존 `BattleManager.TryStartWave`를 한 번 호출한다. 기존 시작 버튼에는 남은 초를 표시한다.
- 수정: `PreparationCountdown`, `BattleManager`, `WavePanel`, 관련 EditMode 테스트, 설계와 구현 계획 문서.
- 보존: Gold 초기화 없음, 핀볼 상태 변경 없음, 구매·강화 단계 변경 없음, 성공·실패 시 아군 제거 규칙 유지.
- 후속 확정: 발사대 직접 조작 비활성화, 튜토리얼 중 타이머 정지, 10초 시각 강조와 5초 경고음, 배속과 무관한 실제 60초, 전투 지연 Gold 파밍을 허용된 위험·보상 전략으로 명시했다.
- 검증: `git diff --check` 통과. 순수 `PreparationCountdown.cs`는 Unity Roslyn과 Unity `mscorlib.dll`로 독립 컴파일 성공.
- 제한: Unity batchmode를 두 번 시도했으나 `Connection to channel LicenseClient-SSAFY refused`와 `Timed-out after 60.01s, waiting for Licensing to initialize`로 EditMode 테스트와 전체 Unity 컴파일을 완료하지 못했다. 로컬 시스템 `dotnet`에는 SDK가 설치되지 않아 생성된 `.csproj` 빌드도 실행할 수 없었다.

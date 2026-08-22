# 비활성 기능 코드 정리 AI 활용 기록

## 사용한 AI 도구/모델

- Codex 기반 코드 분석·수정
- 로컬 Git, ripgrep, .NET C# 컴파일

## 사용자 요청

- `temp`에서 삭제·비활성화된 기능의 잔여 코드만 정리
- 함수 → 변수 → 클래스 → 스크립트 순서 준수
- 현재 사용하는 전투와 핀볼 기능 보존

## AI 제안 내용

- C# 참조뿐 아니라 씬·프리팹 GUID와 직렬화 필드까지 확인하는 보수적 삭제
- 각 단계 독립 검증·커밋
- 현재 참조가 남은 핀볼 발사·골·카메라 피드백 코드는 보존

## AI 실제 수정 영역

- 합성·진화 함수, 상태, 타입, UI, 효과, 테스트 삭제
- 비활성 레거시 튜토리얼 타입과 씬 컴포넌트 삭제
- `WavePanel`의 숨겨진 수동 발사 UI 참조 삭제
- 미사용 `GameLayoutController` 삭제
- 합성 능력치 직렬화 데이터 삭제
- 방어선 `Use Full Kinematic Contacts` 비활성화
- 제거 목록 문서 갱신

## 사용자 직접 결정/수정 필요 영역

- 사용자 소유 변경 파일 2개는 수정·스테이징하지 않음
- Unity 라이선스 복구 후 EditMode 전체 실행 필요

## 중요한 프롬프트/지시

- 작업 브랜치: `temp`
- 드래그 위치 이동, 전술 증원, 고정 10웨이브, 재시도, 양측 방어선 유지
- WebGL에 적합한 Trigger + Kinematic Rigidbody2D 유지

## 테스트/검증 결과

- 제거 대상 C# 심볼 검색
- 삭제 스크립트 GUID의 씬·프리팹·에셋 검색
- `Assembly-CSharp.csproj` 컴파일
- `Assembly-CSharp-Editor.csproj` 컴파일
- `git diff --check`
- Unity EditMode 테스트: 실행 시 `Licensing initialization failed`, `Connection to channel LicenseClient-Home refused`; 결과 XML 미생성으로 테스트 미실행 처리

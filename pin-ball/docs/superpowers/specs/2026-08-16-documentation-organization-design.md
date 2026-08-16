# 프로젝트 문서 구조 정리 설계

## 목표

`docs` 루트와 `docs/superpowers` 아래에 흩어진 Markdown 문서를 목적별로 정리하고, 현재 구현 상태와 맞지 않는 최신 리팩터링 문서를 갱신한다. Git 이력을 보존할 수 있도록 내용 변경과 파일 이동을 한 구현 커밋에 포함한다.

## 폴더 구조

```text
docs/
  README.md
  designs/
    YYYY-MM-DD/
  plans/
    YYYY-MM-DD/
  ai-usage/
    YYYY-MM-DD/
  content/
  handbook/
```

### designs

기술 설계와 승인된 구조 결정을 보관한다. 기존 `docs/superpowers/specs` 문서를 날짜별 하위 폴더로 이동한다.

### plans

과거 구현 계획과 작업 체크리스트를 보관한다. 기존 `docs/superpowers/plans` 문서를 날짜별 하위 폴더로 이동한다.

### ai-usage

AGENTS 규칙에 따른 AI 활용 기록을 날짜별로 보관한다. 현재 `docs` 루트의 `*-ai-usage.md`를 이동한다.

### content

게임 소개 영상 대본처럼 기술 설계·작업 기록이 아닌 콘텐츠 문서를 보관한다.

### handbook

새 채팅 인계 지침처럼 프로젝트 작업 방법과 인계 문서를 보관한다.

## 루트 유지 문서

- `AGENTS.md`: Agent가 저장소 진입 시 읽는 고정 규칙이므로 이동하지 않는다.
- `.github/project-master-prompt.md`: 기존 프로젝트 지침 위치를 유지한다.

## 내용 갱신

M2~M8의 최신 설계 문서에 `구현 상태`를 추가한다. 구현 완료 여부와 설계/구현 커밋을 사실대로 기록한다. M6에서 의도적으로 유지한 Button 자동 검색과 M8의 Tutorial PlayerPrefs 유지 정책도 최종 결정으로 명시한다.

이전 8월 9~10일 설계와 계획은 당시 의사결정 기록이므로 내용은 임의로 현대화하지 않는다. 명백한 내부 링크만 새 경로로 갱신한다.

## 문서 인덱스

`docs/README.md`를 추가해 각 폴더의 목적, 최신 리팩터링 M2~M8 링크, 새 문서 저장 규칙을 안내한다.

## 링크와 이름

기존 파일명은 날짜와 주제를 이미 포함하므로 원칙적으로 유지한다. 루트 한글 인계 문서도 이름은 유지하고 위치만 `docs/handbook`으로 옮긴다. Markdown 내부에서 옛 `docs/superpowers` 경로를 직접 가리키는 링크는 새 `docs/designs` 또는 `docs/plans` 경로로 변경한다.

## 확인 범위

사용자 지시에 따라 별도 문서 검사기, 링크 검사기, 빌드, 테스트, 정적 분석은 실행하지 않는다. Git 변경 목록과 Markdown의 직접 경로 참조를 읽어 이동 누락만 확인한다.

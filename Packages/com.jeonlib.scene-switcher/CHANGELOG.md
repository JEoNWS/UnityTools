# Changelog

이 프로젝트의 변경 내역입니다. 형식은 [Keep a Changelog](https://keepachangelog.com/ko/1.1.0/)를 따르고, 버전은 [Semantic Versioning](https://semver.org/lang/ko/)을 따릅니다.

## [1.1.0] - 2026-11-05

### Added
- 개별 씬 등록: 폴더 없이 씬을 하나씩 등록해 목록 맨 위 "개별 씬" 그룹에 보여 줌
- 등록 방법: 설정 패널의 씬 입력칸, 씬을 창으로 끌어다 놓기, 우클릭 메뉴 "개별 씬으로 등록"
- 해제 방법: 설정 패널 목록의 `-` 버튼, 우클릭 메뉴 "개별 씬에서 빼기"
- EditMode 테스트 18개 추가 (설정, 로직, 창 조작)

### Changed
- 폴더에 포함된 씬도 개별 등록할 수 있고, 이때 두 그룹에 모두 표시되며 검색과 열림 표시도 양쪽에 적용됨
- 빈 목록 안내 문구를 "씬이나 씬 폴더를 추가하세요."로 변경

## [1.0.0] - 2026-10-19

### Added
- 등록한 폴더의 씬을 폴더별로 보여 주는 Scene Switcher 창 (UI Toolkit)
- 클릭 전환, Ctrl+클릭 Additive 열기, Hierarchy로 드래그, 우클릭 메뉴
- 활성 씬과 함께 열린 씬 강조, `[Start]`와 `[Build]` 표시, 이름 검색
- 시작 씬부터 플레이 (직접 지정 또는 Build Settings 첫 씬), 이번 한 번만 현재 씬에서 플레이
- 폴더 등록: 입력칸 또는 끌어다 놓기, 겹치는 폴더 방지와 합치기
- `UserSettings/SceneSwitcher.json`에 사용자별 설정 저장, 외부 수정 시 다시 읽기
- EditMode 테스트 60개 (로직, 설정, 창 조작, 플레이 모드)

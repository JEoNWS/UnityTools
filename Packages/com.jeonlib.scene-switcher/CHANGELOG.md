# Changelog

이 프로젝트의 변경 내역입니다. 형식은 [Keep a Changelog](https://keepachangelog.com/ko/1.1.0/)를 따르고, 버전은 [Semantic Versioning](https://semver.org/lang/ko/)을 따릅니다.

## [1.0.0] - 2026-10-19

### Added
- 등록한 폴더의 씬을 폴더별로 보여 주는 Scene Switcher 창 (UI Toolkit)
- 클릭 전환, Ctrl+클릭 Additive 열기, Hierarchy로 드래그, 우클릭 메뉴
- 활성 씬과 함께 열린 씬 강조, `[Start]`와 `[Build]` 표시, 이름 검색
- 시작 씬부터 플레이 (직접 지정 또는 Build Settings 첫 씬), 이번 한 번만 현재 씬에서 플레이
- 폴더 등록: 입력칸 또는 끌어다 놓기, 겹치는 폴더 방지와 합치기
- `UserSettings/SceneSwitcher.json`에 사용자별 설정 저장, 외부 수정 시 다시 읽기
- EditMode 테스트 60개 (로직, 설정, 창 조작, 플레이 모드)

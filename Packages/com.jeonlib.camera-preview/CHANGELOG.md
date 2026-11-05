# Changelog

이 프로젝트의 변경 내역입니다. 형식은 [Keep a Changelog](https://keepachangelog.com/ko/1.1.0/)를 따르고, 버전은 [Semantic Versioning](https://semver.org/lang/ko/)을 따릅니다.

## [1.0.0] - 2026-10-30

### Added
- 선택한 카메라의 화면을 계속 보여 주는 Camera Preview 창 (UI Toolkit)
- 핀으로 카메라 고정, 지금 열린 화면의 카메라 메뉴
- 비율 선택: Game 뷰, 16:9, 4:3, 1:1
- Built-in은 `Camera.Render`, URP와 HDRP는 `RenderPipeline.SubmitRenderRequest`로 자동 분기
- Prefab Mode 카메라는 프리팹 편집 씬을 찍도록 처리
- 배경 알파가 0인 카메라도 알파를 무시하고 표시
- 플레이 중, 카메라 없음, 프리팹 에셋, 지원하지 않는 파이프라인 안내
- EditMode 테스트 25개

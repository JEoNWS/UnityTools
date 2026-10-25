# Changelog

이 프로젝트의 변경 내역입니다. 형식은 [Keep a Changelog](https://keepachangelog.com/ko/1.1.0/)를 따르고, 버전은 [Semantic Versioning](https://semver.org/lang/ko/)을 따릅니다.

## [1.0.0] - 2026-10-20

### Added
- 선택한 오브젝트의 월드 Position, Rotation, Scale을 보여 주고 수정하는 World Transform 창 (UI Toolkit)
- 여러 오브젝트 일괄 편집: 값이 다른 축은 `—`로 표시하고, 수정한 축만 적용
- 부모와 자식을 함께 선택해도 부모부터 적용해 둘 다 목표 값으로 맞춤
- 부모의 비균등 스케일과 회전으로 찌그러진(skew) 대상 경고
- 기즈모 이동, 선택 변경, Undo/Redo를 따라 값 갱신
- Rotation은 입력하던 형태(음수, 90° 초과, 360° 초과)를 유지해 이어서 표시
- EditMode 테스트 24개

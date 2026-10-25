# Changelog

이 프로젝트의 변경 내역입니다. 형식은 [Keep a Changelog](https://keepachangelog.com/ko/1.1.0/)를 따르고, 버전은 [Semantic Versioning](https://semver.org/lang/ko/)을 따릅니다.

## [1.0.0] - 2026-10-22

### Added
- 폴더의 테이블 txt 목록과 셀 그리드를 보여 주는 Table Editor 창 (IMGUI, 행 가상화)
- 기존 셀 값만 수정: id 열, 행 추가/삭제, 탭/줄바꿈 입력은 막음
- 수정한 셀과 행 표시, 수정한 행만 보기, 검색, 되돌리기
- 엑셀 붙여넣기용 TSV 복사: 선택한 행, 선택한 열(헤더 제외), 수정한 행, 전체(헤더 포함)
- `ITableEditorBridge`로 프로젝트별 테이블 폴더, 암호화(decode/encode), 플레이 중 런타임 적용 연결
- 브리지가 없으면 평문 txt를 다루는 기본 브리지 사용, 폴더 직접 지정
- 플레이 중 수정 즉시 적용, 또는 모아서 한 번에 적용
- 도메인 리로드 뒤에도 수정 중인 값 유지
- EditMode 테스트 25개

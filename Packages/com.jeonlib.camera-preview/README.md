# Camera-Preview

씬뷰 오른쪽 아래의 카메라 미리보기는 작고, 다른 오브젝트를 선택하면 사라집니다. 이 창은 선택하거나 핀으로 지정한 카메라의 화면을 **원하는 크기로 계속** 보여 줍니다.
UI Toolkit으로 만들었고 게임 코드에 의존하지 않습니다.

## 기능

- **선택한 카메라 미리보기**: Hierarchy나 씬에서 Camera가 있는 오브젝트를 선택하면 그 카메라의 화면을 보여 줍니다. 씬을 편집하면 바로 따라 바뀝니다.
- **핀**: 선택이 바뀌어도 지금 카메라를 계속 보여 줍니다. 다른 오브젝트를 옮기면서 카메라에 어떻게 보이는지 확인할 때 씁니다.
- **카메라 메뉴**: 지금 열린 화면(씬 또는 Prefab Mode)의 카메라 목록에서 골라 바로 핀으로 지정합니다. 씬뷰용 내부 카메라는 목록에서 빠집니다.
- **비율**: Game 뷰 해상도를 따르거나 16:9, 4:3, 1:1 중에서 고릅니다. 남는 영역은 검은 여백으로 채웁니다.
- **렌더 파이프라인 자동 판별**: Built-in은 `Camera.Render`, URP와 HDRP는 `RenderPipeline.SubmitRenderRequest`(StandardRequest)로 렌더합니다. 파이프라인 패키지를 직접 참조하지 않습니다.
- **Prefab Mode 카메라**: 프리팹 편집 화면 안의 카메라는 메인 씬이 아니라 프리팹 내용을 찍습니다.
- **투명 배경 카메라**: 배경 알파가 0인 카메라(텍스처 렌더용 등)도 Game 뷰처럼 알파를 무시하고 색 그대로 보여 줍니다.

## 제한 사항

- 플레이 중에는 미리보기를 멈추고 안내만 표시합니다. 플레이 중에는 Game 뷰를 사용하세요.
- Project 창의 프리팹 에셋 안 카메라는 미리볼 수 없습니다. Prefab Mode로 열거나 씬에 놓고 선택하세요.
- 창을 열어 두는 동안 카메라를 초당 20번 다시 렌더합니다. 무거운 씬에서는 필요할 때만 열어 두세요.
- StandardRequest를 지원하지 않는 커스텀 렌더 파이프라인에서는 미리볼 수 없다는 안내가 표시됩니다.

## 요구 사항

- Unity 6000.0 이상 (6000.4에서 확인했습니다)
- Built-in, HDRP에서 확인했습니다. URP는 같은 StandardRequest 방식으로 지원하지만 아직 직접 확인하지 않았습니다.

## 설치

Package Manager에서 **+ > Install package from git URL...** 을 누르고 아래 주소를 입력합니다.

```
https://github.com/JEoNWS/UnityTools.git?path=/Packages/com.jeonlib.camera-preview#camera-preview/v1.0.0
```

또는 `Packages/manifest.json`의 `dependencies`에 직접 추가합니다.

```json
"com.jeonlib.camera-preview": "https://github.com/JEoNWS/UnityTools.git?path=/Packages/com.jeonlib.camera-preview#camera-preview/v1.0.0"
```

`#` 뒤의 태그를 빼면 기본 브랜치의 최신 코드가 설치됩니다.

## 사용법

1. **Tools > Camera Preview > Open** 으로 창을 엽니다.
2. Camera가 있는 오브젝트를 선택하거나, 툴바의 **카메라 ▾** 에서 카메라를 고릅니다.
3. 계속 보고 싶으면 **핀**을 켭니다. 핀을 끄면 다시 선택한 카메라를 따라갑니다.

## 테스트

패키지 테스트를 실행하려면 `Packages/manifest.json`에 `testables`를 추가합니다.

```json
"testables": [ "com.jeonlib.camera-preview" ]
```

그다음 **Window > General > Test Runner > EditMode** 에서 `JeonLib.CameraPreview.Editor.Tests`를 실행합니다. 테스트는 임시 GameObject만 만들었다가 지우고, 에셋이나 씬은 건드리지 않습니다.

## 라이선스

[MIT](LICENSE.md)

# Scene-Switcher

지정한 폴더의 씬 목록을 보여 주고, 클릭 한 번으로 에디터 씬을 전환하는 Unity 에디터 창입니다.
UI Toolkit으로 만들었고 게임 코드에 의존하지 않습니다.

## 기능

- **씬 목록**: 등록한 폴더(하위 폴더 포함)의 씬을 폴더별로 묶어 보여 줍니다. 이름으로 검색할 수 있습니다.
- **씬 전환**
  - 클릭: 전환 (저장하지 않은 변경이 있으면 먼저 묻습니다)
  - Ctrl+클릭 (Mac은 Cmd+클릭): 지금 씬을 유지한 채 추가로 열기 (Additive)
  - 드래그: Hierarchy에 놓으면 추가로 열립니다
  - 우클릭 메뉴: 전환, 추가로 열기, 닫기, 이 씬에서 바로 플레이, 시작 씬 지정, Project에서 보기
- **열린 씬 표시**: 활성 씬은 진하게, Additive로 함께 열린 씬은 옅게 강조합니다. `[Start]`는 시작 씬, `[Build]`는 Build Settings에 포함된 씬입니다.
- **시작 씬부터 플레이**: 켜 두면 어떤 씬을 열어 두었든 Play는 시작 씬부터 실행되고, Stop하면 원래 씬으로 돌아옵니다.
  - 시작 씬은 직접 지정할 수 있고, 비워 두면 Build Settings의 첫 활성 씬을 씁니다.
  - "이 씬에서 바로 플레이"를 쓰면 이번 한 번만 현재 씬에서 시작합니다.
- **폴더 등록**: 설정 패널의 입력칸에 넣거나 Project 창에서 폴더를 창으로 끌어다 놓습니다. 이미 등록된 폴더의 하위 폴더는 막고, 하위 폴더가 등록된 상위 폴더를 추가하면 합칠지 묻습니다.

## 요구 사항

- Unity 6000.0 이상 (6000.4에서 확인했습니다)

## 설치

Package Manager에서 **+ > Install package from git URL...** 을 누르고 아래 주소를 입력합니다.

```
https://github.com/JEoNWS/UnityTool.git?path=/Packages/com.jeonlib.scene-switcher#scene-switcher/v1.0.0
```

또는 `Packages/manifest.json`의 `dependencies`에 직접 추가합니다.

```json
"com.jeonlib.scene-switcher": "https://github.com/JEoNWS/UnityTool.git?path=/Packages/com.jeonlib.scene-switcher#scene-switcher/v1.0.0"
```

`#` 뒤의 태그를 빼면 기본 브랜치의 최신 코드가 설치됩니다.

## 사용법

1. **Tools > Scene Switcher > Open** 으로 창을 엽니다.
2. 처음에는 설정 패널이 열려 있습니다. 씬이 있는 폴더를 추가합니다.
3. 목록에서 씬을 클릭해 전환합니다.
4. 툴바의 **▶ 시작 씬부터** 로 시작 씬부터 플레이를 켜고 끕니다. **Tools > Scene Switcher > Play From Start Scene** 메뉴로도 바꿀 수 있습니다.

## 설정 저장 위치

설정은 프로젝트의 `UserSettings/SceneSwitcher.json`에 저장됩니다. `UserSettings/`는 보통 버전 관리에서 제외하므로 사용자마다 따로 유지됩니다.

```json
{
    "m_sceneFolderGuids": [ "..." ],
    "m_playFromStartScene": true,
    "m_startSceneGuid": ""
}
```

폴더와 씬은 GUID로 저장하므로 이름이나 위치를 바꿔도 따라갑니다.

## 테스트

패키지 테스트를 실행하려면 `Packages/manifest.json`에 `testables`를 추가합니다.

```json
"testables": [ "com.jeonlib.scene-switcher" ]
```

그다음 **Window > General > Test Runner > EditMode** 에서 `JeonLib.SceneSwitcher.Editor.Tests`를 실행합니다.

- 테스트는 `Assets/` 아래에 임시 폴더와 씬을 만들었다가 지우고, 씬을 전환하거나 플레이 모드에 들어갑니다.
- 저장하지 않은 씬이 열려 있으면 저장 확인 창이 뜰 수 있으니, 저장한 뒤 실행하세요.
- 실행 중 실제 설정 파일은 `Temp/`에 백업했다가 끝나면 복구합니다.

## 라이선스

정해지지 않았습니다.

# UnityTool

JeonLib Unity 에디터 도구 모음입니다. 도구마다 독립된 UPM 패키지라서 필요한 것만 골라 설치할 수 있습니다.

## 도구 목록

| 패키지 | 설명 | 최신 버전 |
|---|---|---|
| [Scene-Switcher](Packages/com.jeonlib.scene-switcher) | 지정한 폴더의 씬 목록에서 클릭 한 번으로 에디터 씬 전환, 시작 씬부터 플레이 | 1.0.0 |
| [World-Transform](Packages/com.jeonlib.world-transform) | 선택한 오브젝트의 월드 기준 Position, Rotation, Scale 표시와 수정, 다중 선택 일괄 편집 | 1.0.0 |
| [Table-Editor](Packages/com.jeonlib.table-editor) | TSV 테이블 txt의 기존 값 수정, 엑셀용 복사, 브리지로 암호화와 플레이 중 런타임 적용 연결 | 1.0.0 |

## 설치

Package Manager에서 **+ > Install package from git URL...** 을 누르고 아래 형식의 주소를 입력합니다.

```
https://github.com/JEoNWS/UnityTool.git?path=/Packages/<패키지 이름>#<도구>/v<버전>
```

예: Scene-Switcher 1.0.0

```
https://github.com/JEoNWS/UnityTool.git?path=/Packages/com.jeonlib.scene-switcher#scene-switcher/v1.0.0
```

자세한 사용법은 각 패키지 폴더의 README를 참고하세요.

## 구조

```
Packages/
└─ com.jeonlib.<도구>/
   ├─ package.json, README.md, CHANGELOG.md
   ├─ Editor/          에디터 코드와 asmdef
   └─ Tests/Editor/    EditMode 테스트
```

- 네임스페이스는 `JeonLib.<도구>`, asmdef는 `JeonLib.<도구>.Editor` 형식을 씁니다.
- 버전 태그는 `<도구>/v<버전>` 형식입니다. 예: `scene-switcher/v1.0.0`

## 라이선스

정해지지 않았습니다.

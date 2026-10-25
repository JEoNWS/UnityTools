# Table-Editor

엑셀 등에서 익스포트한 TSV 테이블 txt를 에디터에서 보고, **기존 값만** 바로 고치는 임시 튜닝용 창입니다.
플레이 중에 고친 값을 메모리의 테이블에 바로 적용하고, 확정한 값은 엑셀에 붙여넣기 쉬운 형식으로 복사합니다.
코어는 게임 코드에 의존하지 않고, 프로젝트마다 다른 부분(폴더, 암호화, 런타임 적용)은 브리지로 연결합니다.

## 기능

- **테이블 보기**: 폴더의 txt 목록에서 파일을 고르면 첫 줄을 헤더로, 나머지를 탭으로 나눈 셀 그리드로 보여 줍니다. 행이 많아도 보이는 부분만 그립니다.
- **기존 값 수정**: 셀을 고치면 노란색으로 표시됩니다. id(첫 열) 변경, 행 추가/삭제, 탭/줄바꿈 입력은 막습니다.
- **복사**: 선택한 행, 선택한 열(헤더 제외), 수정한 행, 전체(헤더 포함)를 TSV로 복사합니다. 엑셀에서 같은 위치에 그대로 붙여넣을 수 있습니다.
- **런타임 적용**: 플레이 중 수정한 행을 메모리의 테이블에 반영합니다. "수정 즉시 적용"을 켜면 고칠 때마다, 끄면 "런타임 적용 (N)"으로 모아서 적용합니다.
- **txt 저장**: 브리지의 `encode`를 거쳐 원래 파일을 덮어씁니다. 원본에서 다시 익스포트하면 사라지므로 확정 값은 원본에 옮기세요.
- 수정한 행만 보기, 검색, 되돌리기, 도메인 리로드 뒤 수정 내용 유지

## 브리지

프로젝트의 Editor 어셈블리에 `ITableEditorBridge` 구현을 하나 두면 자동으로 찾아 씁니다(`TypeCache`, 이름이 `.Tests`로 끝나는 어셈블리는 제외).
구현이 없으면 평문 txt를 그대로 읽고 쓰는 기본 브리지를 쓰고, 창의 "폴더" 버튼으로 테이블 폴더를 고릅니다. 이때 런타임 적용은 꺼집니다.

```csharp
using System.Collections.Generic;
using JeonLib.TableEditor;

internal class MyTableBridge : ITableEditorBridge
{
    public string tableFolder => "Assets/Resources/Table";

    // 파일 내용을 TSV 평문으로 바꾼다. 실패하면 예외를 던진다.
    public string decode(string text) => MyCrypto.decrypt(text);

    public string encode(string plain) => MyCrypto.encrypt(plain);

    public bool canApply(string fileName, out string reason)
    {
        reason = UnityEngine.Application.isPlaying ? null : "플레이 중이 아닙니다.";
        return reason == null;
    }

    // cells[0]은 id. 메모리에 있는 해당 row에 값을 반영한다.
    public bool tryApplyRow(string fileName, IReadOnlyList<string> cells, out string error)
    {
        error = null;
        return MyTables.reparse(fileName, cells);
    }
}
```

런타임 적용은 row 인스턴스를 새로 만들지 말고 **기존 인스턴스에 다시 parse**하는 방식을 권장합니다. 다른 코드가 들고 있는 row 참조가 그대로 유효합니다.
단, 키/분류 컬럼 변경, 이미 다른 곳에 복사되거나 계산된 값, 새 인스턴스로 만드는 리스트 필드는 즉시 반영되지 않을 수 있습니다.

## 요구 사항

- Unity 6000.0 이상 (6000.4에서 확인했습니다)

## 설치

Package Manager에서 **+ > Install package from git URL...** 을 누르고 아래 주소를 입력합니다.

```
https://github.com/JEoNWS/UnityTools.git?path=/Packages/com.jeonlib.table-editor#table-editor/v1.0.0
```

또는 `Packages/manifest.json`의 `dependencies`에 직접 추가합니다.

```json
"com.jeonlib.table-editor": "https://github.com/JEoNWS/UnityTools.git?path=/Packages/com.jeonlib.table-editor#table-editor/v1.0.0"
```

`#` 뒤의 태그를 빼면 기본 브랜치의 최신 코드가 설치됩니다.

## 사용법

1. **Tools > Table Editor > Open** 으로 창을 엽니다.
2. 왼쪽 목록에서 테이블을 고르고 셀 값을 고칩니다.
3. 플레이 중이면 "런타임 적용"으로 게임에 반영해 확인합니다.
4. 확정한 값은 "복사"로 엑셀 원본에 옮깁니다.

## 테스트

패키지 테스트를 실행하려면 `Packages/manifest.json`에 `testables`를 추가합니다.

```json
"testables": [ "com.jeonlib.table-editor" ]
```

그다음 **Window > General > Test Runner > EditMode** 에서 `JeonLib.TableEditor.Editor.Tests`를 실행합니다. 테스트는 메모리의 문자열만 다루고, 에셋이나 파일은 건드리지 않습니다.

## 라이선스

정해지지 않았습니다.

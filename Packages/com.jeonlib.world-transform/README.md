# World-Transform

Inspector의 Transform은 부모 기준(local) 값만 보여 줍니다. 이 창은 선택한 오브젝트의 **월드 기준** Position, Rotation, Scale을 보여 주고 바로 수정할 수 있게 합니다.
UI Toolkit으로 만들었고 게임 코드에 의존하지 않습니다.

## 기능

- **월드 값 표시**: Position은 `transform.position`, Rotation은 월드 회전의 euler, Scale은 `lossyScale`을 보여 줍니다.
- **축 단위 수정**: 칸에 입력하고 Enter를 누르거나 축 라벨(X/Y/Z)을 드래그하면, 그 축만 월드 기준으로 적용합니다. Undo로 되돌릴 수 있습니다.
- **여러 오브젝트 편집**: 값이 서로 다른 축은 `—`로 표시하고, 수정한 축만 선택한 모든 오브젝트에 적용합니다. 부모와 자식을 함께 골라도 둘 다 입력한 값이 됩니다.
- **자동 갱신**: 씬에서 기즈모로 옮기거나 선택을 바꾸거나 Undo/Redo를 하면 창의 값이 따라 바뀝니다.
- **Rotation 표시 유지**: 같은 회전이라도 euler 표현은 여러 가지입니다. 입력하던 형태(예: `-30`, `100`, `370`)를 기억했다가, 기즈모로 돌리거나 Undo해도 가장 가까운 형태로 이어서 보여 줍니다. 기본 Inspector의 Rotation과 같은 방식입니다.

## Skew 경고

부모에 비균등 스케일(예: `(2, 1, 1)`)이 있고 자식이 그 축과 비스듬히 회전해 있으면, 자식은 월드에서 찌그러진(skew) 모양이 됩니다.
이 상태는 회전과 축별 스케일만으로 표현할 수 없어서, `lossyScale`은 찌그러짐 성분을 빼고 보여 주는 값입니다.
입력한 Scale은 `lossyScale` 기준으로 맞춰지지만 찌그러짐은 남아 있어 실제 모양과 다를 수 있습니다. 이런 대상이 선택되면 창에 경고가 표시됩니다.

부모 스케일이 균등하거나, 자식의 회전이 부모 축과 나란하면(0°/90°/180° 단위) 정확하게 맞습니다.

## 요구 사항

- Unity 6000.0 이상 (6000.4에서 확인했습니다)

## 설치

Package Manager에서 **+ > Install package from git URL...** 을 누르고 아래 주소를 입력합니다.

```
https://github.com/JEoNWS/UnityTools.git?path=/Packages/com.jeonlib.world-transform#world-transform/v1.0.0
```

또는 `Packages/manifest.json`의 `dependencies`에 직접 추가합니다.

```json
"com.jeonlib.world-transform": "https://github.com/JEoNWS/UnityTools.git?path=/Packages/com.jeonlib.world-transform#world-transform/v1.0.0"
```

`#` 뒤의 태그를 빼면 기본 브랜치의 최신 코드가 설치됩니다.

## 사용법

1. **Tools > World Transform > Open** 으로 창을 엽니다.
2. Hierarchy나 씬에서 오브젝트를 선택합니다.
3. Position, Rotation, Scale 칸에 월드 기준 값을 입력합니다.

## 테스트

패키지 테스트를 실행하려면 `Packages/manifest.json`에 `testables`를 추가합니다.

```json
"testables": [ "com.jeonlib.world-transform" ]
```

그다음 **Window > General > Test Runner > EditMode** 에서 `JeonLib.WorldTransform.Editor.Tests`를 실행합니다. 테스트는 임시 GameObject만 만들었다가 지우고, 에셋이나 씬은 건드리지 않습니다.

## 라이선스

정해지지 않았습니다.

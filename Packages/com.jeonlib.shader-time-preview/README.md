# Shader-Time-Preview

폭발, 충격파처럼 셰이더 안에서 `elapsed = _Time.y - _StartTime` 식으로 진행하는 이펙트는 에디트 모드에서 원하는 순간을 보기 어렵습니다.
이 창은 지정한 머티리얼을 창 안의 쿼드에 그리면서, 시작 시각을 `현재 셰이더 시간 - Elapsed`로 맞춰 넣어 **지정한 경과 시간의 모습**을 보여 줍니다.
UI Toolkit으로 만들었고 게임 코드에 의존하지 않습니다.

## 기능

- **시점 고정**: Elapsed 슬라이더로 경과 시간을 고르면 그 순간의 모습으로 멈춰 보입니다. 그 상태에서 머티리얼의 다른 값을 바꿔 가며 조정할 수 있습니다.
- **재생**: Play로 0부터 Duration까지 재생하고, Loop를 켜면 반복합니다. 슬라이더를 직접 움직이면 재생이 멈춥니다.
- **에셋 변경 없음**: 값은 `MaterialPropertyBlock`으로 그릴 때만 넘기므로 머티리얼 에셋은 바뀌지 않습니다.
- **색 지정**: 게임 코드가 런타임에 넣어 주는 색이 있으면 Color Property에 그 이름을 적어 미리보기에서도 넘길 수 있습니다.
- **배경색**: 알파 블렌드 이펙트가 잘 보이도록 미리보기 배경색을 바꿀 수 있습니다.

## 대상 셰이더 조건

셰이더가 아래처럼 `_Time.y`와 float 프로퍼티 하나로 경과 시간을 구하면 됩니다.

```hlsl
float elapsed = _Time.y - _StartTime;
```

`MaterialPropertyBlock`으로 값을 넘기기 때문에, 프로퍼티가 `UnityPerMaterial` CBUFFER 밖에 있거나(`Properties`에 선언하지 않은 uniform, 인스턴싱 프로퍼티) SRP Batcher가 켜져 있어도 동작합니다.

## 주의

- **URP 기준**입니다. 셰이더 시간은 URP가 `_Time`에 넣는 값과 같게 계산합니다. 에디트 모드는 `Time.realtimeSinceStartup`, 플레이 모드는 `Time.time`입니다.
- 미리보기는 창 안에서만 보입니다. 1x1 쿼드를 정면에서 직교 카메라로 그리므로 UV 기반 이펙트 확인에 맞습니다.

## 요구 사항

- Unity 6000.0 이상, URP

## 설치

Package Manager에서 **+ > Install package from git URL...** 을 누르고 아래 주소를 입력합니다.

```
https://github.com/JEoNWS/UnityTools.git?path=/Packages/com.jeonlib.shader-time-preview#shader-time-preview/v1.0.0
```

또는 `Packages/manifest.json`의 `dependencies`에 직접 추가합니다.

```json
"com.jeonlib.shader-time-preview": "https://github.com/JEoNWS/UnityTools.git?path=/Packages/com.jeonlib.shader-time-preview#shader-time-preview/v1.0.0"
```

`#` 뒤의 태그를 빼면 기본 브랜치의 최신 코드가 설치됩니다.

## 사용법

1. **Tools > Shader Time Preview > Open** 으로 창을 엽니다.
2. **Material**에 이펙트 머티리얼을 넣습니다.
3. **Start Time Property**에 셰이더가 시작 시각으로 쓰는 프로퍼티 이름을 입력합니다. (예: `_StartTime`)
4. 게임 코드가 색을 따로 넣어 주는 셰이더라면 **Color Property**와 **Color**를 채웁니다.
5. **Duration**을 이펙트 길이에 맞추고, **Elapsed** 슬라이더를 움직이거나 **Play**를 누릅니다.

## 테스트

패키지 테스트를 실행하려면 `Packages/manifest.json`에 `testables`를 추가합니다.

```json
"testables": [ "com.jeonlib.shader-time-preview" ]
```

그다음 **Window > General > Test Runner > EditMode** 에서 `JeonLib.ShaderTimePreview.Editor.Tests`를 실행합니다.

## 라이선스

정해지지 않았습니다.

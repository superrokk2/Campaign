# Campaign — Tactical Auto-Battle Prototype

Unity 6로 제작한 소규모 2D 탑다운 자동 전투 프로토타입입니다.  
전투 전 소대 구성과 자동 교전의 핵심 흐름을 짧게 보여 주는 포트폴리오 프로젝트입니다.

> 특정 게임의 캐릭터, 명칭, UI, 음원 또는 원본 에셋을 사용하지 않았습니다.  
> 모든 전투 표현은 Unity 기본 기능과 런타임 생성 도형으로 구성했습니다.

## 핵심 플레이 흐름

1. 메인 메뉴에서 게임 시작
2. 플레이어 3개 소대와 적 3개 소대 배치
3. 가장 가까운 적을 탐색하여 자동 이동 및 공격
4. 제한 시간 또는 한쪽 팀 전멸 시 승패 결정
5. 결과 확인 후 재시작

## 조작

| 입력 | 기능 |
|---|---|
| `START BATTLE` | 전투 시작 |
| `WASD` / 방향키 | 플레이어 소대 이동 |
| `RETRY` | 전투 재시작 |

## 기술적 특징

- **MVC 분리**
  - Model: 체력, 공격력, 쿨다운과 전투 규칙
  - View: 유닛, 체력바, HUD 및 피격 표현
  - Controller: 입력, 이동, 탐색, 공격 기능 조합
- **State 패턴**
  - `PrepareState → BattleState → ResultState`
- **작은 기능의 조합**
  - `MovementMotor`, `TargetSensor`, `AttackAction`을 독립 책임으로 분리
- **데이터 기반 설정**
  - `GamePrototypeConfig` ScriptableObject에서 밸런스와 색상 조정
- **성능 고려**
  - 단일 전투 Tick
  - 등록형 `CombatantRegistry`
  - 제곱 거리 비교
  - 값 변경 또는 제한된 주기로만 HUD 갱신
  - 반복적인 `Find`, LINQ, 전투 중 `Instantiate` 사용 배제
- **확장 가능한 경계**
  - 입력 인터페이스 주입
  - Config, Factory, Bootstrap 책임 분리
  - 향후 소대 타입, 스킬, 투사체 풀링 추가 가능

## Config, Factory, Bootstrap 책임

게임 초기화 코드를 하나의 클래스에 집중시키지 않고 설정, 생성, 실행 시점이라는 세 가지 책임으로 나눴습니다.

```text
GameScene 로드
    ↓
GamePrototypeBootstrap
    ↓ 설정 자산 로드
GamePrototypeConfig
    ↓ 설정 전달
GamePrototypeFactory
    ↓
Model · View · Controller 생성 및 연결
```

### GamePrototypeConfig — 무엇을 만들 것인가

`GamePrototypeConfig`는 전투를 구성하는 밸런스와 표현 데이터를 보관하는 `ScriptableObject`입니다.

- 팀당 소대 수
- 체력과 기본 공격력
- 이동속도, 공격 사거리, 공격 간격
- 이동 가능한 전장 범위
- 플레이어와 적의 표시 색상

외부에는 읽기 전용 프로퍼티만 제공하여 실행 중 설정이 임의로 변경되는 것을 막습니다. Inspector에서 값을 조정할 수 있으므로 밸런스를 변경하기 위해 게임 생성 코드를 수정할 필요가 없습니다. `OnValidate()`는 잘못된 음수나 0이 입력되더라도 Factory가 유효한 값을 받도록 보정합니다.

### GamePrototypeFactory — 어떻게 만들 것인가

`GamePrototypeFactory`는 Config를 전달받아 실제 런타임 게임 오브젝트를 생성하고 의존성을 연결합니다.

- 카메라, 전장과 EventSystem 구성
- `CombatantRegistry`와 입력 구현체 생성
- 플레이어 및 적 전투원 생성
- 전투원의 Model, View, Controller 연결
- HUD와 `GameFlowController` 생성

유닛 생성 과정에서는 먼저 `CombatantModel`을 만들고, `CombatantView`를 생성한 다음, `CombatantController`가 양쪽과 전투 서비스를 연결합니다. 이 생성 규칙을 Factory에 모아 두어 프리팹이나 오브젝트 풀을 도입하더라도 Bootstrap과 전투 로직을 변경하지 않도록 했습니다.

### GamePrototypeBootstrap — 언제 만들 것인가

`GamePrototypeBootstrap`은 게임 초기화가 실행될 시점을 결정하는 Composition Root입니다.

- 첫 Scene이 로드되기 전에 `sceneLoaded` 이벤트 등록
- 로드된 Scene이 `GameScene`인지 확인
- `GamePrototypeConfig` 자산 로드
- Config를 전달하여 `GamePrototypeFactory.Build()` 실행
- Retry 요청 시 `GameScene` 재로드

Bootstrap은 밸런스 수치나 유닛 생성 방법을 알지 않습니다. Scene 진입을 감지하고 최상위 의존성을 연결하는 일만 담당하므로 초기화 시점이 바뀌어도 Config와 Factory에 영향을 주지 않습니다.

| 변경 내용 | 담당 위치 |
|---|---|
| 체력, 공격력, 색상 조정 | `GamePrototypeConfig` |
| 유닛이나 HUD 생성 방식 변경 | `GamePrototypeFactory` |
| 게임 초기화 및 Scene 진입 시점 변경 | `GamePrototypeBootstrap` |

## 주요 코드

```text
Assets/Script/Game/
├─ Model/          순수 전투 데이터와 규칙
├─ View/           유닛 및 HUD 표현
├─ Controller/     전투원과 게임 흐름 제어
├─ State/          전투 단계별 상태
├─ Combat/         이동, 탐색, 공격 기능
├─ GamePrototypeConfig.cs
├─ GamePrototypeFactory.cs
└─ GamePrototypeBootstrap.cs
```

## 실행 환경

- Unity `6000.5.7f1`
- Universal Render Pipeline `17.5.0`
- Input System `1.20.0`
- 기준 해상도: `1920 × 1080`

## 실행 방법

1. 저장소를 Clone합니다.
2. Unity Hub에서 프로젝트를 Unity `6000.5.7f1`로 엽니다.
3. `Assets/Scenes/MainScene.unity`를 엽니다.
4. Play Mode를 실행합니다.

`GameScene`을 직접 실행해도 `GamePrototypeBootstrap`이 런타임 전투를 구성합니다.

## 설계 의도

초기 프로토타입에서는 기능을 큰 클래스 하나에 구현하지 않고 이동, 탐색, 공격, 체력과 상태 전이를 작은 단위로 먼저 분리했습니다. 이후 `CombatantController`와 `GameFlowController`가 해당 기능을 조합하도록 구성하여 기능 추가와 교체가 기존 코드에 미치는 영향을 줄였습니다.

`GamePrototypeFactory`는 Model, View, Controller 생성과 의존성 연결을 담당합니다. 밸런스 값은 `GamePrototypeConfig`에 격리하여 코드 수정 없이 조정할 수 있습니다.

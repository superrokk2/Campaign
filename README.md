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

## 현재 범위와 향후 개선

현재 버전은 포트폴리오용 핵심 전투 루프에 집중합니다.

- 소대별 고유 역할과 능력
- 유닛 간 겹침 방지 및 진형 유지
- 투사체와 VFX 오브젝트 풀링
- EditMode/PlayMode 자동 테스트
- Windows 독립 실행 빌드와 프로파일링 결과

위 항목은 다음 확장 단계로 남겨 두었습니다.

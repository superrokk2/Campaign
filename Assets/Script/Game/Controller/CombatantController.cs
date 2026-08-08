using Campaign.Game.Combat;
using Campaign.Game.Model;
using Campaign.Game.View;
using UnityEngine;

namespace Campaign.Game.Controller
{
    /// <summary>
    /// CombatantModel과 View를 연결하고 이동, 탐색, 공격 기능을 조합하는 MVC Controller.
    /// 자체 Update 없이 GameFlowController의 단일 Tick에서 호출되어 실행 순서가 예측 가능하다.
    /// </summary>
    public sealed class CombatantController : MonoBehaviour
    {
        // 현재 전투에 참여 중인 유닛을 팀별로 관리하고 적 탐색에 사용하는 공유 레지스트리.
        CombatantRegistry registry;

        // Transform 이동과 전투 영역 경계 제한만 담당하는 이동 기능 객체.
        MovementMotor movement;

        // 레지스트리에서 현재 유닛과 가장 가까운 생존 적을 찾는 타깃 탐색 기능 객체.
        TargetSensor sensor;

        // 공격 쿨다운 확인, 소비 및 타깃에게 피해 전달을 담당하는 공격 기능 객체.
        AttackAction attack;

        // Sprite, 체력바, 피격 및 사망 연출을 표시하는 MVC View 참조.
        CombatantView view;

        // 현재 추적하거나 공격하고 있는 적 유닛. 사망하거나 유효하지 않으면 다시 탐색한다.
        CombatantController target;

        // 플레이어의 수동 이동이 허용되는 월드 좌표 범위.
        Rect movementBounds;

        // 체력, 공격력, 이동속도, 팀 등 Unity에 의존하지 않는 전투 데이터 모델.
        public CombatantModel Model { get; private set; }

        // 외부 시스템이 Model 내부 구조를 직접 확인하지 않고 생존 여부를 조회하는 프로퍼티.
        public bool IsAlive => Model != null && !Model.Health.IsDead;

        // 타깃 탐색과 거리 계산에 사용하는 현재 월드 위치.
        public Vector3 Position => transform.position;

        public void Initialize(CombatantModel model, CombatantRegistry combatRegistry, CombatantView combatantView, Rect bounds)
        {
            Model = model;
            registry = combatRegistry;
            view = combatantView;
            movementBounds = bounds;
            movement = new MovementMotor(transform);
            sensor = new TargetSensor(registry, this);
            attack = new AttackAction(this);
            // Model 이벤트를 View 표현으로 변환하며 OnDestroy에서 반드시 해제한다.
            Model.Health.Changed += view.SetHealth;
            Model.Health.Died += Die;
            registry.Register(this);
            view.SetHealth(Model.Health.Current, Model.Health.Max);
        }

        public void BattleTick(float deltaTime, Vector2 playerInput)
        {
            if (!IsAlive) return;
            Model.AttackCooldown.Tick(deltaTime);
            view.TickVisual(deltaTime);
            // 타깃이 사라진 경우에만 재탐색해 전체 목록 순회 횟수를 줄인다.
            if (target == null || !target.IsAlive) target = sensor.Acquire();
            if (Model.Team == Team.Player && playerInput.sqrMagnitude > 0.01f)
                movement.Move(playerInput, Model.MoveSpeed, deltaTime, movementBounds);
            else if (target != null && (target.Position - Position).sqrMagnitude > Model.AttackRange * Model.AttackRange)
                movement.MoveTowards(target.Position, Model.MoveSpeed, deltaTime);
            if (target != null && (target.Position - Position).sqrMagnitude <= Model.AttackRange * Model.AttackRange)
                attack.TryAttack(target);
        }

        public void ReceiveDamage(int amount)
        {
            if (Model.Health.ApplyDamage(amount)) view.FlashHit();
        }

        void Die()
        {
            // 검색 대상에서 먼저 제거해 다른 유닛이 죽은 대상을 다시 선택하지 않게 한다.
            registry.Unregister(this);
            view.ShowDeath();
        }

        void OnDestroy()
        {
            // 씬 재시작 후 파괴된 객체가 delegate와 레지스트리에 남지 않게 정리한다.
            if (Model == null) return;
            Model.Health.Changed -= view.SetHealth;
            Model.Health.Died -= Die;
            if (IsAlive) registry.Unregister(this);
        }
    }
}

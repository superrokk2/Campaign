using Campaign.Game.Controller;
using UnityEngine;

namespace Campaign.Game.Combat
{
    /// <summary>Transform 위치 변경만 담당하며 입력 해석과 타깃 판단은 알지 못한다.</summary>
    public sealed class MovementMotor
    {
        readonly Transform transform;
        public MovementMotor(Transform transform) => this.transform = transform;

        public void MoveTowards(Vector3 destination, float speed, float deltaTime)
        {
            transform.position = Vector3.MoveTowards(transform.position, destination, speed * deltaTime);
        }

        public void Move(Vector2 direction, float speed, float deltaTime, Rect bounds)
        {
            // 대각선 이동이 축 이동보다 빨라지지 않도록 먼저 정규화한다.
            var next = transform.position + (Vector3)(direction.normalized * speed * deltaTime);
            next.x = Mathf.Clamp(next.x, bounds.xMin, bounds.xMax);
            next.y = Mathf.Clamp(next.y, bounds.yMin, bounds.yMax);
            transform.position = next;
        }
    }

    /// <summary>타깃 획득 책임을 레지스트리에 위임하는 작은 전투 기능.</summary>
    public sealed class TargetSensor
    {
        readonly CombatantRegistry registry;
        readonly CombatantController owner;
        public TargetSensor(CombatantRegistry registry, CombatantController owner)
        {
            this.registry = registry;
            this.owner = owner;
        }
        public CombatantController Acquire() => registry.FindClosestEnemy(owner.Model.Team, owner.Position);
    }

    /// <summary>
    /// 공격 가능 여부, 쿨다운 소비, 피해 전달만 담당한다.
    /// 애니메이션과 피격 표현은 Controller와 View 경계에서 처리한다.
    /// </summary>
    public sealed class AttackAction
    {
        readonly CombatantController owner;
        public AttackAction(CombatantController owner) => this.owner = owner;

        public bool TryAttack(CombatantController target)
        {
            if (target == null || !target.IsAlive || !owner.Model.AttackCooldown.TryConsume()) return false;
            target.ReceiveDamage(owner.Model.Damage);
            return true;
        }
    }
}

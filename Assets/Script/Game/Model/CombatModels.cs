using System;

namespace Campaign.Game.Model
{
    // Model 계층은 UnityEngine에 의존하지 않는다. 따라서 전투 규칙을 씬이나
    // MonoBehaviour 없이 단위 테스트할 수 있고, View 교체에도 영향을 받지 않는다.
    public enum Team { Player, Enemy }
    public enum BattlePhase { Prepare, Battle, Result }

    /// <summary>
    /// 체력의 유효 범위와 사망 전이를 소유하는 순수 C# 모델.
    /// Controller는 수치를 직접 변경하지 않고 반드시 이 API를 통해 피해를 적용한다.
    /// </summary>
    public sealed class HealthModel
    {
        public int Max { get; }
        public int Current { get; private set; }
        public bool IsDead => Current <= 0;
        public event Action<int, int> Changed;
        public event Action Died;

        public HealthModel(int maximum)
        {
            // 잘못된 데이터가 들어와도 체력이 0인 상태로 생성되지 않도록 방어한다.
            Max = Math.Max(1, maximum);
            Current = Max;
        }

        public bool ApplyDamage(int amount)
        {
            // 사망 후 중복 보상이나 연출이 발생하지 않도록 추가 피해를 무시한다.
            if (IsDead || amount <= 0) return false;
            Current = Math.Max(0, Current - amount);
            Changed?.Invoke(Current, Max);
            if (IsDead) Died?.Invoke();
            return true;
        }
    }

    /// <summary>시간 누적과 사용 가능 여부만 담당하는 재사용 가능한 쿨다운 모델.</summary>
    public sealed class CooldownModel
    {
        readonly float duration;
        float remaining;
        public bool IsReady => remaining <= 0f;

        public CooldownModel(float durationSeconds) => duration = Math.Max(0.01f, durationSeconds);
        public void Tick(float deltaTime) => remaining = Math.Max(0f, remaining - deltaTime);
        public bool TryConsume()
        {
            if (!IsReady) return false;
            remaining = duration;
            return true;
        }
    }

    /// <summary>전투원 한 명의 런타임 규칙 데이터. 이동과 렌더링 구현은 포함하지 않는다.</summary>
    public sealed class CombatantModel
    {
        public Team Team { get; }
        public HealthModel Health { get; }
        public CooldownModel AttackCooldown { get; }
        public int Damage { get; }
        public float MoveSpeed { get; }
        public float AttackRange { get; }

        public CombatantModel(Team team, int health, int damage, float moveSpeed, float attackRange, float attackInterval)
        {
            Team = team;
            Health = new HealthModel(health);
            Damage = Math.Max(1, damage);
            MoveSpeed = Math.Max(0f, moveSpeed);
            AttackRange = Math.Max(0.1f, attackRange);
            AttackCooldown = new CooldownModel(attackInterval);
        }
    }
}

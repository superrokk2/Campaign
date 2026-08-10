using System.Collections.Generic;
using Campaign.Game.Controller;
using Campaign.Game.Model;
using UnityEngine;

namespace Campaign.Game.Combat
{
    /// <summary>
    /// 활성 전투원을 팀별로 보관하는 레지스트리입니다.
    /// Find 계열 API 대신 캐시된 목록을 순회하여 검색 비용과 GC를 줄입니다.
    /// 현재 소규모 전투에서는 선형 탐색이 가장 단순하고 충분히 빠릅니다.
    /// </summary>
    public sealed class CombatantRegistry
    {
        readonly List<CombatantController> player = new(3);
        readonly List<CombatantController> enemy = new(3);

        public IReadOnlyList<CombatantController> Player => player;
        public IReadOnlyList<CombatantController> Enemy => enemy;
        public int LivingPlayerCount => CountLiving(player);
        public int LivingEnemyCount => CountLiving(enemy);

        public void Register(CombatantController unit)
        {
            // Initialize가 실수로 중복 호출돼도 생존 수가 오염되지 않게 합니다.
            var list = ListFor(unit.Model.Team);
            if (!list.Contains(unit)) list.Add(unit);
        }
        public void Unregister(CombatantController unit) => ListFor(unit.Model.Team).Remove(unit);

        public CombatantController FindClosestEnemy(Team team, Vector3 origin)
        {
            var candidates = team == Team.Player ? enemy : player;
            CombatantController closest = null;
            var closestSqr = float.MaxValue;
            for (var i = 0; i < candidates.Count; i++)
            {
                var candidate = candidates[i];
                if (!candidate.IsAlive) continue;
                
                // 실제 거리는 필요하지 않으므로 sqrt가 없는 제곱 거리로 비교합니다.
                var sqr = (candidate.Position - origin).sqrMagnitude;
                if (sqr >= closestSqr) continue;
                closestSqr = sqr;
                closest = candidate;
            }
            return closest;
        }

        List<CombatantController> ListFor(Team team) => team == Team.Player ? player : enemy;
        static int CountLiving(List<CombatantController> list)
        {
            var count = 0;
            for (var i = 0; i < list.Count; i++) if (list[i].IsAlive) count++;
            return count;
        }
    }
}

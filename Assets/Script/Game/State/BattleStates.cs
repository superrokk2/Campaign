using Campaign.Game.Controller;
using Campaign.Game.Model;

namespace Campaign.Game.State
{
    /// <summary>모든 전투 단계가 따르는 공통 생명주기 계약.</summary>
    public interface IBattleState
    {
        BattlePhase Phase { get; }
        void Enter();
        void Tick(float deltaTime);
        void Exit();
    }

    /// <summary>전투를 멈춘 채 시작 입력을 기다리고 준비 HUD를 표시한다.</summary>
    public sealed class PrepareState : IBattleState
    {
        readonly GameFlowController flow;
        public BattlePhase Phase => BattlePhase.Prepare;
        public PrepareState(GameFlowController flow) => this.flow = flow;
        public void Enter() => flow.ShowPrepare();
        public void Tick(float deltaTime) { }
        public void Exit() { }
    }

    /// <summary>
    /// 경과 시간, HUD 갱신 주기, 종료 조건과 승패 판정을 소유하는 핵심 State.
    /// 이 책임을 Flow에서 분리해 새 상태를 추가할 때 Controller가 비대해지는 것을 막는다.
    /// </summary>
    public sealed class BattleState : IBattleState
    {
        readonly GameFlowController flow;
        float elapsed;
        float hudRefreshTimer;
        int lastPlayers;
        int lastEnemies;
        public BattlePhase Phase => BattlePhase.Battle;
        public BattleState(GameFlowController flow) => this.flow = flow;
        public void Enter()
        {
            elapsed = 0f;
            hudRefreshTimer = 0f;
            lastPlayers = flow.LivingPlayers;
            lastEnemies = flow.LivingEnemies;
            flow.BeginBattle();
        }
        public void Tick(float deltaTime)
        {
            elapsed += deltaTime;
            hudRefreshTimer -= deltaTime;

            // 모든 유닛의 Tick을 한 번에 호출해 실행 순서를 예측 가능하게 한다.
            flow.TickCombat(deltaTime);

            // 문자열 생성과 Canvas rebuild를 매 프레임 하지 않도록 주기 또는 인원 변화 시에만 갱신한다.
            var countsChanged = lastPlayers != flow.LivingPlayers || lastEnemies != flow.LivingEnemies;
            if (countsChanged || hudRefreshTimer <= 0f)
            {
                lastPlayers = flow.LivingPlayers;
                lastEnemies = flow.LivingEnemies;
                hudRefreshTimer = 0.2f;
                flow.RenderBattleHud(GameFlowController.BattleLimit - elapsed);
            }

            // 양 팀이 생존하고 제한 시간이 남아 있으면 현재 State를 유지한다.
            if (flow.LivingEnemies > 0 && flow.LivingPlayers > 0 && elapsed < GameFlowController.BattleLimit) return;
            var playerWon = flow.LivingEnemies == 0 ||
                            (elapsed >= GameFlowController.BattleLimit && flow.LivingPlayers > flow.LivingEnemies);
            flow.FinishBattle(playerWon);
        }
        public void Exit() { }
    }

    /// <summary>전투 Tick을 중단하고 결과와 재시작 입력만 노출한다.</summary>
    public sealed class ResultState : IBattleState
    {
        readonly GameFlowController flow;
        public BattlePhase Phase => BattlePhase.Result;
        public ResultState(GameFlowController flow) => this.flow = flow;
        public void Enter() => flow.ShowResult();
        public void Tick(float deltaTime) { }
        public void Exit() { }
    }
}

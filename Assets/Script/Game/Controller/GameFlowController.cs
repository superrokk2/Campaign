using System.Collections.Generic;
using Campaign.Game.Combat;
using Campaign.Game.Model;
using Campaign.Game.State;
using Campaign.Game.View;
using UnityEngine;

namespace Campaign.Game.Controller
{
    /// <summary>
    /// 전투의 MVC Controller이자 State들의 Context입니다.
    /// 상태별 시간과 전이 규칙은 State가 소유하고, 이 클래스는 유닛 Tick과 View API를 제공합니다.
    /// </summary>
    public sealed class GameFlowController : MonoBehaviour
    {
        readonly List<CombatantController> units = new(6);
        CombatantRegistry registry;
        GameHudView hud;
        IBattleState currentState;
        IPlayerInputSource inputSource;
        bool playerWon;
        public const float BattleLimit = 77f;
        public int LivingPlayers => registry.LivingPlayerCount;
        public int LivingEnemies => registry.LivingEnemyCount;

        public void Initialize(CombatantRegistry combatRegistry, GameHudView hudView,
            IReadOnlyList<CombatantController> combatants, IPlayerInputSource playerInputSource)
        {
            registry = combatRegistry;
            hud = hudView;
            inputSource = playerInputSource;
            for (var i = 0; i < combatants.Count; i++) units.Add(combatants[i]);
            // HUD 액션 이벤트를 구독한 뒤 최초 상태를 활성화할 준비를 합니다.
            hud.ActionPressed += HandleAction;
            // 모든 의존성 연결이 끝난 뒤 최초 상태에 진입합니다.
            ChangeState(new PrepareState(this));
        }

        // 전투 전체의 Update 진입점을 하나로 유지해 상태와 유닛 실행 순서를 통제합니다.
        void Update() => currentState?.Tick(Time.deltaTime);

        public void ShowPrepare()
        {
            Time.timeScale = 1f;
            hud.Render("PREPARE", "3 squads vs 3 squads", "START BATTLE", true);
        }

        public void BeginBattle()
        {
            RenderBattleHud(BattleLimit);
        }

        public void TickCombat(float deltaTime)
        {
            // 입력을 인터페이스 뒤에 숨겨 자동 테스트와 플랫폼 확장을 쉽게 합니다.
            var input = inputSource.ReadMovement();
            for (var i = 0; i < units.Count; i++) units[i].BattleTick(deltaTime, input);
        }

        public void RenderBattleHud(float remaining) =>
            hud.Render("BATTLE", BuildStatus(remaining), string.Empty, false);

        public void FinishBattle(bool didPlayerWin)
        {
            playerWon = didPlayerWin;
            ChangeState(new ResultState(this));
        }

        public void ShowResult()
        {
            hud.Render(playerWon ? "VICTORY" : "DEFEAT", BuildStatus(0f), "RETRY", true);
        }

        void HandleAction()
        {
            if (currentState.Phase == BattlePhase.Prepare) ChangeState(new BattleState(this));
            else if (currentState.Phase == BattlePhase.Result) GamePrototypeBootstrap.Rebuild();
        }

        void ChangeState(IBattleState next)
        {
            // Exit -> 교체 -> Enter 순서를 보장해 상태별 자원 정리 시점을 명확히 합니다.
            currentState?.Exit();
            currentState = next;
            currentState.Enter();
        }

        string BuildStatus(float remaining) =>
            $"ALLIES {LivingPlayers}  |  ENEMIES {LivingEnemies}  |  {Mathf.Max(0f, remaining):0}s";

        void OnDestroy()
        {
            if (hud != null) hud.ActionPressed -= HandleAction;
        }
    }
}

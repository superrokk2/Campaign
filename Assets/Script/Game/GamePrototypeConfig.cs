using UnityEngine;

namespace Campaign.Game
{
    /// <summary>
    /// 프로토타입의 밸런스와 표현 값을 보관하는 ScriptableObject 설정 자산입니다.
    /// 코드 재컴파일 없이 Inspector에서 수치를 조정하고 여러 설정 프리셋을 만들 수 있습니다.
    /// </summary>
    [CreateAssetMenu(
        fileName = "GamePrototypeConfig",
        menuName = "Campaign/Game Prototype Config",
        order = 0)]
    public sealed class GamePrototypeConfig : ScriptableObject
    {
        public const string ResourcesPath = "GamePrototypeConfig";

        [Header("Squad Composition")]
        [SerializeField, Min(1)] int squadsPerTeam = 3;

        [Header("Combat Balance")]
        [SerializeField, Min(1)] int health = 100;
        [SerializeField, Min(1)] int baseDamage = 14;
        [SerializeField, Min(0f)] float baseMoveSpeed = 1.8f;
        [SerializeField, Min(0.1f)] float attackRange = 1.25f;
        [SerializeField, Min(0.01f)] float baseAttackInterval = 0.75f;

        [Header("Arena")]
        [SerializeField] Rect movementBounds = new(-7.6f, -3.8f, 15.2f, 7.6f);

        [Header("Presentation")]
        [SerializeField] Color playerColor = new(0.15f, 0.75f, 0.95f);
        [SerializeField] Color enemyColor = new(0.95f, 0.28f, 0.3f);

        // 외부 시스템에는 읽기 전용 프로퍼티만 공개해 런타임 중 설정 변조를 막습니다.
        public int SquadsPerTeam => squadsPerTeam;
        public int Health => health;
        public int BaseDamage => baseDamage;
        public float BaseMoveSpeed => baseMoveSpeed;
        public float AttackRange => attackRange;
        public float BaseAttackInterval => baseAttackInterval;
        public Rect MovementBounds => movementBounds;
        public Color PlayerColor => playerColor;
        public Color EnemyColor => enemyColor;

        /// <summary>
        /// Resources/GamePrototypeConfig.asset을 로드합니다.
        /// 자산을 아직 만들지 않은 초기 상태에서는 기본 직렬화 값을 가진 임시 인스턴스를 반환합니다.
        /// </summary>
        public static GamePrototypeConfig LoadOrCreateDefault()
        {
            var asset = Resources.Load<GamePrototypeConfig>(ResourcesPath);
            if (asset != null) return asset;

            var fallback = CreateInstance<GamePrototypeConfig>();
            fallback.name = "RuntimeDefaultGamePrototypeConfig";
            fallback.hideFlags = HideFlags.HideAndDontSave;
            Debug.LogWarning(
                "Resources/GamePrototypeConfig 에 GamePrototypeConfig 자산이 없습니다. " +
                "런타임 기본값을 사용합니다. Assets > Create > Campaign > Game Prototype Config를 통해 생성해주세요.");
            return fallback;
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            // YAML을 직접 편집하거나 다중 선택을 사용할 때도 GamePrototypeFactory가 항상 유효한 값을 받게 합니다.
            squadsPerTeam = Mathf.Max(1, squadsPerTeam);
            health = Mathf.Max(1, health);
            baseDamage = Mathf.Max(1, baseDamage);
            baseMoveSpeed = Mathf.Max(0f, baseMoveSpeed);
            attackRange = Mathf.Max(0.1f, attackRange);
            baseAttackInterval = Mathf.Max(0.01f, baseAttackInterval);
            movementBounds.width = Mathf.Max(0.1f, movementBounds.width);
            movementBounds.height = Mathf.Max(0.1f, movementBounds.height);
        }
#endif
    }
}

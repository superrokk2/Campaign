using System.Collections.Generic;
using Campaign.Game.Combat;
using Campaign.Game.Controller;
using Campaign.Game.Model;
using Campaign.Game.View;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;

namespace Campaign.Game
{
    /// <summary>
    /// 런타임 전투 월드의 Model, View, Controller를 생성하고 의존성을 연결하는 Factory입니다.
    /// Bootstrap은 씬 감지만 담당하고 모든 조립 규칙은 이곳에 모아 변경 지점을 제한합니다.
    /// </summary>
    public sealed class GamePrototypeFactory
    {
        const string RootName = "RuntimeCombatPrototype";
        readonly GamePrototypeConfig config;
        static Sprite squareSprite;

        public GamePrototypeFactory(GamePrototypeConfig config) => this.config = config;

        public void Build(Scene scene)
        {
            var roots = scene.GetRootGameObjects();

            // 같은 Scene에서 Build가 중복 호출돼도 런타임 루트를 다시 생성하지 않습니다.
            for (var i = 0; i < roots.Length; i++) if (roots[i].name == RootName) return;

            // 불필요한 플레이스홀더 UI가 있으면 비활성화합니다.
            DisableKnownPlaceholder(roots);

            // 런타임에 생성한 전투 오브젝트 전체를 정확한 GameScene에 소속시키고, GameScene의 로드·언로드 생명주기를 따르게 합니다.
            var root = new GameObject(RootName);
            SceneManager.MoveGameObjectToScene(root, scene);

            // 카메라와 Arena를 생성하고 EventSystem을 재사용하거나 생성합니다.
            ConfigureCamera(roots, root.transform);
            CreateArena(root.transform);
            GetOrCreateEventSystem(roots, root.transform);

            // 전투 유닛을 생성하고 레지스트리에 등록합니다. CombatantController가 Model과 View를 연결합니다.
            var registry = new CombatantRegistry();
            var units = new List<CombatantController>(config.SquadsPerTeam * 2);
            for (var i = 0; i < config.SquadsPerTeam; i++)
            {
                units.Add(CreateUnit(root.transform, registry, Team.Player, new Vector3(-5.3f, (i - 1) * 2f), i));
                units.Add(CreateUnit(root.transform, registry, Team.Enemy, new Vector3(5.3f, (i - 1) * 2f), i));
            }

            // 전투 HUD를 생성합니다.
            var hudObject = new GameObject("CombatHUD");
            hudObject.transform.SetParent(root.transform, false);
            var hud = hudObject.AddComponent<GameHudView>();
            hud.Initialize();

            // GameFlowController를 생성하고 GameHudView와 CombatantRegistry를 연결합니다.
            root.AddComponent<GameFlowController>().Initialize(registry, hud, units, new UnityInputSystemSource());
        }

        static void DisableKnownPlaceholder(GameObject[] roots)
        {
            for (var i = 0; i < roots.Length; i++)
            {
                var candidate = roots[i];
                // 이름만으로 판정하지 않고 현재 플레이스홀더의 고유 자식 구조까지 확인합니다.
                if (candidate.GetComponent<Canvas>() == null) continue;
                if (candidate.transform.Find("Background") != null && candidate.transform.Find("TopBand") != null &&
                    candidate.transform.Find("Title") != null && candidate.transform.Find("Message") != null)
                {
                    candidate.SetActive(false);
                    return;
                }
            }
        }

        CombatantController CreateUnit(Transform parent, CombatantRegistry registry, Team team, Vector3 position, int index)
        {
            var go = new GameObject($"{team}Squad_{index + 1}");
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            go.transform.localScale = new Vector3(0.85f, 0.85f, 1f);

            // 1. Unity에 의존하지 않는 전투 규칙과 수치를 Model로 생성합니다.
            // 부대별 차이를 주기 위해 index로 공격력, 이동 속도, 공격 간격을 약간씩 조정합니다.
            var model = new CombatantModel(
                team,
                config.Health,
                config.BaseDamage + index * 2,
                config.BaseMoveSpeed + index * 0.12f,
                config.AttackRange,
                config.BaseAttackInterval + index * 0.08f);

            // 2. 유닛의 Sprite와 체력바를 표시할 View를 생성합니다.
            var view = go.AddComponent<CombatantView>();
            var color = team == Team.Player ? config.PlayerColor : config.EnemyColor;
            view.Initialize(GetSquareSprite(), color);

            // 3. Controller가 Model, View와 전투 서비스를 연결합니다.
            var controller = go.AddComponent<CombatantController>();
            controller.Initialize(model, registry, view, config.MovementBounds);

            return controller;
        }

        static void ConfigureCamera(GameObject[] roots, Transform parent)
        {
            Camera camera = null;
            for (var i = 0; i < roots.Length && camera == null; i++) camera = roots[i].GetComponentInChildren<Camera>(true);
            if (camera == null)
            {
                var go = new GameObject("Main Camera");
                go.transform.SetParent(parent, false); go.tag = "MainCamera";
                camera = go.AddComponent<Camera>(); go.AddComponent<AudioListener>();
            }
            camera.gameObject.SetActive(true); camera.orthographic = true; camera.orthographicSize = 5.4f;
            camera.transform.position = new Vector3(0f, 0f, -10f);
            camera.backgroundColor = new Color(0.025f, 0.075f, 0.11f); camera.clearFlags = CameraClearFlags.SolidColor;
        }

        static void CreateArena(Transform parent)
        {
            CreateSpriteObject("Arena", parent, new Vector3(16f, 8.4f, 1f),
                new Color(0.055f, 0.16f, 0.19f), -10);
            CreateSpriteObject("CenterLine", parent, new Vector3(0.08f, 8f, 1f),
                new Color(0.85f, 0.68f, 0.25f, 0.55f), -5);
        }

        static void CreateSpriteObject(string name, Transform parent, Vector3 scale, Color color, int order)
        {
            var go = new GameObject(name); go.transform.SetParent(parent, false); go.transform.localScale = scale;
            var renderer = go.AddComponent<SpriteRenderer>(); renderer.sprite = GetSquareSprite();
            renderer.color = color; renderer.sortingOrder = order;
        }

        /// <summary>
        /// Scene에 저장된 EventSystem이 있으면 재사용하고, 없을 때만 런타임 인스턴스를 생성합니다.
        /// 기존 시스템을 재사용하므로 한 Scene에 활성 EventSystem이 두 개 생기지 않습니다.
        /// </summary>
        static EventSystem GetOrCreateEventSystem(GameObject[] roots, Transform parent)
        {
            // Scene에 저장된 EventSystem이 있으면 찾아서 재사용합니다.
            for (var i = 0; i < roots.Length; i++)
            {
                var existing = roots[i].GetComponent<EventSystem>();
                if (existing == null) continue;

                // 기존 EventSystem이 StandaloneInputModule을 사용하면 비활성화하고 InputSystemUIInputModule을 추가합니다.
                var legacyInput = existing.GetComponent<StandaloneInputModule>();
                if (legacyInput != null) legacyInput.enabled = false;
                if (existing.GetComponent<InputSystemUIInputModule>() == null)
                    existing.gameObject.AddComponent<InputSystemUIInputModule>();

                existing.gameObject.SetActive(true);
                return existing;
            }

            // EventSystem이 없으면 런타임에 새로 생성합니다.
            var go = new GameObject("RuntimeEventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            go.transform.SetParent(parent, false);
            return go.GetComponent<EventSystem>();
        }

        static Sprite GetSquareSprite()
        {
            // 모든 단순 도형이 공유하는 1x1 Sprite를 한 번만 생성합니다.
            if (squareSprite != null) return squareSprite;
            var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false)
            { name = "RuntimeWhiteTexture", filterMode = FilterMode.Point, hideFlags = HideFlags.HideAndDontSave };
            texture.SetPixel(0, 0, Color.white); texture.Apply();
            squareSprite = Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
            squareSprite.name = "RuntimeSquareSprite"; squareSprite.hideFlags = HideFlags.HideAndDontSave;
            return squareSprite;
        }
    }
}

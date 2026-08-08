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
    /// 런타임 전투 월드의 View와 Controller를 생성하고 의존성을 연결하는 Factory.
    /// Bootstrap은 씬 감지만 담당하고 모든 조립 규칙은 이곳에 모아 변경 지점을 제한한다.
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
            for (var i = 0; i < roots.Length; i++) if (roots[i].name == RootName) return;

            // 저장된 사용자 씬을 수정하지 않고 알려진 플레이스홀더만 런타임에 숨긴다.
            DisableKnownPlaceholder(roots);
            var root = new GameObject(RootName);
            SceneManager.MoveGameObjectToScene(root, scene);
            ConfigureCamera(roots, root.transform);
            CreateArena(root.transform);
            CreateEventSystem(root.transform);

            var registry = new CombatantRegistry();
            var units = new List<CombatantController>(config.SquadsPerTeam * 2);
            for (var i = 0; i < config.SquadsPerTeam; i++)
            {
                units.Add(CreateUnit(root.transform, registry, Team.Player, new Vector3(-5.3f, (i - 1) * 2f), i));
                units.Add(CreateUnit(root.transform, registry, Team.Enemy, new Vector3(5.3f, (i - 1) * 2f), i));
            }

            var hudObject = new GameObject("CombatHUD");
            hudObject.transform.SetParent(root.transform, false);
            var hud = hudObject.AddComponent<GameHudView>();
            hud.Initialize();
            root.AddComponent<GameFlowController>().Initialize(registry, hud, units, new UnityInputSystemSource());
        }

        static void DisableKnownPlaceholder(GameObject[] roots)
        {
            GameObject placeholderCanvas = null;
            for (var i = 0; i < roots.Length; i++)
            {
                var candidate = roots[i];
                // 이름만으로 판정하지 않고 현재 플레이스홀더의 고유 자식 구조까지 확인한다.
                if (candidate.transform.parent != null || candidate.GetComponent<Canvas>() == null) continue;
                if (candidate.transform.Find("Background") != null && candidate.transform.Find("TopBand") != null &&
                    candidate.transform.Find("Title") != null && candidate.transform.Find("Message") != null)
                {
                    placeholderCanvas = candidate;
                    candidate.SetActive(false);
                    break;
                }
            }
            if (placeholderCanvas == null) return;
            for (var i = 0; i < roots.Length; i++)
            {
                var eventSystem = roots[i].GetComponent<EventSystem>();
                if (eventSystem != null && roots[i].transform.parent == null) roots[i].SetActive(false);
            }
        }

        CombatantController CreateUnit(Transform parent, CombatantRegistry registry, Team team, Vector3 position, int index)
        {
            var go = new GameObject($"{team}Squad_{index + 1}");
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            go.transform.localScale = new Vector3(0.85f, 0.85f, 1f);

            // 1. Unity에 의존하지 않는 전투 규칙과 수치를 Model로 생성한다.
            // 부대 별 차이를 주기 위해 index를 이용해 체력, 공격력, 이동속도, 공격 간격을 약간씩 조정한다.
            var model = new CombatantModel(
                team,
                config.Health,
                config.BaseDamage + index * 2,
                config.BaseMoveSpeed + index * 0.12f,
                config.AttackRange,
                config.BaseAttackInterval + index * 0.08f);

            // 2. 유닛의 Sprite와 체력바를 표시할 View를 생성한다.
            var view = go.AddComponent<CombatantView>();
            var color = team == Team.Player ? config.PlayerColor : config.EnemyColor;
            view.Initialize(GetSquareSprite(), color);

            // 3. Controller가 Model, View와 전투 서비스를 연결한다.
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

        static void CreateEventSystem(Transform parent)
        {
            var go = new GameObject("RuntimeEventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            go.transform.SetParent(parent, false);
        }

        static Sprite GetSquareSprite()
        {
            // 모든 단순 도형이 공유하는 1x1 Sprite를 한 번만 생성한다.
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

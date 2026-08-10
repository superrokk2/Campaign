using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[InitializeOnLoad]
public static class MainMenuSceneBuilder
{
    const string BuildKey = "Campaign.MainMenuSceneBuilder.Completed.v1";
    const string MainScenePath = "Assets/Scenes/MainScene.unity";
    const string GameScenePath = "Assets/Scenes/GameScene.unity";
    const string SampleScenePath = "Assets/Scenes/SampleScene.unity";

    static readonly Color Navy = Hex("071923");
    static readonly Color Teal = Hex("0E4750");
    static readonly Color Gold = Hex("E9B949");
    static readonly Color Cream = Hex("F5F1E8");

    static MainMenuSceneBuilder()
    {
        EditorApplication.delayCall += TryBuild;
    }

    static void TryBuild()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorApplication.delayCall += TryBuild;
            return;
        }

        UpgradeInputModules();

        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(MainScenePath) != null &&
            AssetDatabase.LoadAssetAtPath<SceneAsset>(GameScenePath) != null &&
            AssetDatabase.LoadAssetAtPath<SceneAsset>(SampleScenePath) == null)
        {
            SessionState.SetBool(BuildKey, true);
            return;
        }

        if (SessionState.GetBool(BuildKey, false) || EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            if (!SessionState.GetBool(BuildKey, false))
                EditorApplication.delayCall += TryBuild;
            return;
        }

        SessionState.SetBool(BuildKey, true);
        BuildMainScene();
        BuildGameScene();
        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(MainScenePath, true),
            new EditorBuildSettingsScene(GameScenePath, true)
        };

        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(SampleScenePath) != null)
            AssetDatabase.DeleteAsset(SampleScenePath);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Single);
        Debug.Log("[Campaign] MainScene and GameScene created; START GAME loads GameScene.");
    }

    static void BuildMainScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = "MainScene";
        CreateCamera(Navy);

        var canvas = CreateCanvas();
        CreatePanel(canvas.transform, "Background", Navy, Vector2.zero, Vector2.one);

        var glow = CreatePanel(canvas.transform, "AccentGlow", Teal, new Vector2(0.08f, 0.18f), new Vector2(0.92f, 0.82f));
        glow.GetComponent<Image>().color = new Color(Teal.r, Teal.g, Teal.b, 0.45f);

        CreateText(canvas.transform, "Eyebrow", "TACTICAL COMMAND", 24, Gold,
            new Vector2(0.25f, 0.69f), new Vector2(0.75f, 0.75f), FontStyle.Bold);
        CreateText(canvas.transform, "Title", "CAMPAIGN", 88, Cream,
            new Vector2(0.18f, 0.49f), new Vector2(0.82f, 0.68f), FontStyle.Bold);
        CreateText(canvas.transform, "Subtitle", "PREPARE  •  LEAD  •  PREVAIL", 28, new Color(0.76f, 0.84f, 0.84f),
            new Vector2(0.2f, 0.42f), new Vector2(0.8f, 0.49f), FontStyle.Normal);

        var controllerObject = new GameObject("MainMenuController");
        var controller = controllerObject.AddComponent<MainMenuController>();
        var button = CreateButton(canvas.transform);
        UnityEventTools.AddPersistentListener(button.onClick, controller.LoadGameScene);

        CreateText(canvas.transform, "Footer", "A STORY OF STRATEGY AND CONSEQUENCE", 18,
            new Color(0.55f, 0.66f, 0.67f), new Vector2(0.2f, 0.08f), new Vector2(0.8f, 0.14f), FontStyle.Normal);
        CreateEventSystem();
        EditorSceneManager.SaveScene(scene, MainScenePath);
    }

    static void BuildGameScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = "GameScene";
        CreateCamera(Hex("091D24"));
        var canvas = CreateCanvas();
        CreatePanel(canvas.transform, "Background", Hex("091D24"), Vector2.zero, Vector2.one);
        CreatePanel(canvas.transform, "TopBand", Teal, new Vector2(0f, 0.82f), Vector2.one);
        CreateText(canvas.transform, "Title", "GAME SCENE", 72, Cream,
            new Vector2(0.15f, 0.48f), new Vector2(0.85f, 0.64f), FontStyle.Bold);
        CreateText(canvas.transform, "Message", "The campaign begins here.", 30, Gold,
            new Vector2(0.2f, 0.39f), new Vector2(0.8f, 0.48f), FontStyle.Normal);
        CreateEventSystem();
        EditorSceneManager.SaveScene(scene, GameScenePath);
    }

    static void CreateCamera(Color background)
    {
        var cameraObject = new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";
        var camera = cameraObject.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = background;
        camera.orthographic = true;
        cameraObject.AddComponent<AudioListener>();
    }

    static Canvas CreateCanvas()
    {
        var canvasObject = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        return canvas;
    }

    static GameObject CreatePanel(Transform parent, string name, Color color, Vector2 min, Vector2 max)
    {
        var panel = new GameObject(name, typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(parent, false);
        var rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        panel.GetComponent<Image>().color = color;
        return panel;
    }

    static Text CreateText(Transform parent, string name, string value, int size, Color color,
        Vector2 min, Vector2 max, FontStyle style)
    {
        var textObject = new GameObject(name, typeof(RectTransform), typeof(Text));
        textObject.transform.SetParent(parent, false);
        var rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        var text = textObject.GetComponent<Text>();
        text.text = value;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = size;
        text.fontStyle = style;
        text.color = color;
        text.alignment = TextAnchor.MiddleCenter;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        return text;
    }

    static Button CreateButton(Transform parent)
    {
        var buttonObject = new GameObject("StartGameButton", typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);
        var rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.36f, 0.23f);
        rect.anchorMax = new Vector2(0.64f, 0.35f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        var image = buttonObject.GetComponent<Image>();
        image.color = Gold;
        var button = buttonObject.GetComponent<Button>();
        var colors = button.colors;
        colors.normalColor = Gold;
        colors.highlightedColor = Hex("FFD36A");
        colors.pressedColor = Hex("C89224");
        colors.selectedColor = Gold;
        button.colors = colors;
        CreateText(buttonObject.transform, "Label", "START GAME", 30, Navy, Vector2.zero, Vector2.one, FontStyle.Bold);
        return button;
    }

    static void CreateEventSystem()
    {
        new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
    }

    static void UpgradeInputModules()
    {
        var changed = false;
        foreach (var path in new[] { MainScenePath, GameScenePath })
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) == null)
                continue;

            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            foreach (var root in scene.GetRootGameObjects())
            {
                var legacy = root.GetComponentInChildren<StandaloneInputModule>(true);
                if (legacy == null)
                    continue;

                var eventSystemObject = legacy.gameObject;
                Object.DestroyImmediate(legacy);
                if (eventSystemObject.GetComponent<InputSystemUIInputModule>() == null)
                    eventSystemObject.AddComponent<InputSystemUIInputModule>();
                changed = true;
            }

            if (changed)
                EditorSceneManager.SaveScene(scene);
        }

        if (changed && AssetDatabase.LoadAssetAtPath<SceneAsset>(MainScenePath) != null)
            EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Single);
    }

    static Color Hex(string value)
    {
        ColorUtility.TryParseHtmlString("#" + value, out var color);
        return color;
    }
}

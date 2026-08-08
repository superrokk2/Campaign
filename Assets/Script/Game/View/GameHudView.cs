using System;
using UnityEngine;
using UnityEngine.UI;

namespace Campaign.Game.View
{
    /// <summary>
    /// HUD 생성과 표시만 담당하는 View. 마지막 값을 캐시해 같은 Text 할당과
    /// 불필요한 Canvas rebuild를 방지한다.
    /// </summary>
    public sealed class GameHudView : MonoBehaviour
    {
        Text phaseText;
        Text statusText;
        Button actionButton;
        Text actionLabel;
        string cachedPhase;
        string cachedStatus;
        string cachedAction;
        bool? cachedActionVisible;
        public event Action ActionPressed;

        public void Initialize()
        {
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            gameObject.AddComponent<GraphicRaycaster>();

            phaseText = CreateText("Phase", 42, TextAnchor.MiddleLeft, new Vector2(0.03f, 0.89f), new Vector2(0.45f, 0.98f));
            statusText = CreateText("Status", 28, TextAnchor.MiddleRight, new Vector2(0.55f, 0.89f), new Vector2(0.97f, 0.98f));
            actionButton = CreateButton();
            actionButton.onClick.AddListener(() => ActionPressed?.Invoke());
        }

        public void Render(string phase, string status, string action, bool showAction)
        {
            // Unity UI의 Text 변경은 Canvas 갱신으로 이어지므로 값이 달라질 때만 쓴다.
            if (cachedPhase != phase) { cachedPhase = phase; phaseText.text = phase; }
            if (cachedStatus != status) { cachedStatus = status; statusText.text = status; }
            if (cachedAction != action) { cachedAction = action; actionLabel.text = action; }
            if (cachedActionVisible != showAction)
            {
                cachedActionVisible = showAction;
                actionButton.gameObject.SetActive(showAction);
            }
        }

        Text CreateText(string objectName, int size, TextAnchor alignment, Vector2 min, Vector2 max)
        {
            var go = new GameObject(objectName, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(transform, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = min; rect.anchorMax = max; rect.offsetMin = rect.offsetMax = Vector2.zero;
            var text = go.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = size; text.alignment = alignment; text.color = new Color(0.93f, 0.96f, 1f);
            return text;
        }

        Button CreateButton()
        {
            var go = new GameObject("ActionButton", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(transform, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.38f, 0.06f); rect.anchorMax = new Vector2(0.62f, 0.16f);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            go.GetComponent<Image>().color = new Color(0.95f, 0.65f, 0.18f);
            actionLabel = CreateText("Label", 34, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one);
            actionLabel.transform.SetParent(go.transform, false);
            actionLabel.color = new Color(0.08f, 0.12f, 0.16f);
            return go.GetComponent<Button>();
        }
    }
}

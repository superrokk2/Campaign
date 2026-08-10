using UnityEngine;

namespace Campaign.Game.View
{
    /// <summary>
    /// 전투원의 시각 표현만 담당하는 MVC View.
    /// 체력 계산과 사망 판정은 Model에 두고 이 클래스는 전달받은 결과만 표시합니다.
    /// </summary>
    public sealed class CombatantView : MonoBehaviour
    {
        SpriteRenderer body;
        Transform healthFill;
        Vector3 originalScale;

        public void Initialize(Sprite sprite, Color color)
        {
            body = gameObject.AddComponent<SpriteRenderer>();
            body.sprite = sprite;
            body.color = color;
            body.sortingOrder = 2;
            originalScale = transform.localScale;

            var back = CreateBar("HealthBack", new Color(0.08f, 0.08f, 0.1f), 3);
            back.localPosition = new Vector3(0f, 0.72f, 0f);
            healthFill = CreateBar("HealthFill", new Color(0.3f, 0.95f, 0.45f), 4);
            healthFill.SetParent(back, false);
            healthFill.localScale = Vector3.one;
            healthFill.localPosition = new Vector3(0f, 0f, -0.01f);
        }

        Transform CreateBar(string objectName, Color color, int sortingOrder)
        {
            var bar = new GameObject(objectName).transform;
            bar.SetParent(transform, false);
            bar.localScale = new Vector3(0.9f, 0.1f, 1f);
            var renderer = bar.gameObject.AddComponent<SpriteRenderer>();
            renderer.sprite = body.sprite;
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;
            return bar;
        }

        public void SetHealth(int current, int maximum)
        {
            var ratio = Mathf.Clamp01((float)current / maximum);
            healthFill.localScale = new Vector3(ratio, 1f, 1f);
            healthFill.localPosition = new Vector3((ratio - 1f) * 0.5f, 0f, -0.01f);
        }

        public void FlashHit()
        {
            // 별도 코루틴 할당 없이 TickVisual 보간으로 짧은 피격 피드백을 만듭니다.
            if (body != null) body.color = Color.Lerp(body.color, Color.white, 0.55f);
            transform.localScale = originalScale * 1.12f;
        }

        public void TickVisual(float deltaTime)
        {
            transform.localScale = Vector3.Lerp(transform.localScale, originalScale, deltaTime * 12f);
        }

        public void ShowDeath() => gameObject.SetActive(false);
    }
}

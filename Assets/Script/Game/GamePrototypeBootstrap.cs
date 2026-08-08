using UnityEngine;
using UnityEngine.SceneManagement;

namespace Campaign.Game
{
    /// <summary>
    /// GameScene 진입을 감지하고 Factory를 호출하는 Composition Root.
    /// 생성 세부사항은 GamePrototypeFactory에 위임해 부트스트랩 책임을 최소화한다.
    /// </summary>
    public static class GamePrototypeBootstrap
    {
        public const string SceneName = "GameScene";
        static bool subscribed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Install()
        {
            // Domain Reload 설정과 관계없이 sceneLoaded 중복 구독을 막는다.
            if (subscribed) return;
            subscribed = true;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name != SceneName) return;

            // Factory는 ScriptableObject에서 읽은 설정에만 의존하므로 설정 프리셋 교체가 쉽다.
            var config = GamePrototypeConfig.LoadOrCreateDefault();
            new GamePrototypeFactory(config).Build(scene);
        }

        public static void Rebuild() => SceneManager.LoadScene(SceneName);
    }
}

using UnityEngine;
using UnityEngine.SceneManagement;
using EasyTransition;
using VContainer;
using VContainer.Unity;

namespace TowerBreakers.Core.SceneManagement
{
    /// <summary>
    /// [설명]: 씬 로딩 로직을 구현하며, VContainer를 통해 데이터를 다음 씬으로 전달합니다.
    /// </summary>
    public class SceneLoader : ISceneLoader
    {
        private readonly IObjectResolver m_resolver;

        // [설명]: VContainer의 Resolver를 주입받아 Scoping 기능을 활용합니다.
        public SceneLoader(IObjectResolver resolver)
        {
            m_resolver = resolver;
        }

        public void LoadScene(string sceneName, SceneContextDTO context, TransitionSettings settings = null)
        {
            Debug.Log($"[SceneLoader] 씬 전환 시작: {sceneName}");

            // [설명]: 다음 씬의 LifetimeScope에 데이터를 주입하기 위한 Scoping 설정
            // VContainer의 LifetimeScope.EnqueueParent로 데이터를 전달할 수 있습니다.
            LifetimeScope.Enqueue(builder =>
            {
                if (context != null)
                {
                    builder.RegisterInstance(context);
                }
            });

            if (settings != null && TransitionManager.Instance() != null)
            {
                TransitionManager.Instance().Transition(sceneName, settings, 0f);
            }
            else
            {
                SceneManager.LoadScene(sceneName);
            }
        }
    }
}

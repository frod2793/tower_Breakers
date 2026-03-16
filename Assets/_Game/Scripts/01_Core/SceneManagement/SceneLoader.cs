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
        #region 내부 변수
        private readonly IObjectResolver m_resolver;
        private bool m_isTransitioning;
        #endregion

        #region 초기화
        public SceneLoader(IObjectResolver resolver)
        {
            m_resolver = resolver;
            m_isTransitioning = false;
            
            // [설명]: 전역 싱글톤으로 유지되므로, 새로운 씬이 로드되면 플래그를 초기화
            SceneManager.sceneLoaded += (scene, mode) => m_isTransitioning = false;
        }
        #endregion

        #region 공개 API
        public void LoadScene(string sceneName, SceneContextDTO context, TransitionSettings settings = null)
        {
            if (m_isTransitioning)
            {
                return;
            }

            Debug.Log($"[SceneLoader] 씬 전환 시작: {sceneName}");
            m_isTransitioning = true;

            // [설명]: 전역 SceneContextDTO 인스턴스를 찾아 데이터를 업데이트합니다.
            if (m_resolver.TryResolve<SceneContextDTO>(out var globalContext) && context != null)
            {
                globalContext.Equipment = context.Equipment;
                // 필요한 경우 ExtraData 등도 동기화
                foreach (var kvp in context.ExtraData)
                {
                    globalContext.ExtraData[kvp.Key] = kvp.Value;
                }
            }

            // [설명]: TransitionManager.Instance()는 없으면 내부적으로 LogError를 발생시키므로 직접 검색
            var transitionManager = Object.FindFirstObjectByType<TransitionManager>();
            if (settings != null && transitionManager != null)
            {
                transitionManager.Transition(sceneName, settings, 0f);
            }
            else
            {
                SceneManager.LoadScene(sceneName);
            }
        }
        #endregion
    }
}

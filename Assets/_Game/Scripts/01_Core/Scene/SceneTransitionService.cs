using System;
using UnityEngine;
using EasyTransition;

namespace TowerBreakers.Core.Scene
{
    /// <summary>
    /// [기능]: 씬 전환 서비스 (EasyTransitions 래퍼)
    /// </summary>
    public class SceneTransitionService
    {
        private TransitionManager m_transitionManager;
        private readonly SceneContextDTO m_currentContext;

        public event Action OnTransitionBegin;
        public event Action OnTransitionCutPoint;
        public event Action OnTransitionEnd;

        public SceneContextDTO CurrentContext => m_currentContext;

        public SceneTransitionService()
        {
            m_currentContext = new SceneContextDTO();
            SetupTransitionManager();
        }

        private void SetupTransitionManager()
        {
            var managers = GameObject.FindObjectsOfType<TransitionManager>();
            if (managers.Length > 0)
            {
                m_transitionManager = managers[0];
                SubscribeToEvents();
            }
            else
            {
                Debug.LogWarning("[SceneTransitionService] TransitionManager를 찾을 수 없습니다.");
            }
        }

        private void SubscribeToEvents()
        {
            if (m_transitionManager != null)
            {
                m_transitionManager.onTransitionBegin += () => OnTransitionBegin?.Invoke();
                m_transitionManager.onTransitionCutPointReached += () => OnTransitionCutPoint?.Invoke();
                m_transitionManager.onTransitionEnd += () => OnTransitionEnd?.Invoke();
            }
        }

        public void LoadScene(string sceneName, TransitionSettings transition, float delay = 0f)
        {
            if (m_transitionManager == null)
            {
                Debug.LogError("[SceneTransitionManager] TransitionManager가 없습니다.");
                return;
            }

            Debug.Log($"[SceneTransitionService] 씬 전환: {sceneName}");
            m_transitionManager.Transition(sceneName, transition, delay);
        }

        public void LoadScene(int sceneIndex, TransitionSettings transition, float delay = 0f)
        {
            if (m_transitionManager == null)
            {
                Debug.LogError("[SceneTransitionManager] TransitionManager가 없습니다.");
                return;
            }

            Debug.Log($"[SceneTransitionService] 씬 전환: 인덱스 {sceneIndex}");
            m_transitionManager.Transition(sceneIndex, transition, delay);
        }

        public void LoadSceneWithContext(string sceneName, TransitionSettings transition, SceneContextDTO context, float delay = 0f)
        {
            m_currentContext.CurrentStage = context.CurrentStage;
            m_currentContext.DifficultyLevel = context.DifficultyLevel;
            m_currentContext.PlayerGold = context.PlayerGold;
            m_currentContext.EarnedItemIds = context.EarnedItemIds;
            m_currentContext.IsVictory = context.IsVictory;
            m_currentContext.Score = context.Score;

            LoadScene(sceneName, transition, delay);
        }

        public void LoadLobby(TransitionSettings transition)
        {
            LoadScene("OutGame", transition);
        }

        public void LoadInGame(TransitionSettings transition, int stage = 1, int difficulty = 1)
        {
            var context = SceneContextDTO.CreateForBattle(stage, difficulty);
            LoadSceneWithContext("inGame", transition, context);
        }

        public void LoadInGameWithRewards(TransitionSettings transition, SceneContextDTO battleResult)
        {
            LoadSceneWithContext("OutGame", transition, battleResult);
        }
    }
}

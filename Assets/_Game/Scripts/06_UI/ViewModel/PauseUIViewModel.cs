using System;
using UnityEngine;
using TowerBreakers.Core.Scene;
using EasyTransition;

namespace TowerBreakers.UI.ViewModel
{
    /// <summary>
    /// [설명]: 일시 정지 UI의 비즈니스 로직을 담당하는 뷰모델입니다.
    /// </summary>
    public class PauseUIViewModel
    {
        public event Action<bool> OnPauseStateChanged;

        private bool m_isPaused = false;
        public bool IsPaused => m_isPaused;

        private readonly SceneTransitionService m_sceneService;
        private readonly TransitionSettings m_transitionSettings;

        public PauseUIViewModel(SceneTransitionService sceneService, TransitionSettings settings = null)
        {
            m_sceneService = sceneService;
            m_transitionSettings = settings;
        }

        public void TogglePause()
        {
            SetPause(!m_isPaused);
        }

        public void SetPause(bool pause)
        {
            m_isPaused = pause;
            Time.timeScale = pause ? 0f : 1f;
            OnPauseStateChanged?.Invoke(m_isPaused);
            Debug.Log($"[PauseUI] 게임 상태: {(pause ? "일시 정지" : "재개")}");
        }

        public void Resume()
        {
            SetPause(false);
        }

        public void GoToLobby()
        {
            Time.timeScale = 1f; // 씬 전환 전 시간 복구 필수
            if (m_sceneService != null)
            {
                m_sceneService.LoadLobby(m_transitionSettings);
            }
            else
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene("OutGame");
            }
        }
    }
}
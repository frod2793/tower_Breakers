using System;
using TowerBreakers.Core.Events;
using UnityEngine;
using EasyTransition;

namespace TowerBreakers.UI.Screens
{
    /// <summary>
    /// [설명]: 인게임 메뉴(일시정지)의 비즈니스 로직을 담당하는 뷰모델입니다.
    /// 시간 배율 조절 및 씬 전환 기능을 제공합니다.
    /// </summary>
    public class InGameMenuViewModel : IDisposable
    {
        #region 내부 필드
        private readonly IEventBus m_eventBus;
        private bool m_isPaused;
        private const string OUT_GAME_SCENE_NAME = "OutGame";
        #endregion

        #region 프로퍼티 및 이벤트
        /// <summary>
        /// [설명]: 메뉴 표시 여부가 변경될 때 알림을 보냅니다.
        /// </summary>
        public event Action<bool> OnVisibilityChanged;

        public bool IsPaused => m_isPaused;
        #endregion

        #region 초기화
        public InGameMenuViewModel(IEventBus eventBus)
        {
            m_eventBus = eventBus;
            m_isPaused = false;
        }
        #endregion

        #region 공개 API
        /// <summary>
        /// [설명]: 메뉴 표시 상태를 토글합니다.
        /// </summary>
        public void ToggleMenu()
        {
            if (m_isPaused) Resume();
            else Pause();
        }

        /// <summary>
        /// [설명]: 게임을 일시정지하고 메뉴를 표시합니다.
        /// </summary>
        public void Pause()
        {
            if (m_isPaused) return;

            m_isPaused = true;
            Time.timeScale = 0f; // 시간 정지
            
            m_eventBus?.Publish(new OnGamePause());
            OnVisibilityChanged?.Invoke(true);
            Debug.Log("[InGameMenu] Game Paused");
        }

        /// <summary>
        /// [설명]: 게임을 재개하고 메뉴를 숨깁니다.
        /// </summary>
        public void Resume()
        {
            if (!m_isPaused) return;

            m_isPaused = false;
            Time.timeScale = 1f; // 시간 재개
            
            m_eventBus?.Publish(new OnGameResume());
            OnVisibilityChanged?.Invoke(false);
            Debug.Log("[InGameMenu] Game Resumed");
        }

        /// <summary>
        /// [설명]: 아웃게임(메인 로비) 씬으로 이동합니다.
        /// </summary>
        /// <param name="settings">전환 효과 설정</param>
        public void ExitToOutGame(TransitionSettings settings)
        {
            // 나갈 때는 반드시 시간 배율을 정상으로 복구해야 함
            Time.timeScale = 1f;

            if (TransitionManager.Instance() != null && settings != null)
            {
                TransitionManager.Instance().Transition(OUT_GAME_SCENE_NAME, settings, 0f);
            }
            else
            {
                // 트랜지션 매니저가 없으면 직접 씬 로드 (폴백)
                UnityEngine.SceneManagement.SceneManager.LoadScene(OUT_GAME_SCENE_NAME);
            }
        }

        public void Dispose()
        {
            // 종료 시 안전하게 시간 복구
            if (m_isPaused) Time.timeScale = 1f;
        }
        #endregion
    }
}

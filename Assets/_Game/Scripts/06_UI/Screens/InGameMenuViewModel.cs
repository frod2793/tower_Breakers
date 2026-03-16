using System;
using TowerBreakers.Core.Events;
using TowerBreakers.Core.SceneManagement;
using TowerBreakers.Player.Data;
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
        private readonly ISceneLoader m_sceneLoader;
        private readonly UserSessionModel m_sessionModel;
        private bool m_isPaused;
        private bool m_isExiting;
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
        public InGameMenuViewModel(IEventBus eventBus, ISceneLoader sceneLoader, UserSessionModel sessionModel)
        {
            m_eventBus = eventBus;
            m_sceneLoader = sceneLoader;
            m_sessionModel = sessionModel;
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
            // [방어 코드]: 중복 전환 방지
            if (m_isExiting)
            {
                return;
            }
            m_isExiting = true;

            // 나갈 때는 반드시 시간 배율을 정상으로 복구해야 함
            Time.timeScale = 1f;

            // [수정]: 현재 세션의 데이터를 포함하여 전달 - 데이터 유실 방지
            var context = new SceneContextDTO();
            
            if (m_sessionModel != null)
            {
                var currentEquipment = m_sessionModel.ExportDTO();
                context.Equipment = currentEquipment;
                Debug.Log($"[TRACE] ExitToOutGame: ExportDTO() 결과 - 무기 {currentEquipment.OwnedWeaponIds.Count}개, 갑주 {currentEquipment.OwnedArmorIds.Count}개");
                Debug.Log($"[TRACE] ExitToOutGame: DTO 데이터 - {JsonUtility.ToJson(currentEquipment)}");
                Debug.Log($"[InGameMenu] ExitToOutGame: 무기 {currentEquipment.OwnedWeaponIds.Count}개, 갑주 {currentEquipment.OwnedArmorIds.Count}개 전달");
            }
            else
            {
                Debug.LogWarning("[InGameMenu] UserSessionModel이 null입니다. 빈 DTO를 전달합니다.");
            }
            
            if (m_sceneLoader != null)
            {
                m_sceneLoader.LoadScene(OUT_GAME_SCENE_NAME, context, settings);
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

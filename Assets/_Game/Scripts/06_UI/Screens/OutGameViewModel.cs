using UnityEngine;
using EasyTransition;
using TowerBreakers.Core.SceneManagement;
using TowerBreakers.Player.Data;

namespace TowerBreakers.UI.Screens
{
    /// <summary>
    /// [설명]: 아웃게임의 비즈니스 로직을 담당하는 뷰모델 클래스입니다.
    /// 씬 전환 명령을 처리하며, 특정 트랜지션 설정을 사용합니다.
    /// </summary>
    public class OutGameViewModel
    {
        #region 내부 변수
        private readonly ISceneLoader m_sceneLoader;
        private readonly UserSessionModel m_sessionModel;
        private bool m_isStarting;
        private const string IN_GAME_SCENE_NAME = "InGame";
        #endregion

        #region 초기화
        public OutGameViewModel(ISceneLoader sceneLoader, UserSessionModel sessionModel)
        {
            m_sceneLoader = sceneLoader;
            m_sessionModel = sessionModel;
            m_isStarting = false;
        }
        #endregion

        #region 공개 메서드
        /// <summary>
        /// [설명]: 게임 플레이를 위해 인게임 씬으로의 전환을 시작합니다.
        /// </summary>
        /// <param name="settings">사용할 트랜지션 설정 에셋</param>
        /// <param name="delay">트랜지션 시작 전 지연 시간</param>
        public void StartGameTransition(TransitionSettings settings, float delay = 0f)
        {
            if (m_isStarting)
            {
                return;
            }

            if (settings == null)
            {
                Debug.LogError("[OutGameViewModel] TransitionSettings 가 지정되지 않았습니다.");
                return;
            }

            m_isStarting = true;

            // [설명]: 현재 세션의 장비 데이터를 DTO에 담아 다음 씬으로 전달 준비
            var context = new SceneContextDTO();
            if (m_sessionModel != null)
            {
                context.Equipment = m_sessionModel.CurrentEquipment;
            }

            // [설명]: 명시적으로 정의된 SceneLoader를 통해 데이터와 함께 씬 전환
            if (m_sceneLoader != null)
            {
                m_sceneLoader.LoadScene(IN_GAME_SCENE_NAME, context, settings);
            }
            else
            {
                // 폴백: 직접 씬 로드
                UnityEngine.SceneManagement.SceneManager.LoadScene(IN_GAME_SCENE_NAME);
            }
        }
        #endregion
    }
}

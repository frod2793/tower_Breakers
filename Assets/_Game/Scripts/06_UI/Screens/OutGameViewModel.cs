using UnityEngine;
using EasyTransition;

namespace TowerBreakers.UI.Screens
{
    /// <summary>
    /// [설명]: 아웃게임의 비즈니스 로직을 담당하는 뷰모델 클래스입니다.
    /// 씬 전환 명령을 처리하며, 특정 트랜지션 설정을 사용합니다.
    /// </summary>
    public class OutGameViewModel
    {
        #region 내부 변수
        private const string IN_GAME_SCENE_NAME = "InGame";
        #endregion

        #region 공개 메서드
        /// <summary>
        /// [설명]: 게임 플레이를 위해 인게임 씬으로의 전환을 시작합니다.
        /// </summary>
        /// <param name="settings">사용할 트랜지션 설정 에셋</param>
        /// <param name="delay">트랜지션 시작 전 지연 시간</param>
        public void StartGameTransition(TransitionSettings settings, float delay = 0f)
        {
            if (settings == null)
            {
                Debug.LogError("[OutGameViewModel] TransitionSettings 가 지정되지 않았습니다.");
                return;
            }

            // EasyTransitions 매니저를 통해 씬 로드
            if (TransitionManager.Instance() != null)
            {
                TransitionManager.Instance().Transition(IN_GAME_SCENE_NAME, settings, delay);
            }
            else
            {
                Debug.LogError("[OutGameViewModel] TransitionManager 인스턴스를 찾을 수 없습니다.");
            }
        }
        #endregion
    }
}

using UnityEngine;
using UnityEngine.UI;
using EasyTransition;

namespace TowerBreakers.UI.Screens
{
    /// <summary>
    /// [설명]: 아웃게임 메인 화면을 관리하는 뷰 클래스입니다.
    /// MVVM 패턴을 따르며, 게임 시작 버튼 이벤트를 뷰모델로 전달합니다.
    /// </summary>
    public class OutGameView : MonoBehaviour
    {
        #region 에디터 설정
        [Header("UI 요소")]
        [SerializeField, Tooltip("게임 플레이 시작 버튼")]
        private Button m_playButton;

        [Header("트랜지션 설정")]
        [SerializeField, Tooltip("씬 전환 시 사용할 이지 트랜지션 설정")]
        private TransitionSettings m_transitionSettings;

        [SerializeField, Tooltip("트랜지션 시작 전 지연 시간")]
        private float m_startDelay = 0.1f;
        #endregion

        #region 내부 변수
        private OutGameViewModel m_viewModel;
        #endregion

        #region 초기화 및 바인딩
        /// <summary>
        /// [설명]: 외부(LifetimeScope)에서 뷰모델을 주입받아 초기화합니다.
        /// </summary>
        /// <param name="viewModel">주입될 뷰모델 인스턴스</param>
        public void Initialize(OutGameViewModel viewModel)
        {
            if (viewModel == null)
            {
                Debug.LogError("[OutGameView] ViewModel 이 null 입니다.");
                return;
            }

            m_viewModel = viewModel;
            Bind();
        }

        /// <summary>
        /// [설명]: UI 이벤트를 뷰모델 명령에 바인딩합니다.
        /// </summary>
        private void Bind()
        {
            if (m_playButton != null)
            {
                m_playButton.onClick.RemoveAllListeners();
                m_playButton.onClick.AddListener(OnPlayButtonClicked);
            }
        }
        #endregion

        #region UI 이벤트 핸들러
        /// <summary>
        /// [설명]: 플레이 버튼 클릭 시 호출됩니다.
        /// </summary>
        private void OnPlayButtonClicked()
        {
            if (m_viewModel != null)
            {
                m_viewModel.StartGameTransition(m_transitionSettings, m_startDelay);
            }
        }
        #endregion
    }
}

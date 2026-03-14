using UnityEngine;
using UnityEngine.UI;
using EasyTransition;
using TowerBreakers.UI.Screens;

namespace TowerBreakers.UI.Screens
{
    /// <summary>
    /// [설명]: 인게임 메뉴 UI를 관리하는 클래스입니다.
    /// 버튼 입력 및 키보드(Escape) 입력을 처리합니다.
    /// </summary>
    public class InGameMenuView : MonoBehaviour
    {
        #region 에디터 설정
        [Header("UI 요소")]
        [SerializeField, Tooltip("메뉴의 최상위 루트 오브젝트")]
        private GameObject m_menuRoot;

        [SerializeField, Tooltip("게임 재개 버튼")]
        private Button m_resumeButton;

        [SerializeField, Tooltip("메인으로 나가기 버튼")]
        private Button m_exitButton;

        [Header("씬 전환 설정")]
        [SerializeField, Tooltip("아웃게임으로 전환 시 사용할 효과 설정")]
        private TransitionSettings m_transitionSettings;
        #endregion

        #region 내부 변수
        private InGameMenuViewModel m_viewModel;
        #endregion

        #region 초기화
        /// <summary>
        /// [설명]: 뷰모델을 주입받아 초기 상태를 설정하고 이벤트를 바인딩합니다.
        /// </summary>
        public void Initialize(InGameMenuViewModel viewModel)
        {
            m_viewModel = viewModel;
            
            // 초기 가시성 설정
            if (m_menuRoot != null) m_menuRoot.SetActive(m_viewModel.IsPaused);

            // 뷰모델 이벤트 구독
            m_viewModel.OnVisibilityChanged += HandleVisibilityChanged;

            // UI 버튼 리스너 등록
            if (m_resumeButton != null)
                m_resumeButton.onClick.AddListener(OnResumeButtonClicked);

            if (m_exitButton != null)
                m_exitButton.onClick.AddListener(OnExitButtonClicked);
        }
        #endregion

        #region 유니티 생명주기

        private void OnDestroy()
        {
            if (m_viewModel != null)
            {
                m_viewModel.OnVisibilityChanged -= HandleVisibilityChanged;
            }
        }
        #endregion

        #region 이벤트 핸들러
        private void HandleVisibilityChanged(bool isVisible)
        {
            if (m_menuRoot != null)
            {
                m_menuRoot.SetActive(isVisible);
            }
        }

        private void OnResumeButtonClicked()
        {
            if (m_viewModel != null)
            {
                m_viewModel.Resume();
            }
        }

        private void OnExitButtonClicked()
        {
            if (m_viewModel != null)
            {
                m_viewModel.ExitToOutGame(m_transitionSettings);
            }
        }
        #endregion
    }
}

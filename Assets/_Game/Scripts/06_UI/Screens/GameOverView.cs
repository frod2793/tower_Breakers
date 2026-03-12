using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace TowerBreakers.UI.Screens
{
    /// <summary>
    /// [설명]: 게임 오버 화면을 관리하는 뷰 클래스입니다.
    /// </summary>
    public class GameOverView : MonoBehaviour
    {
        #region 에디터 설정
        [SerializeField] private GameObject m_panel;
        [SerializeField] private Button m_restartButton;
        #endregion

        #region 내부 변수
        private GameOverViewModel m_viewModel;
        #endregion

        [Inject]
        public void Initialize(GameOverViewModel viewModel)
        {
            m_viewModel = viewModel;
            m_viewModel.OnShow += Show;
            
            if (m_restartButton != null)
                m_restartButton.onClick.AddListener(OnRestartClicked);
            
            m_panel.SetActive(false);
        }

        private void Show()
        {
            m_panel.SetActive(true);
        }

        private void OnRestartClicked()
        {
            m_viewModel.RestartGame();
            m_panel.SetActive(false);
        }

        private void OnDestroy()
        {
            if (m_viewModel != null)
                m_viewModel.OnShow -= Show;
        }
    }
}

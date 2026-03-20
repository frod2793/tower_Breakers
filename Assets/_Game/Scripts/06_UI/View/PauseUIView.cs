using UnityEngine;
using UnityEngine.UI;
using TowerBreakers.UI.View;
using TowerBreakers.UI.ViewModel;
using VContainer;

namespace TowerBreakers.UI.View
{
    /// <summary>
    /// [설명]: 일시 정지 UI를 시각적으로 제어하는 뷰 클래스입니다.
    /// </summary>
    public class PauseUIView : MonoBehaviour
    {
        [Header("UI 참조")]
        [SerializeField] private GameObject m_pausePanel;
        [SerializeField] private Button m_resumeButton;
        [SerializeField] private Button m_lobbyButton;
        [SerializeField, Tooltip("HUD에 위치한 일시정지 메뉴 열기 버튼")] 
        private Button m_openMenuButton;

        private PauseUIViewModel m_viewModel;

        [Inject]
        public void Construct(PauseUIViewModel viewModel)
        {
            m_viewModel = viewModel;
            m_viewModel.OnPauseStateChanged += UpdateUI;
        }

        private void Start()
        {
            if (m_resumeButton != null) m_resumeButton.onClick.AddListener(() => m_viewModel.Resume());
            if (m_lobbyButton != null) m_lobbyButton.onClick.AddListener(() => m_viewModel.GoToLobby());
            if (m_openMenuButton != null) m_openMenuButton.onClick.AddListener(() => m_viewModel.SetPause(true));
            
            if (m_pausePanel != null) m_pausePanel.SetActive(false);
        }
        
        private void UpdateUI(bool isPaused)
        {
            if (m_pausePanel != null) m_pausePanel.SetActive(isPaused);
        }

        private void OnDestroy()
        {
            if (m_viewModel != null) m_viewModel.OnPauseStateChanged -= UpdateUI;
        }
    }
}
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace TowerBreakers.UI.HUD
{
    /// <summary>
    /// [설명]: 실제 HUD UI 요소를 관리하고 뷰모델의 데이터를 시각화하는 뷰 클래스입니다.
    /// </summary>
    public class HUDView : MonoBehaviour
    {
        #region 에디터 설정
        [SerializeField, Tooltip("HP 슬라이더")]
        private Slider m_hpSlider;

        [SerializeField, Tooltip("현재 층 텍스트")]
        private Text m_floorText;
        #endregion

        #region 내부 필드
        private HUDViewModel m_viewModel;
        #endregion

        #region 초기화 및 바인딩
        [Inject]
        public void Initialize(HUDViewModel viewModel)
        {
            m_viewModel = viewModel;
            m_viewModel.OnDataUpdated += UpdateUI;
            UpdateUI();
        }

        private void UpdateUI()
        {
            if (m_viewModel == null) return;

            if (m_hpSlider != null)
                m_hpSlider.value = m_viewModel.HpRatio;

            if (m_floorText != null)
                m_floorText.text = $"Floor {m_viewModel.CurrentFloor}";
        }
        #endregion

        #region 유니티 생명주기
        private void OnDestroy()
        {
            if (m_viewModel != null)
                m_viewModel.OnDataUpdated -= UpdateUI;
        }
        #endregion
    }
}

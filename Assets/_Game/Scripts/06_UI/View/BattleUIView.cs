using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TowerBreakers.UI.ViewModel;
using TowerBreakers.UI.DTO;
using TowerBreakers.Player.DTO;

namespace TowerBreakers.UI.View
{
    /// <summary>
    /// [설명]: 전투 UI의 시각적 요소를 담당하는 뷰 클래스입니다.
    /// MVVM 패턴을 따르며 BattleUIViewModel과 바인딩됩니다.
    /// </summary>
    public class BattleUIView : MonoBehaviour
    {
        #region 에디터 설정
        [Header("스킬 버튼")]
        [SerializeField] private Button m_dashButton;
        [SerializeField] private Button m_parryButton;
        [SerializeField] private Button m_attackButton;
        [SerializeField] private Button m_skill1Button;
        [SerializeField] private Button m_skill2Button;
        [SerializeField] private Button m_skill3Button;

        [Header("쿨다운 이미지")]
        [SerializeField] private Image m_dashCooldownImage;
        [SerializeField] private Image m_parryCooldownImage;
        [SerializeField] private Image m_attackCooldownImage;
        [SerializeField] private Image m_skill1CooldownImage;
        [SerializeField] private Image m_skill2CooldownImage;
        [SerializeField] private Image m_skill3CooldownImage;
        #endregion

        #region 내부 필드
        private BattleUIViewModel m_viewModel;
        private Dictionary<string, Image> m_cooldownImages = new Dictionary<string, Image>();
        #endregion

        #region 초기화 및 바인딩 로직
        public void Initialize(BattleUIViewModel viewModel, BattleUIDTO dto)
        {
            if (viewModel == null) return;
            m_viewModel = viewModel;

            // 쿨다운 이미지 매핑
            m_cooldownImages[dto.DashSkill.Name] = m_dashCooldownImage;
            m_cooldownImages[dto.ParrySkill.Name] = m_parryCooldownImage;
            m_cooldownImages[dto.AttackSkill.Name] = m_attackCooldownImage;
            m_cooldownImages[dto.Skill1.Name] = m_skill1CooldownImage;
            m_cooldownImages[dto.Skill2.Name] = m_skill2CooldownImage;
            m_cooldownImages[dto.Skill3.Name] = m_skill3CooldownImage;

            // 버튼 이벤트 바인딩
            if (m_dashButton != null) m_dashButton.onClick.AddListener(() => m_viewModel.ExecuteSkill(dto.DashSkill.Name));
            if (m_parryButton != null) m_parryButton.onClick.AddListener(() => m_viewModel.ExecuteSkill(dto.ParrySkill.Name));
            if (m_attackButton != null) m_attackButton.onClick.AddListener(() => m_viewModel.ExecuteSkill(dto.AttackSkill.Name));
            if (m_skill1Button != null) m_skill1Button.onClick.AddListener(() => m_viewModel.ExecuteSkill(dto.Skill1.Name));
            if (m_skill2Button != null) m_skill2Button.onClick.AddListener(() => m_viewModel.ExecuteSkill(dto.Skill2.Name));
            if (m_skill3Button != null) m_skill3Button.onClick.AddListener(() => m_viewModel.ExecuteSkill(dto.Skill3.Name));

            // 뷰모델 상태 변경 구독
            m_viewModel.OnCooldownChanged += UpdateCooldownValue;
        }

        private void UpdateCooldownValue(string skillName, float ratio)
        {
            if (m_cooldownImages.TryGetValue(skillName, out var image))
            {
                if (image != null) image.fillAmount = ratio;
            }
        }

        private void OnDestroy()
        {
            if (m_viewModel != null)
            {
                m_viewModel.OnCooldownChanged -= UpdateCooldownValue;
            }
        }
        #endregion

        #region 유니티 생명주기
        private void Update()
        {
            if (m_viewModel != null)
            {
                m_viewModel.Update(Time.deltaTime);
            }
        }
        #endregion
    }
}

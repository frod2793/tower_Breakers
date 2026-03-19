using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TowerBreakers.UI.ViewModel;
using TowerBreakers.UI.DTO;
using TowerBreakers.Player.DTO;
using DG.Tweening;

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
        [SerializeField] private HoldableButton m_attackHoldBtn;
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

        [Header("특수 효과")]
        [SerializeField] private Image m_goImage; // [추가]: 층 클리어 시 점멸할 GO 이미지
        [SerializeField] private Button m_screenClickArea; // [추가]: 전체 화면 클릭 영역 (투명 버튼)
        #endregion

        #region 내부 필드
        private BattleUIViewModel m_viewModel;
        private Dictionary<string, Image> m_cooldownImages = new Dictionary<string, Image>();
        private List<Button> m_skillButtons = new List<Button>();
        private DG.Tweening.Tween m_goTween;
        #endregion

        #region 초기화 및 바인딩 로직
        public void Initialize(BattleUIViewModel viewModel, BattleUIDTO dto)
        {
            if (viewModel == null) return;
            m_viewModel = viewModel;

            // 스킬 버튼 리스트화 (일괄 활성/비활성용)
            m_skillButtons.Clear();
            if (m_dashButton != null) m_skillButtons.Add(m_dashButton);
            if (m_parryButton != null) m_skillButtons.Add(m_parryButton);
            if (m_attackButton != null) m_skillButtons.Add(m_attackButton);
            if (m_skill1Button != null) m_skillButtons.Add(m_skill1Button);
            if (m_skill2Button != null) m_skillButtons.Add(m_skill2Button);
            if (m_skill3Button != null) m_skillButtons.Add(m_skill3Button);

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
            
            // [개선]: 공격 버튼은 홀드와 연타가 가능하도록 HoldableButton 이벤트를 사용
            if (m_attackHoldBtn != null) 
            {
                m_attackHoldBtn.OnExecute += () => m_viewModel.ExecuteSkill(dto.AttackSkill.Name);
            }
            else if (m_attackButton != null) 
            {
                m_attackButton.onClick.AddListener(() => m_viewModel.ExecuteSkill(dto.AttackSkill.Name));
            }

            if (m_skill1Button != null) m_skill1Button.onClick.AddListener(() => m_viewModel.ExecuteSkill(dto.Skill1.Name));
            if (m_skill2Button != null) m_skill2Button.onClick.AddListener(() => m_viewModel.ExecuteSkill(dto.Skill2.Name));
            if (m_skill3Button != null) m_skill3Button.onClick.AddListener(() => m_viewModel.ExecuteSkill(dto.Skill3.Name));

            // [추가]: 전체 화면 클릭 바인딩
            if (m_screenClickArea != null)
            {
                m_screenClickArea.onClick.AddListener(() => m_viewModel.NotifyScreenClicked());
                m_screenClickArea.gameObject.SetActive(false); // 기본은 비활성
            }

            // 뷰모델 상태 변경 구독
            m_viewModel.OnCooldownChanged += UpdateCooldownValue;
            m_viewModel.OnGoStateChanged += OnGoStateChanged;
            m_viewModel.OnInteractionChanged += OnInteractionChanged;
        }

        private void OnGoStateChanged(bool active)
        {
            if (m_goImage == null) return;

            m_goImage.gameObject.SetActive(active);
            if (m_goTween != null) m_goTween.Kill();

            if (active)
            {
                // [연출]: 0.5초 주기로 점멸하는 루프 애니메이션
                m_goTween = m_goImage.DOFade(0.2f, 0.5f).SetLoops(-1, DG.Tweening.LoopType.Yoyo);
                if (m_screenClickArea != null) m_screenClickArea.gameObject.SetActive(true);
            }
            else
            {
                if (m_screenClickArea != null) m_screenClickArea.gameObject.SetActive(false);
            }
        }

        private void OnInteractionChanged(bool enabled)
        {
            foreach (var btn in m_skillButtons)
            {
                if (btn != null) btn.interactable = enabled;
            }
            
            // [개선]: HoldableButton 컴포넌트도 개별적으로 처리 필요할 경우 추가
            if (m_attackHoldBtn != null) m_attackHoldBtn.enabled = enabled;
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
                m_viewModel.OnGoStateChanged -= OnGoStateChanged;
                m_viewModel.OnInteractionChanged -= OnInteractionChanged;
            }
            if (m_goTween != null) m_goTween.Kill();
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

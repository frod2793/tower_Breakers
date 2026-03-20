using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TowerBreakers.UI.ViewModel;
using TowerBreakers.UI.DTO;
using TowerBreakers.Player.DTO;
using DG.Tweening;
using TMPro;
using TowerBreakers.Core.Events;

namespace TowerBreakers.UI.View
{
    /// <summary>
    /// [클래스]: 전투 UI의 시각적 요소를 담당하는 뷰 클래스입니다.
    /// 체력과 적 수량을 시각적인 아이콘 리스트(BattleIconGroup)로 표시합니다.
    /// </summary>
    public class BattleUIView : MonoBehaviour
    {
        #region 에디터 설정
        [Header("플레이어 상태 UI (아이콘)")]
        [SerializeField, Tooltip("플레이어 체력(생명) 아이콘 그룹")]
        private BattleIconGroup m_healthIconGroup;

        [Header("적 상태 UI (아이콘)")]
        [SerializeField, Tooltip("노멀 적 아이콘 그룹")]
        private BattleIconGroup m_normalEnemyIconGroup;
        
        [SerializeField, Tooltip("엘리트 적 아이콘 그룹")]
        private BattleIconGroup m_eliteEnemyIconGroup;

        [Header("보조 텍스트")]
        [SerializeField] private TextMeshProUGUI m_enemyCountText;

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
        [SerializeField] private Image m_goImage;
        [SerializeField] private Button m_screenClickArea;
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

            // 스킬 버튼 리스트화
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

            if (m_screenClickArea != null)
            {
                m_screenClickArea.onClick.RemoveAllListeners();
                m_screenClickArea.onClick.AddListener(() => m_viewModel.NotifyScreenClicked());
                m_screenClickArea.gameObject.SetActive(false);
            }

            // 뷰모델 상태 변경 구독
            m_viewModel.OnCooldownChanged += UpdateCooldownValue;
            m_viewModel.OnGoStateChanged += OnGoStateChanged;
            m_viewModel.OnInteractionChanged += OnInteractionChanged;
            
            // [리팩토링]: 아이콘 기반 상태 UI 구독
            m_viewModel.OnHealthChanged += UpdateHealthIcons;
            m_viewModel.OnRemainingEnemyChanged += UpdateEnemyCountText;
            m_viewModel.OnDetailedEnemyCountChanged += UpdateEnemyIcons;

            // [추가]: 구독 완료 후 초기 상태 요청 (게임 시작 시 UI 즉시 반영)
            m_viewModel.RequestInitialState();
        }

        private void UpdateHealthIcons(int current, int max)
        {
            // [리팩토링]: 슬라이더 대신 아이콘 그룹의 개수 설정 (1:1 매칭)
            if (m_healthIconGroup != null)
            {
                m_healthIconGroup.SetCount(current);
            }
        }

        private void UpdateEnemyCountText(int remaining, int total)
        {
            if (m_enemyCountText != null)
            {
                m_enemyCountText.text = $"ENEMIES: {remaining}/{total}";
                m_enemyCountText.transform.DOPunchScale(Vector3.one * 0.1f, 0.2f);
            }
        }

        private void UpdateEnemyIcons(OnEnemyCountChanged evt)
        {
            if (m_normalEnemyIconGroup != null) m_normalEnemyIconGroup.SetCount(evt.NormalRemaining);
            if (m_eliteEnemyIconGroup != null) m_eliteEnemyIconGroup.SetCount(evt.EliteRemaining);
        }

        private void OnGoStateChanged(bool active)
        {
            if (m_goImage == null) return;
            if (m_goTween != null) m_goTween.Kill();

            if (active)
            {
                m_goImage.gameObject.SetActive(true);
                m_goTween = m_goImage.DOFade(0.2f, 0.5f).SetLoops(-1, LoopType.Yoyo);
                if (m_screenClickArea != null) m_screenClickArea.gameObject.SetActive(true);
            }
            else
            {
                m_goImage.gameObject.SetActive(false);
                if (m_screenClickArea != null) m_screenClickArea.gameObject.SetActive(false);
            }
        }

        private void OnInteractionChanged(bool enabled)
        {
            foreach (var btn in m_skillButtons) if (btn != null) btn.interactable = enabled;
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
                m_viewModel.OnHealthChanged -= UpdateHealthIcons;
                m_viewModel.OnRemainingEnemyChanged -= UpdateEnemyCountText;
                m_viewModel.OnDetailedEnemyCountChanged -= UpdateEnemyIcons;
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

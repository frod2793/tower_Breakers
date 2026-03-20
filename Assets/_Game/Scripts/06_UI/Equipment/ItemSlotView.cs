using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using TowerBreakers.Player.Data;

namespace TowerBreakers.UI.Equipment
{
    /// <summary>
    /// [기능]: 아이템 슬릿 UI 뷰
    /// </summary>
    public class ItemSlotView : MonoBehaviour
    {
        [Header("UI 컴포넌트")]
        [Tooltip("아이템 이미지")]
        [SerializeField] private Image m_itemImage;

        [Tooltip("아이템 이름 텍스트")]
        [SerializeField] private TextMeshProUGUI m_itemNameText;

        [Tooltip("장착 상태 텍스트")]
        [SerializeField] private TextMeshProUGUI m_equippedText;

        [Tooltip("스탯 요약 텍스트")]
        [SerializeField] private TextMeshProUGUI m_statSummaryText;

        [Tooltip("등급 표시 이미지")]
        [SerializeField] private Image m_gradeBadge;

        [Tooltip("장착/해제 버튼 (선택 사항)")]
        [SerializeField] private Button m_equipButton;

        [Header("설정")]
        [Tooltip("등급별 색상")]
        [SerializeField] private Color[] m_gradeColors;

        private ItemSlotViewModel m_viewModel;

        public void Initialize(ItemSlotViewModel viewModel)
        {
            m_viewModel = viewModel;
            UpdateUI();

            if (m_equipButton != null)
            {
                m_equipButton.onClick.RemoveAllListeners();
                m_equipButton.onClick.AddListener(OnSlotClicked);
            }
        }

        private void UpdateUI()
        {
            if (m_viewModel == null)
            {
                return;
            }

            if (m_itemNameText != null)
            {
                m_itemNameText.text = m_viewModel.ItemName;
            }

            if (m_equippedText != null)
            {
                m_equippedText.text = m_viewModel.IsEquipped ? "장착중" : string.Empty;
                m_equippedText.gameObject.SetActive(m_viewModel.IsEquipped);
            }

            if (m_statSummaryText != null)
            {
                m_statSummaryText.text = m_viewModel.GetStatSummary();
            }

            if (m_gradeBadge != null && m_gradeColors != null && m_gradeColors.Length > m_viewModel.Grade)
            {
                m_gradeBadge.color = m_gradeColors[m_viewModel.Grade];
            }

            UpdateItemImage();
        }

        private void UpdateItemImage()
        {
            if (m_itemImage != null && m_viewModel != null)
            {
                var sprite = m_viewModel.Icon;
                m_itemImage.sprite = sprite;
                m_itemImage.gameObject.SetActive(sprite != null);
            }
        }

        public void OnSlotClicked()
        {
            if (m_viewModel == null) return;

            if (m_viewModel.IsEquipped)
            {
                m_viewModel.Unequip();
            }
            else
            {
                m_viewModel.Equip();
            }
            
            PlayClickAnimation();
        }

        private void PlayClickAnimation()
        {
            if (transform != null)
            {
                transform.DOKill();
                transform.DOScale(0.9f, 0.1f)
                    .SetEase(Ease.OutQuad)
                    .OnComplete(() =>
                    {
                        transform.DOScale(1f, 0.1f)
                            .SetEase(Ease.InQuad);
                    });
            }
        }

        private void OnDestroy()
        {
            transform.DOKill();
        }
    }
}

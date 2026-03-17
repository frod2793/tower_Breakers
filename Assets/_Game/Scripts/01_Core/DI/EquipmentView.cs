using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using VContainer;
using TowerBreakers.Player.Data;
using TowerBreakers.Player.Model;
using TowerBreakers.UI.Equipment;
using TowerBreakers.Core.Scene;
using EasyTransition;

namespace TowerBreakers.Core.DI
{
    /// <summary>
    /// [기능]: 장비 UI 메인 뷰
    /// </summary>
    public class EquipmentView : MonoBehaviour
    {
        [Header("인벤토리 패널")]
        [Tooltip("인벤토리 슬롯 부모 패널")]
        [SerializeField] private Transform m_inventoryPanel;

        [Tooltip("아이템 슬롯 프리팹")]
        [SerializeField] private ItemSlotView m_itemSlotPrefab;

        [Header("스탯 패널")]
        [Tooltip("공격력 텍스트")]
        [SerializeField] private TextMeshProUGUI m_attackText;

        [Tooltip("방어력 텍스트")]
        [SerializeField] private TextMeshProUGUI m_defenseText;

        [Tooltip("체력 텍스트")]
        [SerializeField] private TextMeshProUGUI m_healthText;

        [Tooltip("이동속도 텍스트")]
        [SerializeField] private TextMeshProUGUI m_moveSpeedText;

        [Tooltip("골드 텍스트")]
        [SerializeField] private TextMeshProUGUI m_goldText;

        [Header("장착 슬롯")]
        [Tooltip("무기 슬롯")]
        [SerializeField] private ItemSlotView m_weaponSlot;

        [Tooltip("방어구 슬롯")]
        [SerializeField] private ItemSlotView m_armorSlot;

        [Tooltip("투구 슬롯")]
        [SerializeField] private ItemSlotView m_helmetSlot;

        [Header("네비게이션")]
        [Tooltip("인게임 시작 버튼")]
        [SerializeField] private Button m_startGameButton;

        [Tooltip("인게임 시작 시 사용할 트랜지션 설정")]
        [SerializeField] private EasyTransition.TransitionSettings m_startGameTransition;

        private EquipmentViewModel m_viewModel;
        private List<ItemSlotView> m_instantiatedSlots = new List<ItemSlotView>();
        private UserSessionModel m_userSession;
        private SceneTransitionService m_sceneTransitionService;

        [Inject]
        public void Construct(EquipmentViewModel viewModel, UserSessionModel userSession, SceneTransitionService sceneTransitionService)
        {
            m_viewModel = viewModel;
            m_userSession = userSession;
            m_sceneTransitionService = sceneTransitionService;

            SubscribeEvents();
            InitializeSlots();
        }

        private void Start()
        {
            if (m_viewModel != null)
            {
                RefreshUI();
            }

            SetupNavigationButtons();
        }

        private void SubscribeEvents()
        {
            if (m_viewModel != null)
            {
                m_viewModel.OnInventoryUpdated += OnInventoryUpdated;
                m_viewModel.OnStatsUpdated += OnStatsUpdated;
            }
        }

        private void UnsubscribeEvents()
        {
            if (m_viewModel != null)
            {
                m_viewModel.OnInventoryUpdated -= OnInventoryUpdated;
                m_viewModel.OnStatsUpdated -= OnStatsUpdated;
            }
        }

        private void OnInventoryUpdated(System.Collections.Generic.IReadOnlyList<ItemSlotViewModel> items)
        {
            RefreshInventorySlots(items);
        }

        private void OnStatsUpdated(StatModifiers stats)
        {
            RefreshStatsPanel(stats);
        }

        private void InitializeSlots()
        {
            if (m_itemSlotPrefab == null)
            {
                Debug.LogWarning("[EquipmentView] 아이템 슬롯 프리팹이 설정되지 않았습니다.");
            }

            ClearInventorySlots();
        }

        private void RefreshUI()
        {
            if (m_userSession != null)
            {
                UpdateGoldText(m_userSession.Gold);
            }
        }

        private void RefreshInventorySlots(System.Collections.Generic.IReadOnlyList<ItemSlotViewModel> items)
        {
            ClearInventorySlots();

            if (items == null || items.Count == 0)
            {
                return;
            }

            foreach (var item in items)
            {
                CreateInventorySlot(item);
            }
        }

        private void CreateInventorySlot(ItemSlotViewModel itemViewModel)
        {
            if (m_itemSlotPrefab == null || m_inventoryPanel == null)
            {
                return;
            }

            var slot = Instantiate(m_itemSlotPrefab, m_inventoryPanel);
            slot.Initialize(itemViewModel);
            m_instantiatedSlots.Add(slot);
        }

        private void ClearInventorySlots()
        {
            foreach (var slot in m_instantiatedSlots)
            {
                if (slot != null)
                {
                    Destroy(slot.gameObject);
                }
            }
            m_instantiatedSlots.Clear();
        }

        private void RefreshStatsPanel(StatModifiers stats)
        {
            if (stats == null)
            {
                return;
            }

            UpdateStatText(m_attackText, "공격력", stats.Attack);
            UpdateStatText(m_defenseText, "방어력", stats.Defense);
            UpdateStatText(m_healthText, "체력", stats.Health);
            UpdateStatText(m_moveSpeedText, "이동속도", stats.MoveSpeed);
        }

        private void UpdateStatText(TextMeshProUGUI textMesh, string statName, float value)
        {
            if (textMesh != null)
            {
                var sign = value >= 0 ? "+" : "";
                textMesh.text = $"{statName}: {sign}{value:F1}";
            }
        }

        private void UpdateGoldText(int gold)
        {
            if (m_goldText != null)
            {
                m_goldText.text = $"골드: {gold:N0}";
            }
        }

        private void SetupNavigationButtons()
        {
            if (m_startGameButton != null)
            {
                m_startGameButton.onClick.AddListener(OnStartGameClicked);
            }
        }

        private void OnStartGameClicked()
        {
            Debug.Log("[EquipmentView] 인게임 시작 버튼 클릭");

            if (m_sceneTransitionService != null)
            {
                m_sceneTransitionService.LoadInGame(m_startGameTransition, 1, 1);
            }
            else
            {
                Debug.LogWarning("[EquipmentView] SceneTransitionService가 null입니다.");
            }
        }

        private void OnDestroy()
        {
            UnsubscribeEvents();
        }
    }
}

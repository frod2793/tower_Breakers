using UnityEngine;
using UnityEngine.UI;
using TMPro;
using TowerBreakers.UI.Equipment;
using TowerBreakers.Player.Data;
using TowerBreakers.Player.Model;
using TowerBreakers.Core.Scene;
using System.Collections.Generic;
using VContainer;

namespace TowerBreakers.UI.View
{
    /// <summary>
    /// [기능]: 플레이어의 인벤토리, 장착 상태, 스탯을 관리하는 로비 메인 UI 뷰 클래스입니다.
    /// </summary>
    public class EquipmentView : MonoBehaviour
    {
        [Header("장착 슬롯")]
        [SerializeField] private ItemSlotView m_weaponSlot;
        [SerializeField] private ItemSlotView m_armorSlot;
        [SerializeField] private ItemSlotView m_helmetSlot;

        [Header("인벤토리")]
        [SerializeField] private Transform m_inventoryContent;
        [SerializeField] private ItemSlotView m_slotPrefab;

        [Header("스탯 정보")]
        [SerializeField] private TextMeshProUGUI m_attackText;
        [SerializeField] private TextMeshProUGUI m_defenseText;
        [SerializeField] private TextMeshProUGUI m_healthText;
        [SerializeField] private TextMeshProUGUI m_moveSpeedText;
        [SerializeField] private TextMeshProUGUI m_goldText;

        [Header("네비게이션")]
        [SerializeField] private Button m_startGameButton;
        [SerializeField] private EasyTransition.TransitionSettings m_transitionSettings;

        private EquipmentViewModel m_viewModel;
        private UserSessionModel m_userSession;
        private SceneTransitionService m_sceneService;

        [Inject]
        public void Construct(EquipmentViewModel viewModel, UserSessionModel userSession, SceneTransitionService sceneService)
        {
            m_viewModel = viewModel;
            m_userSession = userSession;
            m_sceneService = sceneService;

            m_viewModel.OnInventoryUpdated += UpdateInventory;
            m_viewModel.OnEquippedItemUpdated += UpdateEquippedSlot;
            m_viewModel.OnStatsUpdated += UpdateStatsUI;
        }

        private void Start()
        {
            if (m_viewModel == null) return;

            // 초기 데이터 로드
            UpdateInventory(m_viewModel.ItemSlots);
            RefreshAllEquippedSlots();
            UpdateStatsUI(m_userSession?.CurrentStats ?? new StatModifiers());
            UpdateGoldUI();

            if (m_startGameButton != null)
                m_startGameButton.onClick.AddListener(OnStartGameClicked);
        }

        private void RefreshAllEquippedSlots()
        {
            UpdateEquippedSlot(EquipmentType.Weapon, m_viewModel.GetEquippedItem(EquipmentType.Weapon));
            UpdateEquippedSlot(EquipmentType.Armor, m_viewModel.GetEquippedItem(EquipmentType.Armor));
            UpdateEquippedSlot(EquipmentType.Helmet, m_viewModel.GetEquippedItem(EquipmentType.Helmet));
        }

        private void UpdateInventory(IReadOnlyList<ItemSlotViewModel> slots)
        {
            if (m_inventoryContent == null || m_slotPrefab == null) return;

            foreach (Transform child in m_inventoryContent) Destroy(child.gameObject);

            foreach (var slotVm in slots)
            {
                var slotView = Instantiate(m_slotPrefab, m_inventoryContent);
                slotView.Initialize(slotVm);
            }
        }

        private void UpdateEquippedSlot(EquipmentType type, ItemSlotViewModel slotVm)
        {
            ItemSlotView targetView = null;
            switch (type)
            {
                case EquipmentType.Weapon: targetView = m_weaponSlot; break;
                case EquipmentType.Armor: targetView = m_armorSlot; break;
                case EquipmentType.Helmet: targetView = m_helmetSlot; break;
            }

            if (targetView != null)
            {
                targetView.Initialize(slotVm);
                // [참고]: 장착 슬롯 클릭 시 해제는 ItemSlotView 내부의 버튼이 있다면 거기서 처리되거나,
                // 별도의 해제 버튼이 없다면 slotVm.Unequip()을 호출하도록 구성 가능합니다.
            }
        }

        private void UpdateStatsUI(StatModifiers stats)
        {
            SetStatText(m_attackText, "ATK", stats.Attack);
            SetStatText(m_defenseText, "DEF", stats.Defense);
            SetStatText(m_healthText, "HP", stats.Health);
            SetStatText(m_moveSpeedText, "SPD", stats.MoveSpeed);
            UpdateGoldUI();
        }

        private void SetStatText(TextMeshProUGUI tmp, string label, float value)
        {
            if (tmp != null) tmp.text = $"{label}: {value:F0}";
        }

        private void UpdateGoldUI()
        {
            if (m_goldText != null && m_userSession != null)
                m_goldText.text = m_userSession.Gold.ToString("N0");
        }

        private void OnStartGameClicked()
        {
            m_sceneService?.LoadInGame(m_transitionSettings);
        }

        private void OnDestroy()
        {
            if (m_viewModel != null)
            {
                m_viewModel.OnInventoryUpdated -= UpdateInventory;
                m_viewModel.OnEquippedItemUpdated -= UpdateEquippedSlot;
                m_viewModel.OnStatsUpdated -= UpdateStatsUI;
            }
        }
    }
}
using UnityEngine;
using UnityEngine.UI;
using TowerBreakers.UI.Equipment;
using TowerBreakers.Player.Data;
using System.Collections.Generic;
using VContainer;

namespace TowerBreakers.UI.View
{
    /// <summary>
    /// [설명]: 로비에서 장비를 관리하는 UI 뷰 클래스입니다.
    /// </summary>
    public class LobbyEquipmentView : MonoBehaviour
    {
        [Header("장착 슬롯")]
        [SerializeField] private ItemSlotView m_weaponSlot;
        [SerializeField] private ItemSlotView m_armorSlot;
        [SerializeField] private ItemSlotView m_helmetSlot;

        [Header("인벤토리")]
        [SerializeField] private Transform m_inventoryContent;
        [SerializeField] private ItemSlotView m_slotPrefab;

        private EquipmentViewModel m_viewModel;

        [Inject]
        public void Construct(EquipmentViewModel viewModel)
        {
            m_viewModel = viewModel;
            m_viewModel.OnInventoryUpdated += UpdateInventory;
            m_viewModel.OnEquippedItemUpdated += UpdateEquippedSlot;
        }

        private void Start()
        {
            if (m_viewModel == null) return;

            // 초기 데이터 로드
            UpdateInventory(m_viewModel.ItemSlots);
            RefreshAllEquippedSlots();
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
                
                // 클릭 시 장착 명령 연결
                var button = slotView.GetComponent<Button>();
                if (button != null)
                {
                    button.onClick.AddListener(() => m_viewModel.EquipItem(slotVm.ItemId));
                }
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
                // 장착 슬롯 클릭 시 해제 명령 연결
                var button = targetView.GetComponent<Button>();
                if (button != null)
                {
                    button.onClick.RemoveAllListeners();
                    if (slotVm != null) button.onClick.AddListener(() => m_viewModel.UnequipItem(type));
                }
            }
        }
    }
}
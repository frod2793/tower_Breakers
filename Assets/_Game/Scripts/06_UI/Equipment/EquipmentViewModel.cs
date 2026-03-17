using System;
using System.Collections.Generic;
using System.Linq;
using VContainer;
using TowerBreakers.Player.Data;
using TowerBreakers.Player.Model;
using TowerBreakers.Player.Service;

namespace TowerBreakers.UI.Equipment
{
    /// <summary>
    /// [기능]: 장비 UI 뷰모델
    /// </summary>
    public class EquipmentViewModel : IDisposable
    {
        private readonly UserSessionModel m_userSession;
        private readonly IEquipmentService m_equipmentService;
        private readonly List<ItemSlotViewModel> m_itemSlotViewModels = new List<ItemSlotViewModel>();

        public event Action<IReadOnlyList<ItemSlotViewModel>> OnInventoryUpdated;
        public event Action<StatModifiers> OnStatsUpdated;

        public IReadOnlyList<ItemSlotViewModel> ItemSlots => m_itemSlotViewModels;

        [Inject]
        public EquipmentViewModel(UserSessionModel userSession, IEquipmentService equipmentService)
        {
            m_userSession = userSession;
            m_equipmentService = equipmentService;

            SubscribeEvents();
            RefreshInventory();
            RefreshStats();
        }

        private void SubscribeEvents()
        {
            m_userSession.OnInventoryChanged += OnInventoryChanged;
            m_userSession.OnEquipmentChanged += OnEquipmentChanged;
            m_userSession.OnStatsChanged += OnStatsChanged;
        }

        private void UnsubscribeEvents()
        {
            m_userSession.OnInventoryChanged -= OnInventoryChanged;
            m_userSession.OnEquipmentChanged -= OnEquipmentChanged;
            m_userSession.OnStatsChanged -= OnStatsChanged;
        }

        private void OnInventoryChanged()
        {
            RefreshInventory();
        }

        private void OnEquipmentChanged(EquipmentType type, string itemId)
        {
            RefreshInventory();
            RefreshStats();
        }

        private void OnStatsChanged(StatModifiers stats)
        {
            OnStatsUpdated?.Invoke(stats);
        }

        private void RefreshInventory()
        {
            m_itemSlotViewModels.Clear();

            var inventoryItems = m_equipmentService.GetInventoryItems();
            foreach (var item in inventoryItems)
            {
                var isEquipped = m_userSession.EquippedIds.Values.Contains(item.ID);
                var slotVm = new ItemSlotViewModel(item, isEquipped, m_equipmentService);
                m_itemSlotViewModels.Add(slotVm);
            }

            OnInventoryUpdated?.Invoke(m_itemSlotViewModels);
        }

        private void RefreshStats()
        {
            var stats = m_equipmentService.CalculateTotalStats();
            OnStatsUpdated?.Invoke(stats);
        }

        public void Dispose()
        {
            UnsubscribeEvents();
        }
    }
}

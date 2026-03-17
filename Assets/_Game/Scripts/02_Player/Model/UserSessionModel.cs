using System;
using System.Collections.Generic;
using TowerBreakers.Player.Data;

namespace TowerBreakers.Player.Model
{
    /// <summary>
    /// [기능]: 사용자 세션 데이터 모델 (인벤토리 및 장비 관리)
    /// </summary>
    public class UserSessionModel
    {
        private readonly List<string> m_inventoryIds = new List<string>();
        private readonly Dictionary<EquipmentType, string> m_equippedIds = new Dictionary<EquipmentType, string>();

        public IReadOnlyList<string> InventoryIds => m_inventoryIds;
        public IReadOnlyDictionary<EquipmentType, string> EquippedIds => m_equippedIds;

        public event Action<string> OnItemAdded;
        public event Action<EquipmentType, string> OnEquipmentChanged;
        public event Action OnInventoryChanged;
        public event Action<StatModifiers> OnStatsChanged;

        public int Gold
        {
            get;
            set;
        }

        public StatModifiers CurrentStats
        {
            get;
            private set;
        }

        public UserSessionModel()
        {
            Gold = 0;
            CurrentStats = new StatModifiers();

            foreach (EquipmentType type in System.Enum.GetValues(typeof(EquipmentType)))
            {
                m_equippedIds[type] = string.Empty;
            }
        }

        public void AddItem(string itemId)
        {
            if (m_inventoryIds.Contains(itemId))
            {
                return;
            }

            m_inventoryIds.Add(itemId);
            OnItemAdded?.Invoke(itemId);
            OnInventoryChanged?.Invoke();
        }

        public void RemoveItem(string itemId)
        {
            if (m_inventoryIds.Remove(itemId))
            {
                OnInventoryChanged?.Invoke();
            }
        }

        public bool HasItem(string itemId)
        {
            return m_inventoryIds.Contains(itemId);
        }

        public void SetEquip(EquipmentType type, string itemId)
        {
            if (m_equippedIds.ContainsKey(type))
            {
                m_equippedIds[type] = itemId;
                OnEquipmentChanged?.Invoke(type, itemId);
            }
        }

        public void Unequip(EquipmentType type)
        {
            if (m_equippedIds.ContainsKey(type))
            {
                m_equippedIds[type] = string.Empty;
                OnEquipmentChanged?.Invoke(type, string.Empty);
            }
        }

        public string GetEquippedId(EquipmentType type)
        {
            return m_equippedIds.TryGetValue(type, out var id) ? id : string.Empty;
        }

        public void UpdateStats(StatModifiers totalStats)
        {
            CurrentStats = totalStats;
            OnStatsChanged?.Invoke(totalStats);
        }

        public void Clear()
        {
            m_inventoryIds.Clear();
            Gold = 0;

            foreach (EquipmentType type in System.Enum.GetValues(typeof(EquipmentType)))
            {
                m_equippedIds[type] = string.Empty;
            }

            CurrentStats = new StatModifiers();
            OnInventoryChanged?.Invoke();
        }
    }
}

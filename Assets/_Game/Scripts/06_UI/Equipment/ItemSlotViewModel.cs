using System;
using TowerBreakers.Player.Data;
using TowerBreakers.Player.Service;
using UnityEngine;

namespace TowerBreakers.UI.Equipment
{
    /// <summary>
    /// [기능]: 아이템 슬롯 뷰모델
    /// </summary>
    public class ItemSlotViewModel
    {
        private readonly EquipmentData m_data;
        private readonly bool m_isEquipped;
        private readonly IEquipmentService m_equipmentService;

        public string ItemId => m_data.ID;
        public string ItemName => m_data.ItemName;
        public string Description => m_data.Description;
        public EquipmentType Type => m_data.Type;
        public int Grade => m_data.Grade;
        public Sprite Icon => m_data.Icon;
        public StatModifiers Stats => m_data.Stats;
        public bool IsEquipped => m_isEquipped;

        public event Action<string> OnEquipClicked;

        public ItemSlotViewModel(EquipmentData data, bool isEquipped, IEquipmentService equipmentService)
        {
            m_data = data;
            m_isEquipped = isEquipped;
            m_equipmentService = equipmentService;
        }

        public void Equip()
        {
            if (m_equipmentService.HasItem(ItemId))
            {
                m_equipmentService.Equip(ItemId);
                OnEquipClicked?.Invoke(ItemId);
            }
        }

        public void Unequip()
        {
            m_equipmentService.Unequip(Type);
        }

        public string GetStatSummary()
        {
            var stats = m_data.Stats;
            var summary = string.Empty;

            if (stats.Attack != 0)
                summary += $"공격력: +{stats.Attack}\n";
            if (stats.Defense != 0)
                summary += $"방어력: +{stats.Defense}\n";
            if (stats.Health != 0)
                summary += $"체력: +{stats.Health}\n";
            if (stats.MoveSpeed != 0)
                summary += $"이동속도: +{stats.MoveSpeed}\n";
            if (stats.CritRate != 0)
                summary += $"치명타: +{stats.CritRate}%\n";
            if (stats.CritDamage != 0)
                summary += $"치명타 피해: +{stats.CritDamage}%";

            return summary.TrimEnd();
        }
    }
}

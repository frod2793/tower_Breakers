using System;
using System.Collections.Generic;
using TowerBreakers.Player.Data;

namespace TowerBreakers.Player.Service
{
    /// <summary>
    /// [기능]: 장비 서비스 인터페이스
    /// </summary>
    public interface IEquipmentService
    {
        void Equip(string itemId);
        void Unequip(EquipmentType type);
        bool HasItem(string itemId);
        EquipmentData GetEquipmentData(string itemId);
        IReadOnlyList<EquipmentData> GetAllEquipmentData();
        IReadOnlyList<EquipmentData> GetInventoryItems();
        EquipmentData GetEquippedItem(EquipmentType type);
        StatModifiers CalculateTotalStats();
    }
}

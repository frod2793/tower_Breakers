using System;
using System.Collections.Generic;
using UnityEngine;
using TowerBreakers.Player.Data;
using TowerBreakers.Player.Model;

namespace TowerBreakers.Player.Service
{
    /// <summary>
    /// [기능]: 장비 서비스 구현체
    /// </summary>
    public class EquipmentService : IEquipmentService
    {
        private readonly UserSessionModel m_userSession;
        private readonly EquipmentDatabase m_database;
        private readonly Dictionary<string, EquipmentData> m_equipmentCache = new Dictionary<string, EquipmentData>();

        public EquipmentService(UserSessionModel userSession, EquipmentDatabase database)
        {
            m_userSession = userSession;
            m_database = database;
            LoadEquipmentDatabase();
        }

        private void LoadEquipmentDatabase()
        {
            if (m_database == null)
            {
                Debug.LogWarning("[EquipmentService] 데이터베이스가 없습니다.");
                return;
            }

            if (m_database.Weapons != null)
            {
                foreach (var weapon in m_database.Weapons)
                {
                    if (weapon != null)
                    {
                        m_equipmentCache[weapon.ID] = weapon;
                    }
                }
            }

            if (m_database.Armors != null)
            {
                foreach (var armor in m_database.Armors)
                {
                    if (armor != null)
                    {
                        m_equipmentCache[armor.ID] = armor;
                    }
                }
            }

            if (m_database.Helmets != null)
            {
                foreach (var helmet in m_database.Helmets)
                {
                    if (helmet != null)
                    {
                        m_equipmentCache[helmet.ID] = helmet;
                    }
                }
            }
        }

        public void Equip(string itemId)
        {
            var data = GetEquipmentData(itemId);
            if (data == null)
            {
                Debug.LogWarning($"[EquipmentService] 존재하지 않는 아이템입니다: {itemId}");
                return;
            }

            if (!m_userSession.HasItem(itemId))
            {
                Debug.LogWarning($"[EquipmentService] 인벤토리에 없는 아이템입니다: {itemId}");
                return;
            }

            m_userSession.SetEquip(data.Type, itemId);
            UpdateTotalStats();

            Debug.Log($"[EquipmentService] 장비 교체: {data.Type} -> {data.ItemName}");
        }

        public void Unequip(EquipmentType type)
        {
            m_userSession.Unequip(type);
            UpdateTotalStats();

            Debug.Log($"[EquipmentService] 장비 해제: {type}");
        }

        public bool HasItem(string itemId)
        {
            return m_userSession.HasItem(itemId);
        }

        public EquipmentData GetEquipmentData(string itemId)
        {
            if (string.IsNullOrEmpty(itemId))
            {
                return null;
            }

            if (m_equipmentCache.TryGetValue(itemId, out var data))
            {
                return data;
            }

            return null;
        }

        public IReadOnlyList<EquipmentData> GetAllEquipmentData()
        {
            var result = new List<EquipmentData>();
            foreach (var kvp in m_equipmentCache)
            {
                result.Add(kvp.Value);
            }
            return result;
        }

        public IReadOnlyList<EquipmentData> GetInventoryItems()
        {
            var result = new List<EquipmentData>();
            foreach (var itemId in m_userSession.InventoryIds)
            {
                var data = GetEquipmentData(itemId);
                if (data != null)
                {
                    result.Add(data);
                }
            }
            return result;
        }

        public EquipmentData GetEquippedItem(EquipmentType type)
        {
            var itemId = m_userSession.GetEquippedId(type);
            return GetEquipmentData(itemId);
        }

        public StatModifiers CalculateTotalStats()
        {
            var total = new StatModifiers();

            foreach (var kvp in m_userSession.EquippedIds)
            {
                if (!string.IsNullOrEmpty(kvp.Value))
                {
                    var data = GetEquipmentData(kvp.Value);
                    if (data != null && data.Stats != null)
                    {
                        total += data.Stats;
                    }
                }
            }

            return total;
        }

        private void UpdateTotalStats()
        {
            var totalStats = CalculateTotalStats();
            m_userSession.UpdateStats(totalStats);

            Debug.Log($"[EquipmentService] 스탯 업데이트 - 공격력: {totalStats.Attack}, 방어력: {totalStats.Defense}, 체력: {totalStats.Health}");
        }
    }
}

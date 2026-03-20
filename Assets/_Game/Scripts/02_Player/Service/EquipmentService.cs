using System;
using System.Collections.Generic;
using UnityEngine;
using TowerBreakers.Player.Data;
using TowerBreakers.Player.Model;
using TowerBreakers.Core.Service;

namespace TowerBreakers.Player.Service
{
    /// <summary>
    /// [기능]: 장비 서비스 구현체 (저장 시스템 연동)
    /// </summary>
    public class EquipmentService : IEquipmentService
    {
        public event Action<EquipmentType, EquipmentData> OnEquipmentChanged;
        
        private readonly UserSessionModel m_userSession;
        private readonly EquipmentDatabase m_database;
        private readonly DataPersistenceService m_persistenceService;
        private readonly Dictionary<string, EquipmentData> m_equipmentCache = new Dictionary<string, EquipmentData>();

        public EquipmentService(UserSessionModel userSession, EquipmentDatabase database, DataPersistenceService persistenceService)
        {
            m_userSession = userSession;
            m_database = database;
            m_persistenceService = persistenceService;

            LoadEquipmentDatabase();
            
            // 최초 데이터 로드
            if (m_persistenceService != null)
            {
                m_persistenceService.Load(m_userSession);
            }

            // 아이템 획득 시 자동 저장 구독
            m_userSession.OnItemAdded += (id) => SaveData();
        }

        private void LoadEquipmentDatabase()
        {
            if (m_database == null) return;

            AddListToCache(m_database.Weapons);
            AddListToCache(m_database.Armors);
            AddListToCache(m_database.Helmets);
        }

        private void AddListToCache(List<EquipmentData> list)
        {
            if (list == null) return;
            foreach (var item in list)
            {
                if (item != null) m_equipmentCache[item.ID] = item;
            }
        }

        public void Equip(string itemId)
        {
            var data = GetEquipmentData(itemId);
            if (data == null || !m_userSession.HasItem(itemId)) return;

            m_userSession.SetEquip(data.Type, itemId);
            UpdateTotalStats();
            OnEquipmentChanged?.Invoke(data.Type, data);
            SaveData();

            Debug.Log($"[EquipmentService] 장비 교체: {data.ItemName}");
        }

        public void Unequip(EquipmentType type)
        {
            m_userSession.Unequip(type);
            UpdateTotalStats();
            SaveData();

            Debug.Log($"[EquipmentService] 장비 해제: {type}");
        }

        public void SaveData()
        {
            m_persistenceService?.Save(m_userSession);
        }

        public bool HasItem(string itemId) => m_userSession.HasItem(itemId);

        public EquipmentData GetEquipmentData(string itemId)
        {
            if (string.IsNullOrEmpty(itemId)) return null;
            return m_equipmentCache.TryGetValue(itemId, out var data) ? data : null;
        }

        public IReadOnlyList<EquipmentData> GetAllEquipmentData() => new List<EquipmentData>(m_equipmentCache.Values);

        public IReadOnlyList<EquipmentData> GetInventoryItems()
        {
            var result = new List<EquipmentData>();
            foreach (var itemId in m_userSession.InventoryIds)
            {
                var data = GetEquipmentData(itemId);
                if (data != null) result.Add(data);
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
                var data = GetEquipmentData(kvp.Value);
                if (data != null && data.Stats != null) total += data.Stats;
            }
            return total;
        }

        private void UpdateTotalStats()
        {
            var totalStats = CalculateTotalStats();
            m_userSession.UpdateStats(totalStats);
        }
    }
}
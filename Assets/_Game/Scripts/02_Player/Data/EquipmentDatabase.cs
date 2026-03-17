using UnityEngine;
using System.Collections.Generic;
using TowerBreakers.Player.Data;

namespace TowerBreakers.Player.Data
{
    /// <summary>
    /// [기능]: 장비 데이터베이스 ScriptableObject
    /// </summary>
    [CreateAssetMenu(fileName = "EquipmentDatabase", menuName = "Data/EquipmentDatabase")]
    public class EquipmentDatabase : ScriptableObject
    {
        [Header("무기")]
        [Tooltip("무기 목록")]
        [SerializeField] private List<EquipmentData> m_weapons;

        [Header("방어구")]
        [Tooltip("방어구 목록")]
        [SerializeField] private List<EquipmentData> m_armors;

        [Header("투구")]
        [Tooltip("투구 목록")]
        [SerializeField] private List<EquipmentData> m_helmets;

        public List<EquipmentData> Weapons => m_weapons ?? new List<EquipmentData>();
        public List<EquipmentData> Armors => m_armors ?? new List<EquipmentData>();
        public List<EquipmentData> Helmets => m_helmets ?? new List<EquipmentData>();

        public static EquipmentDatabase LoadFromResources()
        {
            var database = Resources.Load<EquipmentDatabase>("Data/EquipmentDatabase");
            if (database == null)
            {
                Debug.LogWarning("[EquipmentDatabase] Resources에서 찾을 수 없습니다.");
            }
            return database;
        }

        public EquipmentData GetWeaponById(string id)
        {
            if (m_weapons == null) return null;
            return m_weapons.Find(w => w != null && w.ID == id);
        }

        public EquipmentData GetArmorById(string id)
        {
            if (m_armors == null) return null;
            return m_armors.Find(a => a != null && a.ID == id);
        }

        public EquipmentData GetHelmetById(string id)
        {
            if (m_helmets == null) return null;
            return m_helmets.Find(h => h != null && h.ID == id);
        }

        public EquipmentData GetEquipmentById(string id)
        {
            return GetWeaponById(id) ?? GetArmorById(id) ?? GetHelmetById(id);
        }
    }
}

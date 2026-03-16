using System.Collections.Generic;
using UnityEngine;
using TowerBreakers.Player.Data.SO;
using System.Linq;

namespace TowerBreakers.Player.Data
{
    /// <summary>
    /// [설명]: 게임 내 모든 장비(무기, 갑주) ScriptableObject를 ID 기반으로 관리하는 데이터베이스입니다.
    /// DTO의 문자열 ID를 실제 데이터 객체로 변환하는 역할을 수행합니다.
    /// </summary>
    [CreateAssetMenu(fileName = "EquipmentDatabase", menuName = "TowerBreakers/Data/Equipment Database")]
    public class EquipmentDatabase : ScriptableObject
    {
        #region 에디터 설정
        [Header("무기 목록")]
        [SerializeField] private List<WeaponData> m_weapons = new List<WeaponData>();

        [Header("갑주 목록")]
        [SerializeField] private List<ArmorData> m_armors = new List<ArmorData>();
        #endregion

        #region 내부 데이터 캐시
        private Dictionary<string, WeaponData> m_weaponCache;
        private Dictionary<string, ArmorData> m_armorCache;
        #endregion

        #region 공개 메서드
        /// <summary>
        /// [설명]: ID를 기반으로 무기 데이터를 반환합니다.
        /// 캐시가 비어있을 경우 Resources/Data/Equipment/Weapons 경로에서 자동 로드를 시도합니다.
        /// </summary>
        public WeaponData GetWeapon(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            EnsureCache();
            return m_weaponCache.TryGetValue(id, out var weapon) ? weapon : null;
        }

        /// <summary>
        /// [설명]: ID를 기반으로 갑주 데이터를 찾아 반환합니다.
        /// </summary>
        public ArmorData GetArmor(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            EnsureCache();
            return m_armorCache.TryGetValue(id, out var armor) ? armor : null;
        }

        /// <summary>
        /// [설명]: 특정 무기의 고유 ID를 반환합니다. (ScriptableObject 이름 기반)
        /// </summary>
        public string GetWeaponId(WeaponData weapon)
        {
            return weapon != null ? weapon.name : string.Empty;
        }

        /// <summary>
        /// [설명]: 특정 갑주의 고유 ID를 반환합니다. (ScriptableObject 이름 기반)
        /// </summary>
        public string GetArmorId(ArmorData armor)
        {
            return armor != null ? armor.name : string.Empty;
        }

        /// <summary>
        /// [설명]: 전체 무기 목록을 반환합니다.
        /// </summary>
        public IReadOnlyList<WeaponData> GetAllWeapons()
        {
            EnsureCache();
            return m_weaponCache?.Values.ToList() ?? new List<WeaponData>();
        }

        /// <summary>
        /// [설명]: 전체 갑주 목록을 반환합니다.
        /// </summary>
        public IReadOnlyList<ArmorData> GetAllArmors()
        {
            EnsureCache();
            return m_armorCache?.Values.ToList() ?? new List<ArmorData>();
        }
        #endregion

        #region 내부 로직
        private void EnsureCache()
        {
            if (m_weaponCache == null)
            {
                // [방어 코드]: 리스트가 비어있을 경우 Resources에서 자동 검색 시도
                if (m_weapons == null || m_weapons.Count == 0)
                {
                    m_weapons = new List<WeaponData>(Resources.LoadAll<WeaponData>("Data/Equipment/Weapons"));
                    if (m_weapons.Count > 0) Debug.Log($"[EquipmentDatabase] Resources에서 {m_weapons.Count}개의 무기 데이터를 로드했습니다.");
                }

                m_weaponCache = m_weapons.Where(w => w != null).ToDictionary(w => w.name, w => w);
                Debug.Log($"[EquipmentDatabase] 무기 캐시 생성 완료 ({m_weaponCache.Count}종): {string.Join(", ", m_weaponCache.Keys)}");
            }

            if (m_armorCache == null)
            {
                // [방어 코드]: 리스트가 비어있을 경우 Resources에서 자동 검색 시도
                if (m_armors == null || m_armors.Count == 0)
                {
                    m_armors = new List<ArmorData>(Resources.LoadAll<ArmorData>("Data/Equipment/Armors"));
                    if (m_armors.Count > 0) Debug.Log($"[EquipmentDatabase] Resources에서 {m_armors.Count}개의 갑주 데이터를 로드했습니다.");
                }

                m_armorCache = m_armors.Where(a => a != null).ToDictionary(a => a.name, a => a);
                Debug.Log($"[EquipmentDatabase] 갑주 캐시 생성 완료 ({m_armorCache.Count}종): {string.Join(", ", m_armorCache.Keys)}");
            }
        }
        #endregion
    }
}

using System;
using System.Collections.Generic;
using System.Linq;

namespace TowerBreakers.Player.Data
{
    /// <summary>
    /// [설명]: 씬 전환 및 세이브 데이터 보관을 위한 장비 데이터 전송 객체(DTO)입니다.
    /// ScriptableObject 참조 대신 문자열 ID를 사용하여 직렬화 안정성을 확보합니다.
    /// </summary>
    [Serializable]
    public class EquipmentDTO
    {
        #region 장착 데이터
        /// <summary>
        /// [설명]: 현재 장착 중인 무기의 고유 ID입니다.
        /// </summary>
        public string WeaponId;

        /// <summary>
        /// [설명]: 현재 장착 중인 헬멧의 고유 ID입니다.
        /// </summary>
        public string HelmetId;

        /// <summary>
        /// [설명]: 현재 장착 중인 흉갑의 고유 ID입니다.
        /// </summary>
        public string BodyArmorId;
        #endregion

        #region 보유 목록 데이터
        /// <summary>
        /// [설명]: 플레이어가 보유 중인 모든 무기의 ID 목록입니다.
        /// </summary>
        public List<string> OwnedWeaponIds = new List<string>();

        /// <summary>
        /// [설명]: 플레이어가 보유 중인 모든 갑주의 ID 목록입니다.
        /// </summary>
        public List<string> OwnedArmorIds = new List<string>();
        #endregion

        #region 생성자
        public EquipmentDTO()
        {
            WeaponId = string.Empty;
            HelmetId = string.Empty;
            BodyArmorId = string.Empty;
            OwnedWeaponIds = new List<string>();
            OwnedArmorIds = new List<string>();
        }

        public EquipmentDTO(string weaponId, string helmetId, string bodyArmorId)
        {
            WeaponId = weaponId;
            HelmetId = helmetId;
            BodyArmorId = bodyArmorId;
            OwnedWeaponIds = new List<string>();
            OwnedArmorIds = new List<string>();
        }
        #endregion

        #region 헬퍼 메서드
        public EquipmentDTO Clone()
        {
            var clone = new EquipmentDTO(WeaponId, HelmetId, BodyArmorId);
            clone.OwnedWeaponIds = new List<string>(OwnedWeaponIds);
            clone.OwnedArmorIds = new List<string>(OwnedArmorIds);
            return clone;
        }

        public bool HasWeapon(string weaponId)
        {
            return !string.IsNullOrEmpty(weaponId) && OwnedWeaponIds.Contains(weaponId);
        }

        public bool HasArmor(string armorId)
        {
            return !string.IsNullOrEmpty(armorId) && OwnedArmorIds.Contains(armorId);
        }

        public void AddWeapon(string weaponId)
        {
            if (!string.IsNullOrEmpty(weaponId) && !OwnedWeaponIds.Contains(weaponId))
            {
                OwnedWeaponIds.Add(weaponId);
            }
        }

        public void AddArmor(string armorId)
        {
            if (!string.IsNullOrEmpty(armorId) && !OwnedArmorIds.Contains(armorId))
            {
                OwnedArmorIds.Add(armorId);
            }
        }

        public int GetTotalItemCount()
        {
            return OwnedWeaponIds.Count + OwnedArmorIds.Count;
        }

        public bool IsEmpty()
        {
            return string.IsNullOrEmpty(WeaponId) &&
                   string.IsNullOrEmpty(HelmetId) &&
                   string.IsNullOrEmpty(BodyArmorId) &&
                   OwnedWeaponIds.Count == 0 &&
                   OwnedArmorIds.Count == 0;
        }
        #endregion
    }
}

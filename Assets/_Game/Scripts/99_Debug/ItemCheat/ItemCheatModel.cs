using System.Collections.Generic;
using TowerBreakers.Player.Data;
using TowerBreakers.Player.Data.SO;
using UnityEngine;

namespace TowerBreakers.DevTools
{
    /// <summary>
    /// [설명]: 아이템 치트 기능의 핵심 비즈니스 로직을 담당하는 모델 클래스입니다.
    /// 장비 데이터베이스에서 아이템 정보를 가져오고, 유저 세션에 아이템을 추가합니다.
    /// </summary>
    public class ItemCheatModel
    {
        #region 내부 필드
        private readonly EquipmentDatabase m_equipmentDatabase;
        private readonly UserSessionModel m_sessionModel;
        #endregion

        #region 생성자
        /// <summary>
        /// [설명]: 필요한 의존성을 주입받아 모델을 생성합니다.
        /// </summary>
        /// <param name="equipmentDatabase">장비 데이터베이스</param>
        /// <param name="sessionModel">유저 세션 모델</param>
        public ItemCheatModel(EquipmentDatabase equipmentDatabase, UserSessionModel sessionModel)
        {
            m_equipmentDatabase = equipmentDatabase;
            m_sessionModel = sessionModel;
        }
        #endregion

        #region 공개 메서드
        /// <summary>
        /// [설명]: 전체 무기 목록을 데이터베이스에서 가져옵니다.
        /// </summary>
        /// <returns>사용 가능한 무기 데이터 리스트</returns>
        public IReadOnlyList<WeaponData> GetAvailableWeapons()
        {
            var weapons = new List<WeaponData>();
            
            if (m_equipmentDatabase != null)
            {
                var allWeapons = m_equipmentDatabase.GetAllWeapons();
                if (allWeapons != null)
                {
                    weapons.AddRange(allWeapons);
                }
            }
            
            return weapons;
        }

        /// <summary>
        /// [설명]: 전체 갑주 목록을 데이터베이스에서 가져옵니다.
        /// </summary>
        /// <returns>사용 가능한 갑주 데이터 리스트</returns>
        public IReadOnlyList<ArmorData> GetAvailableArmors()
        {
            var armors = new List<ArmorData>();
            
            if (m_equipmentDatabase != null)
            {
                var allArmors = m_equipmentDatabase.GetAllArmors();
                if (allArmors != null)
                {
                    armors.AddRange(allArmors);
                }
            }
            
            return armors;
        }

        /// <summary>
        /// [설명]: 특정 ID의 무기를 플레이어 인벤토리에 추가합니다.
        /// </summary>
        /// <param name="id">무기 고유 ID</param>
        public void AcquireWeapon(string id)
        {
            if (string.IsNullOrEmpty(id)) return;
            
            if (m_equipmentDatabase != null)
            {
                var weapon = m_equipmentDatabase.GetWeapon(id);
                if (weapon != null)
                {
                    m_sessionModel?.AddOwnedWeapon(id);
                    Debug.Log($"<color=cyan>[ItemCheat] 무기 획득: {weapon.WeaponName} ({id})</color>");
                }
                else
                {
                    Debug.LogWarning($"<color=yellow>[ItemCheat] 무기 ID를 찾을 수 없음: {id}</color>");
                }
            }
        }

        /// <summary>
        /// [설명]: 특정 ID의 갑주를 플레이어 인벤토리에 추가합니다.
        /// </summary>
        /// <param name="id">갑주 고유 ID</param>
        public void AcquireArmor(string id)
        {
            if (string.IsNullOrEmpty(id)) return;
            
            if (m_equipmentDatabase != null)
            {
                var armor = m_equipmentDatabase.GetArmor(id);
                if (armor != null)
                {
                    m_sessionModel?.AddOwnedArmor(id);
                    Debug.Log($"<color=cyan>[ItemCheat] 갑주 획득: {armor.ArmorName} ({id})</color>");
                }
                else
                {
                    Debug.LogWarning($"<color=yellow>[ItemCheat] 갑주 ID를 찾을 수 없음: {id}</color>");
                }
            }
        }
        #endregion
    }
}


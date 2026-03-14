using UnityEngine;
using System;
using System.Collections.Generic;
using TowerBreakers.Player.Data;

namespace TowerBreakers.Player.Data.SO
{
    [Serializable]
    public class RewardEntry
    {
        [Tooltip("드롭 확률 가중치")]
        public float Weight;

        [Tooltip("드롭할 무기 데이터 (무기인 경우 할당)")]
        public WeaponData Weapon;

        [Tooltip("드롭할 갑주 데이터 (갑주인 경우 할당)")]
        public ArmorData Armor;

        public bool IsWeapon => Weapon != null;
        public bool IsArmor => Armor != null;
    }

    /// <summary>
    /// [설명]: 보상 상자에서 드롭될 수 있는 아이템들과 그 확률(가중치)을 관리하는 ScriptableObject입니다.
    /// </summary>
    [CreateAssetMenu(fileName = "NewRewardTable", menuName = "TowerBreakers/Reward Table")]
    public class RewardTableData : ScriptableObject
    {
        #region 에디터 설정
        [SerializeField] private List<RewardEntry> m_rewardEntries = new List<RewardEntry>();
        #endregion

        #region 공개 메서드
        /// <summary>
        /// [설명]: 가중치 기반으로 랜덤하게 보상 아이템 하나를 선택하여 반환합니다.
        /// </summary>
        /// <returns>선택된 보상 엔트리. 목록이 비어 있으면 null 반환.</returns>
        public RewardEntry GetRandomReward()
        {
            if (m_rewardEntries == null || m_rewardEntries.Count == 0) return null;

            float totalWeight = 0;
            foreach (var entry in m_rewardEntries)
            {
                totalWeight += entry.Weight;
            }

            float randomValue = UnityEngine.Random.Range(0, totalWeight);
            float currentWeight = 0;

            foreach (var entry in m_rewardEntries)
            {
                currentWeight += entry.Weight;
                if (randomValue <= currentWeight)
                {
                    return entry;
                }
            }

            return m_rewardEntries[m_rewardEntries.Count - 1];
        }

        /// <summary>
        /// [설명]: 보상 이름(Key)을 기반으로 해당하는 아이콘 스프라이트를 찾아 반환합니다.
        /// </summary>
        /// <param name="rewardKey">찾으려는 보상의 이름</param>
        /// <returns>찾은 스프라이트. 없으면 null 반환.</returns>
        public Sprite GetSprite(string rewardKey)
        {
            if (string.IsNullOrEmpty(rewardKey) || m_rewardEntries == null) return null;

            foreach (var entry in m_rewardEntries)
            {
                if (entry.IsWeapon && entry.Weapon.WeaponName == rewardKey)
                {
                    return entry.Weapon.Icon;
                }
                if (entry.IsArmor && entry.Armor.ArmorName == rewardKey)
                {
                    return entry.Armor.Icon;
                }
            }

            return null;
        }
        #endregion
    }
}

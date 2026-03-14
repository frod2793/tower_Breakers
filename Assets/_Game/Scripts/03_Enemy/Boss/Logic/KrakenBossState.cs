using System.Collections.Generic;
using TowerBreakers.Core.Events;
using UnityEngine;

namespace TowerBreakers.Enemy.Logic
{
    /// <summary>
    /// [설명]: 크라켄 보스의 런타임 상태를 관리하는 POCO 클래스입니다.
    /// 촉수 개수 등 실시간 데이터를 유지하며 패턴 선택에 활용됩니다.
    /// </summary>
    public class KrakenBossState
    {
        #region 내부 빌드
        private readonly Dictionary<int, int> m_tentacleCounts = new();
        private int m_totalTentacleCount = 0;
        private int m_playerFloorIndex = 0;
        #endregion

        #region 프로퍼티
        public int TotalTentacleCount => m_totalTentacleCount;
        public int PlayerFloorIndex => m_playerFloorIndex;
        #endregion

        #region 공개 메서드
        /// <summary>
        /// [설명]: 특정 층의 촉수 개수를 반환합니다.
        /// </summary>
        public int GetTentacleCount(int floorIndex)
        {
            return m_tentacleCounts.TryGetValue(floorIndex, out int count) ? count : 0;
        }

        /// <summary>
        /// [설명]: 촉수가 소환되었을 때 카운트를 증가시킵니다.
        /// </summary>
        public void IncrementTentacleCount(int floorIndex)
        {
            if (!m_tentacleCounts.ContainsKey(floorIndex))
                m_tentacleCounts[floorIndex] = 0;

            m_tentacleCounts[floorIndex]++;
            m_totalTentacleCount++;
            
            Debug.Log($"[KrakenBossState] 촉수 증가: 층={floorIndex}, 총계={m_totalTentacleCount}");
        }

        /// <summary>
        /// [설명]: 촉수가 파괴되었을 때 카운트를 감소시킵니다.
        /// </summary>
        public void DecrementTentacleCount(int floorIndex)
        {
            if (m_tentacleCounts.TryGetValue(floorIndex, out int count) && count > 0)
            {
                m_tentacleCounts[floorIndex]--;
                m_totalTentacleCount--;
                Debug.Log($"[KrakenBossState] 촉수 감소: 층={floorIndex}, 총계={m_totalTentacleCount}");
            }
        }

        /// <summary>
        /// [설명]: 플레이어의 현재 층 위치를 갱신합니다.
        /// </summary>
        public void SetPlayerFloorIndex(int index)
        {
            m_playerFloorIndex = index;
            Debug.Log($"[KrakenBossState] 플레이어 위치 인식: 층={m_playerFloorIndex}");
        }
        #endregion
    }
}

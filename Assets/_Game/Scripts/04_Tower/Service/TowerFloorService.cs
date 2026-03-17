using System;
using UnityEngine;
using TowerBreakers.Tower.Data;

namespace TowerBreakers.Tower.Service
{
    /// <summary>
    /// [기능]: 타워 층 관리 서비스
    /// </summary>
    public class TowerFloorService
    {
        private TowerData m_towerData;
        private int m_currentFloor = 1;
        private int m_aliveEnemyCount = 0;

        public event Action<int> OnFloorChanged;
        public event Action<int> OnEnemyCountChanged;
        public event Action OnAllEnemiesCleared;
        public event Action OnTowerCompleted;

        public int CurrentFloor => m_currentFloor;
        public int AliveEnemyCount => m_aliveEnemyCount;
        public int TotalFloors => m_towerData?.TotalFloors ?? 0;

        public void Initialize(TowerData towerData)
        {
            m_towerData = towerData;
            m_currentFloor = towerData != null ? towerData.StartFloor : 1;
            m_aliveEnemyCount = 0;

            Debug.Log($"[TowerFloorService] 초기화 완료 - 시작 층: {m_currentFloor}");
        }

        public FloorData GetCurrentFloorData()
        {
            return m_towerData?.GetFloor(m_currentFloor);
        }

        public FloorData GetFloorData(int floorNumber)
        {
            return m_towerData?.GetFloor(floorNumber);
        }

        public void SetEnemyCount(int count)
        {
            m_aliveEnemyCount = count;
            OnEnemyCountChanged?.Invoke(m_aliveEnemyCount);

            Debug.Log($"[TowerFloorService] 적 수 설정: {m_aliveEnemyCount}");
        }

        public void RegisterEnemyDeath()
        {
            if (m_aliveEnemyCount > 0)
            {
                m_aliveEnemyCount--;
                OnEnemyCountChanged?.Invoke(m_aliveEnemyCount);

                Debug.Log($"[TowerFloorService] 적 처치 - 남은 수: {m_aliveEnemyCount}");

                if (m_aliveEnemyCount <= 0)
                {
                    OnAllEnemiesCleared?.Invoke();
                }
            }
        }

        public bool IsLastFloor()
        {
            return m_towerData != null && m_towerData.IsLastFloor(m_currentFloor);
        }

        public void MoveToNextFloor()
        {
            if (IsLastFloor())
            {
                Debug.Log("[TowerFloorService] 타워 클리어!");
                OnTowerCompleted?.Invoke();
                return;
            }

            m_currentFloor++;
            OnFloorChanged?.Invoke(m_currentFloor);

            Debug.Log($"[TowerFloorService] 다음 층 이동: {m_currentFloor}");
        }

        public FloorRewardData GetCurrentFloorReward()
        {
            var floor = GetCurrentFloorData();
            return floor != null ? floor.ClearReward : null;
        }

        public void Reset()
        {
            m_currentFloor = m_towerData != null ? m_towerData.StartFloor : 1;
            m_aliveEnemyCount = 0;
        }
    }
}

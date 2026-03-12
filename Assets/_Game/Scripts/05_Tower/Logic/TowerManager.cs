using UnityEngine;
using TowerBreakers.Core.Events;
using TowerBreakers.Tower.Data;
using System.Collections.Generic;

namespace TowerBreakers.Tower.Logic
{
    /// <summary>
    /// [설명]: 타워의 진행 상태(현재 층 등)를 관리하는 매니저 클래스입니다.
    /// </summary>
    public class TowerManager
    {
        #region 내부 필드
        private readonly IEventBus m_eventBus;
        private TowerData m_currentTower;
        private int m_currentFloorIndex = 0;
        #endregion

        #region 프로퍼티
        public int CurrentFloorIndex => m_currentFloorIndex;
        public FloorData CurrentFloorData => (m_currentTower != null && m_currentFloorIndex < m_currentTower.Floors.Count) ? m_currentTower.Floors[m_currentFloorIndex] : null;
        public GameObject CommonGroundPrefab => m_currentTower != null ? m_currentTower.GroundPrefab : null;
        public bool IsFinished => m_currentTower != null && m_currentFloorIndex >= m_currentTower.TotalFloors;
        #endregion

        public TowerManager(IEventBus eventBus, TowerData towerData)
        {
            m_eventBus = eventBus;
            m_currentTower = towerData;
            m_currentFloorIndex = 0;
            Debug.Log($"[TowerManager] '{towerData?.TowerName}' 데이터로 초기화 완료");
        }

        /// <summary>
        /// [설명]: 다음 층으로 진행합니다.
        /// </summary>
        public void NextFloor()
        {
            m_currentFloorIndex++;
            if (!IsFinished)
            {
                m_eventBus.Publish(new OnFloorCleared(m_currentFloorIndex));
            }
        }

        /// <summary>
        /// [설명]: 타워의 전체 층 데이터 목록을 반환합니다.
        /// </summary>
        public IReadOnlyList<FloorData> GetFloorsList()
        {
            if (m_currentTower == null) return new List<FloorData>();
            return m_currentTower.Floors;
        }
    }
}

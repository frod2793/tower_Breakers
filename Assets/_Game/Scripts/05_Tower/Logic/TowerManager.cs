using UnityEngine;
using TowerBreakers.Core.Events;
using TowerBreakers.Tower.Data;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

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
        private readonly Dictionary<int, int> m_activeEnemiesPerFloor = new Dictionary<int, int>();
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

            m_eventBus.Subscribe<OnEnemyKilled>(HandleEnemyKilled);
            Debug.Log($"[TowerManager] '{towerData?.TowerName}' 데이터로 초기화 완료");
        }

        /// <summary>
        /// [설명]: 다음 층으로 진행합니다.
        /// </summary>
        public void NextFloor()
        {
            if (IsFinished) return;

            m_currentFloorIndex++;
            Debug.Log($"[TowerManager] {m_currentFloorIndex}층으로 이동");
            m_eventBus.Publish(new OnFloorCleared(m_currentFloorIndex));
        }

        /// <summary>
        /// [설명]: 특정 층에 스폰된 적의 수를 등록합니다.
        /// </summary>
        public void RegisterEnemies(int floorIndex, int count)
        {
            if (!m_activeEnemiesPerFloor.ContainsKey(floorIndex))
                m_activeEnemiesPerFloor[floorIndex] = 0;
            
            m_activeEnemiesPerFloor[floorIndex] += count;
            Debug.Log($"[TowerManager] Floor {floorIndex} 적 등록: {count}명 (합계: {m_activeEnemiesPerFloor[floorIndex]})");
        }

        private void HandleEnemyKilled(OnEnemyKilled evt)
        {
            if (m_activeEnemiesPerFloor.ContainsKey(evt.FloorIndex))
            {
                m_activeEnemiesPerFloor[evt.FloorIndex]--;
                Debug.Log($"[TowerManager] Floor {evt.FloorIndex} 적 처치. 남은 수: {m_activeEnemiesPerFloor[evt.FloorIndex]}");

                // 현재 진행 중인 층의 모든 적을 처치했다면 1초 후 다음 층 준비 이벤트 발행
                if (evt.FloorIndex == m_currentFloorIndex && m_activeEnemiesPerFloor[evt.FloorIndex] <= 0)
                {
                    HandleFloorClearedDelayedAsync().Forget();
                }
            }
        }

        /// <summary>
        /// [설명]: 적 처치 완료 후 1초 대기 후에 'GO' 대기 상태로 전환합니다.
        /// </summary>
        private async UniTaskVoid HandleFloorClearedDelayedAsync()
        {
            Debug.Log($"[TowerManager] Floor {m_currentFloorIndex} 처치 완료! 1초 후 'GO' 표시 예정");
            await UniTask.Delay(1000);
            m_eventBus.Publish(new OnFloorReadyForNext());
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

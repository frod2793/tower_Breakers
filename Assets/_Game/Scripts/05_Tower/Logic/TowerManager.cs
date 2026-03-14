using UnityEngine;
using TowerBreakers.Core.Events;
using TowerBreakers.Tower.Data;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace TowerBreakers.Tower.Logic
{
    /// <summary>
    /// [설명]: 타워의 진행 상태(현재 층, 적 처치 수 등)를 관리하는 매니저 클래스입니다.
    /// POCO 클래스로 작성되어 순수 로직을 담당합니다.
    /// </summary>
    public class TowerManager
    {
        #region 내부 필드
        private readonly IEventBus m_eventBus;
        private TowerData m_currentTower;
        private int m_currentFloorIndex = 0;
        
        /// <summary>
        /// [설명]: 층별로 현재 활성화된(살아있는) 적의 수를 추적합니다.
        /// </summary>
        private readonly Dictionary<int, int> m_activeEnemiesPerFloor = new Dictionary<int, int>();
        #endregion

        #region 프로퍼티
        /// <summary>
        /// [설명]: 현재 플레이어가 위치한 층의 인덱스입니다.
        /// </summary>
        public int CurrentFloorIndex => m_currentFloorIndex;

        /// <summary>
        /// [설명]: 현재 층의 데이터를 반환합니다.
        /// </summary>
        public FloorData CurrentFloorData => (m_currentTower != null && m_currentFloorIndex < m_currentTower.Floors.Count) ? m_currentTower.Floors[m_currentFloorIndex] : null;

        /// <summary>
        /// [설명]: 공통적으로 사용되는 지면 프리펩입니다.
        /// </summary>
        public GameObject CommonGroundPrefab => m_currentTower != null ? m_currentTower.GroundPrefab : null;

        /// <summary>
        /// [설명]: 타워의 모든 층을 클리어했는지 여부입니다.
        /// </summary>
        public bool IsFinished => m_currentTower != null && m_currentFloorIndex >= m_currentTower.TotalFloors;
        #endregion

        #region 초기화
        public TowerManager(IEventBus eventBus, TowerData towerData)
        {
            m_eventBus = eventBus;
            m_currentTower = towerData;
            m_currentFloorIndex = 0;

            if (m_eventBus != null)
            {
                m_eventBus.Subscribe<OnEnemyKilled>(HandleEnemyKilled);
            }

            Debug.Log($"[TowerManager] '{towerData?.TowerName}' 데이터로 초기화 완료");
        }
        #endregion

        #region 공개 메서드
        /// <summary>
        /// [설명]: 다음 층으로 진행 인덱스를 변경하고 이벤트를 발행합니다.
        /// </summary>
        public void NextFloor()
        {
            if (IsFinished) return;

            m_currentFloorIndex++;
            Debug.Log($"[TowerManager] {m_currentFloorIndex}층 진행");
            m_eventBus.Publish(new OnFloorCleared(m_currentFloorIndex));
        }

        /// <summary>
        /// [설명]: 특정 층에 스폰된 적의 수를 등록하여 클리어 조건을 관리합니다.
        /// </summary>
        /// <param name="floorIndex">적군이 위치한 층 인덱스</param>
        /// <param name="count">스폰된 적 수</param>
        public void RegisterEnemies(int floorIndex, int count)
        {
            if (!m_activeEnemiesPerFloor.ContainsKey(floorIndex))
            {
                m_activeEnemiesPerFloor[floorIndex] = 0;
            }
            
            m_activeEnemiesPerFloor[floorIndex] += count;
        }

        /// <summary>
        /// [설명]: 타워의 전체 층 데이터 목록을 반환합니다.
        /// </summary>
        public IReadOnlyList<FloorData> GetFloorsList()
        {
            if (m_currentTower == null) return new List<FloorData>();
            return m_currentTower.Floors;
        }
        #endregion

        #region 비즈니스 로직
        /// <summary>
        /// [설명]: 적 처치 이벤트 수신 시 해당 층의 남은 적 수를 갱신합니다.
        /// </summary>
        private void HandleEnemyKilled(OnEnemyKilled evt)
        {
            if (m_activeEnemiesPerFloor.ContainsKey(evt.FloorIndex))
            {
                m_activeEnemiesPerFloor[evt.FloorIndex]--;

                if (evt.FloorIndex == m_currentFloorIndex && m_activeEnemiesPerFloor[evt.FloorIndex] <= 0)
                {
                    // [추가]: 층 클리어 시 즉시 이벤트 발행 (보상 상자 스폰 등 활용)
                    m_eventBus.Publish(new OnFloorCleared(m_currentFloorIndex));
                    
                    HandleFloorClearedDelayedAsync().Forget();
                }
            }
        }

        /// <summary>
        /// [설명]: 층의 모든 적 처치 후 지연 시간을 두고 '다음 층 이동 가능' 상태를 알립니다.
        /// </summary>
        private async UniTaskVoid HandleFloorClearedDelayedAsync()
        {
            // 적 처치 시마다 로그를 찍는 대신, 층 클리어 시점에만 한 번 출력
            Debug.Log($"[TowerManager] {m_currentFloorIndex}층 모든 적 처치 완료");
            
            await UniTask.Delay(1000);
            
            m_eventBus.Publish(new OnFloorReadyForNext());
        }
        #endregion
    }
}

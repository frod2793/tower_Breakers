using System;
using System.Collections.Generic;
using UnityEngine;
using TowerBreakers.Tower.Data;
using TowerBreakers.Core.Events;

namespace TowerBreakers.Tower.Service
{
    /// <summary>
    /// [기능]: 타워의 현재 층 및 적 처치 진행 상황을 관리하는 서비스입니다.
    /// 적의 종류별(Normal, Elite, Boss) 처치 현황을 상세히 관리하여 UI에 전달합니다.
    /// </summary>
    public class TowerFloorService : IDisposable
    {
        #region 내부 필드
        private TowerData m_towerData;
        private readonly IEventBus m_eventBus;
        
        private int m_currentFloor = 1;
        
        // 종류별 총 수량
        private int m_totalNormal;
        private int m_totalElite;
        private int m_totalBoss;

        // 종류별 처치 수량
        private int m_killedNormal;
        private int m_killedElite;
        private int m_killedBoss;
        #endregion

        #region 프로퍼티
        public int CurrentFloor => m_currentFloor;
        public int TotalFloors => m_towerData != null ? m_towerData.Floors.Count : 0;
        
        public int TotalEnemyCount => m_totalNormal + m_totalElite + m_totalBoss;
        public int TotalKilledCount => m_killedNormal + m_killedElite + m_killedBoss;
        #endregion

        #region 초기화
        public TowerFloorService(IEventBus eventBus)
        {
            m_eventBus = eventBus;
            
            // [추가]: 적 사망 이벤트 직접 구독
            m_eventBus?.Subscribe<OnEnemyKilled>(HandleEnemyKilled);
        }

        public void Initialize(TowerData towerData)
        {
            m_towerData = towerData;
            m_currentFloor = 1;
            ResetEnemyCount();
        }

        public void Dispose()
        {
            m_eventBus?.Unsubscribe<OnEnemyKilled>(HandleEnemyKilled);
        }
        #endregion

        #region 이벤트 핸들러
        private void HandleEnemyKilled(OnEnemyKilled evt)
        {
            RegisterEnemyDeath(evt.EnemyType);
        }
        #endregion

        #region 공개 API
        /// <summary>
        /// [설명]: 현재 층의 적 구성 정보를 설정합니다.
        /// </summary>
        public void SetupFloorEnemies(FloorData floor)
        {
            ResetEnemyCount();
            if (floor == null) return;

            foreach (var spawnInfo in floor.Enemies)
            {
                switch (spawnInfo.EnemyType)
                {
                    case EnemyType.Normal: m_totalNormal += spawnInfo.Count; break;
                    case EnemyType.Elite: m_totalElite += spawnInfo.Count; break;
                    case EnemyType.Boss: m_totalBoss += spawnInfo.Count; break;
                }
            }
            NotifyEnemyCountChanged();
        }

        /// <summary>
        /// [설명]: 적 사망 시 호출하여 해당 타입의 카운트를 갱신합니다.
        /// </summary>
        public void RegisterEnemyDeath(EnemyType type)
        {
            switch (type)
            {
                case EnemyType.Normal: m_killedNormal++; break;
                case EnemyType.Elite: m_killedElite++; break;
                case EnemyType.Boss: m_killedBoss++; break;
            }

            NotifyEnemyCountChanged();

            if (TotalKilledCount >= TotalEnemyCount && TotalEnemyCount > 0)
            {
                OnAllEnemiesCleared?.Invoke();
                m_eventBus.Publish(new OnFloorCleared { FloorNumber = m_currentFloor });
            }
        }

        public event Action OnAllEnemiesCleared;
        public event Action OnTowerCompleted;

        public void MoveToNextFloor()
        {
            if (m_currentFloor < TotalFloors)
            {
                m_currentFloor++;
                ResetEnemyCount();
                m_eventBus.Publish(new OnFloorStarted { FloorNumber = m_currentFloor });
            }
            else
            {
                OnTowerCompleted?.Invoke();
            }
        }

        public FloorData GetCurrentFloorData() => GetFloorData(m_currentFloor);

        public FloorData GetFloorData(int floorNumber)
        {
            if (m_towerData == null || floorNumber < 1 || floorNumber > TotalFloors) return null;
            return m_towerData.Floors[floorNumber - 1];
        }

        public FloorRewardData GetCurrentFloorReward()
        {
            var floor = GetCurrentFloorData();
            return floor != null ? floor.ClearReward : null;
        }

        public bool IsLastFloor() => m_currentFloor >= TotalFloors;
        #endregion

        #region 내부 로직
        private void ResetEnemyCount()
        {
            m_totalNormal = m_totalElite = m_totalBoss = 0;
            m_killedNormal = m_killedElite = m_killedBoss = 0;
        }

        private void NotifyEnemyCountChanged()
        {
            m_eventBus?.Publish(new OnEnemyCountChanged
            {
                NormalRemaining = Mathf.Max(0, m_totalNormal - m_killedNormal),
                NormalTotal = m_totalNormal,
                EliteRemaining = Mathf.Max(0, m_totalElite - m_killedElite),
                EliteTotal = m_totalElite,
                BossRemaining = Mathf.Max(0, m_totalBoss - m_killedBoss),
                BossTotal = m_totalBoss
            });
        }
        #endregion
    }
}

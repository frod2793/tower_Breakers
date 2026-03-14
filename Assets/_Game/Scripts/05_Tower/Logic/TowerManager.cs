using System;
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
        /// [설명]: 층별로 현재 활성화된(살아있는) 적의 타입 리스트를 추적합니다.
        /// </summary>
        private readonly Dictionary<int, List<TowerBreakers.Enemy.Data.EnemyType>> m_activeEnemiesPerFloor = new();
        /// <summary>
        /// [설명]: 층별로 존재하는 보상 상자의 수를 추적합니다.
        /// </summary>
        private readonly Dictionary<int, int> m_activeChestsPerFloor = new Dictionary<int, int>();
        #endregion

        #region 프로퍼티
        /// <summary>
        /// [설명]: 현재 플레이어가 위치한 층의 인덱스입니다.
        /// </summary>
        public int CurrentFloorIndex => m_currentFloorIndex;

        /// <summary>
        /// [설명]: 현재 층에서 살아있는 적의 타입 목록을 반환합니다.
        /// </summary>
        public IReadOnlyList<TowerBreakers.Enemy.Data.EnemyType> CurrentFloorEnemies
        {
            get
            {
                if (m_activeEnemiesPerFloor.TryGetValue(m_currentFloorIndex, out var list))
                    return list;
                return Array.Empty<TowerBreakers.Enemy.Data.EnemyType>();
            }
        }

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
                m_eventBus.Subscribe<OnRewardChestRegistered>(HandleChestRegistered);
                m_eventBus.Subscribe<OnRewardChestOpened>(HandleChestOpened);
            }

            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[TowerManager] '{towerData?.TowerName}' 데이터로 초기화 완료");
            #endif
        }
        #endregion

        #region 이벤트
        /// <summary>
        /// [설명]: 적 또는 상자의 상태가 변경되었을 때 발행되는 이벤트입니다.
        /// </summary>
        public event Action OnDataChanged;
        #endregion

        #region 공개 메서드
        /// <summary>
        /// [설명]: 다음 층으로 진행 인덱스를 변경하고 이벤트를 발행합니다.
        /// </summary>
        public void NextFloor()
        {
            if (IsFinished) return;

            m_currentFloorIndex++;
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[TowerManager] {m_currentFloorIndex}층 진행");
            #endif
            m_eventBus.Publish(new OnFloorCleared(m_currentFloorIndex));
            OnDataChanged?.Invoke();
        }

        /// <summary>
        /// [설명]: 특정 층에 스폰된 적의 수를 등록하여 클리어 조건을 관리합니다.
        /// </summary>
        /// <param name="floorIndex">적군이 위치한 층 인덱스</param>
        /// <param name="type">스폰된 적 타입</param>
        public void RegisterEnemies(int floorIndex, TowerBreakers.Enemy.Data.EnemyType type)
        {
            if (!m_activeEnemiesPerFloor.ContainsKey(floorIndex))
            {
                m_activeEnemiesPerFloor[floorIndex] = new List<TowerBreakers.Enemy.Data.EnemyType>();
            }
            
            m_activeEnemiesPerFloor[floorIndex].Add(type);
            OnDataChanged?.Invoke();
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
            if (m_activeEnemiesPerFloor.TryGetValue(evt.FloorIndex, out var enemyList))
            {
                // 해당 타입의 적을 리스트에서 하나 제거
                enemyList.Remove(evt.EnemyType);
                OnDataChanged?.Invoke();

                if (evt.FloorIndex == m_currentFloorIndex && enemyList.Count <= 0)
                {
                    // 적은 다 죽였지만 상자가 남아있는지 확인
                    m_activeChestsPerFloor.TryGetValue(evt.FloorIndex, out int remainingChests);

                    if (remainingChests <= 0)
                    {
                        // 모든 조건 충족 시 'GO' UI 활성화 유도
                        HandleFloorClearedDelayedAsync().Forget();
                    }
                    else
                    {
                        // 상자가 있으면 '적 처치 완료'만 알리고 상자 활성화를 유도
                        m_eventBus.Publish(new OnFloorEnemiesCleared(m_currentFloorIndex));
                    }
                }
            }
        }

        /// <summary>
        /// [설명]: 상자가 존재함을 등록합니다.
        /// </summary>
        private void HandleChestRegistered(OnRewardChestRegistered evt)
        {
            if (!m_activeChestsPerFloor.TryGetValue(evt.FloorIndex, out int count))
            {
                count = 0;
            }
            
            m_activeChestsPerFloor[evt.FloorIndex] = count + 1;
            OnDataChanged?.Invoke();
        }

        /// <summary>
        /// [설명]: 상자 개방 시 카운트를 줄이고 클리어 여부를 체크합니다.
        /// </summary>
        private void HandleChestOpened(OnRewardChestOpened evt)
        {
            if (m_activeChestsPerFloor.TryGetValue(evt.FloorIndex, out int remainingChests))
            {
                remainingChests--;
                m_activeChestsPerFloor[evt.FloorIndex] = remainingChests;
                OnDataChanged?.Invoke();

                // 적이 이미 다 죽은 상태에서 마지막 상자를 열었다면 클리어 처리 준비
                m_activeEnemiesPerFloor.TryGetValue(evt.FloorIndex, out var remainingEnemies);

                bool allEnemiesDead = remainingEnemies == null || remainingEnemies.Count <= 0;

                if (evt.FloorIndex == m_currentFloorIndex && allEnemiesDead && remainingChests <= 0)
                {
                    HandleFloorClearedDelayedAsync().Forget();
                }
            }
        }

        /// <summary>
        /// [설명]: 층의 모든 적 처치 후 지연 시간을 두고 '다음 층 이동 가능' 상태를 알립니다.
        /// </summary>
        private async UniTaskVoid HandleFloorClearedDelayedAsync()
        {
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[TowerManager] {m_currentFloorIndex}층 모든 목표 클리어 (준비 완료)");
            #endif
            
            await UniTask.Delay(1000);
            
            m_eventBus.Publish(new OnFloorReadyForNext());
        }
        #endregion
    }
}

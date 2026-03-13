using UnityEngine;
using System.Collections.Generic;
using TowerBreakers.Core.Events;
using TowerBreakers.Tower.Logic;
using TowerBreakers.Environment.View;
using VContainer;

namespace TowerBreakers.Environment.Logic
{
    /// <summary>
    /// [설명]: 층 진행에 따라 맵 세그먼트를 동적으로 생성하고 관리하는 환경 시스템입니다.
    /// </summary>
    public class EnvironmentManager : MonoBehaviour
    {
        #region 에디터 설정
        [SerializeField, Tooltip("사용할 맵 세그먼트 프리팹(기본 틀)")]
        private MapSegment m_segmentPrefab;

        [SerializeField, Tooltip("초기 생성 세그먼트 개수")]
        private int m_initialSegmentCount = 3;

        [SerializeField, Tooltip("생성된 세그먼트들이 배치될 부모 오브젝트")]
        private Transform m_segmentParent;

        [SerializeField, Tooltip("스폰된 적들이 배치될 부모 오브젝트")]
        private Transform m_enemyParent;

        [SerializeField, Tooltip("플레이어의 기본 착지(전투 시) Y 좌표 (화면 중심 기준)")]
        private float m_defaultLandingY = -1.3f;
        #endregion

        #region 내부 필드
        private IEventBus m_eventBus;
        private TowerManager m_towerManager;
        private Player.Logic.PlayerPushReceiver m_playerReceiver;
        private Enemy.Logic.EnemySpawner m_enemySpawner;
        private List<MapSegment> m_activeSegments = new List<MapSegment>();
        private float m_currentOffset = 0f;
        private int m_spawnedSegmentCount = 0;
        #endregion

        #region 프로퍼티
        /// <summary>
        /// [설명]: 현재 사용 중인 세그먼트 프리팹의 높이를 반환합니다.
        /// </summary>
        public float DefaultSegmentHeight => m_segmentPrefab != null ? m_segmentPrefab.SegmentHeight : 15.0f;

        /// <summary>
        /// [설명]: 스폰된 적들이 배치될 부모 오브젝트를 반환합니다.
        /// </summary>
        public Transform EnemyParent => m_enemyParent;

        /// <summary>
        /// [설명]: 플레이어의 기본 착지 Y 좌표를 반환합니다.
        /// </summary>
        public float DefaultLandingY => m_defaultLandingY;
        #endregion

        #region 초기화
        [Inject]
        public void Construct(IEventBus eventBus, TowerManager towerManager, Player.Logic.PlayerPushReceiver playerReceiver, Enemy.Logic.EnemySpawner enemySpawner)
        {
            m_eventBus = eventBus;
            m_towerManager = towerManager;
            m_playerReceiver = playerReceiver;
            m_enemySpawner = enemySpawner;
            
            // Spawner에 부모 설정
            if (m_enemySpawner != null)
            {
                m_enemySpawner.SetEnemyParent(m_enemyParent);
            }

            m_eventBus.Subscribe<OnFloorCleared>(OnFloorCleared);
            
            InitializeMap();
        }

        private void InitializeMap()
        {
            m_currentOffset = 0f;
            m_spawnedSegmentCount = 0;

            // 초기 세그먼트 생성 (TowerManager에 데이터가 있는 만큼만 혹은 최소 수량)
            for (int i = 0; i < m_initialSegmentCount; i++)
            {
                CreateNextSegment();
            }

            // [추가]: 현재 층의 배경 경계를 플레이어에게 적용
            UpdatePlayerBoundary();
        }
        #endregion

        #region 비즈니스 로직
        private void OnFloorCleared(OnFloorCleared evt)
        {
            // 새로운 층으로 갈 때마다 새 세그먼트 미리 추가
            CreateNextSegment();
            
            // [추가]: 층이 바뀌었으므로 플레이어의 배경 경계도 갱신
            UpdatePlayerBoundary();

            // 최적화: 너무 멀어진 이전 세그먼트 정리
            if (m_activeSegments.Count > m_initialSegmentCount + 2)
            {
                var oldSegment = m_activeSegments[0];
                m_activeSegments.RemoveAt(0);
                if (oldSegment != null) Destroy(oldSegment.gameObject);
            }
        }

        /// <summary>
        /// [설명]: 현재 진행 중인 층의 환경 데이터를 기반으로 플레이어의 이동 제한 경계를 갱신합니다.
        /// </summary>
        private void UpdatePlayerBoundary()
        {
            if (m_playerReceiver == null || m_towerManager == null || m_activeSegments.Count == 0) return;

            int currentIndex = m_towerManager.CurrentFloorIndex;
            int firstSegmentFloorIndex = m_spawnedSegmentCount - m_activeSegments.Count;
            int targetIndex = currentIndex - firstSegmentFloorIndex;

            if (targetIndex < 0) targetIndex = 0;
            if (targetIndex >= m_activeSegments.Count) targetIndex = m_activeSegments.Count - 1;

            var currentSegment = m_activeSegments[targetIndex];
            if (currentSegment != null)
            {
                // 배경의 왼쪽 경계를 맵 제한(Map Limit)으로 설정
                m_playerReceiver.SetMapLimit(currentSegment.LeftBoundaryX);
                
                // 백플립 시작 지점 연동
                m_playerReceiver.SetBackflipThreshold(currentSegment.BackflipStartPoint);
            }
        }

        private void CreateNextSegment()
        {
            if (m_segmentPrefab == null) return;

            // 지정된 부모가 있으면 해당 부모 하위에 생성, 없으면 관리자 본인의 자식으로 생성
            Transform parent = (m_segmentParent != null) ? m_segmentParent : transform;
            MapSegment newSegment = Instantiate(m_segmentPrefab, parent);
            
            // 수직으로 쌓기 (Y축 오프셋 적용, X는 0 고정)
            newSegment.SetPosition(new Vector2(0f, m_currentOffset));
            
            // [최적화]: 타일맵이 이미 루트에 배치되어 있으므로 추가적인 지면 클론 생성을 생략합니다.
            
            m_activeSegments.Add(newSegment);
            // 다음 세그먼트 위치를 위해 높이만큼 오프셋 누적
            m_currentOffset += newSegment.SegmentHeight;
            m_spawnedSegmentCount++;
        }

        /// <summary>
        /// [설명]: 특정 층의 플레이어 스폰(대시 시작) 위치를 반환합니다.
        /// </summary>
        public Vector2 GetPlayerSpawnPosition(int floorIndex)
        {
            var segment = GetSegmentForFloor(floorIndex);
            return segment != null ? segment.PlayerSpawnPosition : new Vector2(-8f, -1.3f);
        }

        /// <summary>
        /// [설명]: 특정 층의 플레이어 착지(전투 시작) 위치를 반환합니다.
        /// </summary>
        public Vector2 GetPlayerLandingPosition(int floorIndex)
        {
            var segment = GetSegmentForFloor(floorIndex);
            return segment != null ? segment.PlayerLandingPosition : new Vector2(2f, -1.3f);
        }

        /// <summary>
        /// [설명]: 특정 층 인덱스에 매칭되는 세그먼트의 스폰 위치를 반환합니다.
        /// </summary>
        public Vector2 GetSpawnPosition(int floorIndex)
        {
            var segment = GetSegmentForFloor(floorIndex);
            return segment != null ? segment.EnemySpawnPosition : new Vector2(5f, -1f);
        }

        /// <summary>
        /// [설명]: 특정 층 인덱스에 해당하는 세그먼트를 반환합니다. 필요 시 세그먼트를 추가 생성합니다.
        /// </summary>
        private MapSegment GetSegmentForFloor(int floorIndex)
        {
            if (m_activeSegments.Count == 0) return null;

            int firstSegmentFloorIndex = m_spawnedSegmentCount - m_activeSegments.Count;
            int targetIndex = floorIndex - firstSegmentFloorIndex;

            if (targetIndex < 0) targetIndex = 0;
            
            // 아직 세그먼트가 생성되지 않았을 경우 필요한 만큼 더 생성 시도
            while (targetIndex >= m_activeSegments.Count && m_spawnedSegmentCount < floorIndex + 2)
            {
                CreateNextSegment();
            }

            if (targetIndex >= m_activeSegments.Count) targetIndex = m_activeSegments.Count - 1;

            return m_activeSegments[targetIndex];
        }
        #endregion

        #region 유니티 생명주기
        private void OnDestroy()
        {
            m_eventBus?.Unsubscribe<OnFloorCleared>(OnFloorCleared);
        }
        #endregion
    }
}

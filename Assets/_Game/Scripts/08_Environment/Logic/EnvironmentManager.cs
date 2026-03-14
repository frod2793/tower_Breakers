using System.Collections.Generic;
using TowerBreakers.Core.Events;
using TowerBreakers.Environment.View;
using TowerBreakers.Tower.Logic;
using UnityEngine;
using VContainer;

namespace TowerBreakers.Environment.Logic
{
    /// <summary>
    /// [설명]: 층 진행에 따라 맵 세그먼트를 동적으로 생성하고 관리하는 환경 시스템입니다.
    /// 플레이어와 적의 스폰 위치 및 맵 경계를 관리합니다.
    /// </summary>
    public class EnvironmentManager : MonoBehaviour
    {
        #region 에디터 설정
        [Header("프리팹 및 부모 설정")]
        [SerializeField, Tooltip("사용할 맵 세그먼트 프리팹(기본 틀)")]
        private MapSegment m_segmentPrefab;

        [SerializeField, Tooltip("초기 생성 세그먼트 개수")]
        private int m_initialSegmentCount = 3;

        [SerializeField, Tooltip("생성된 세그먼트들이 배치될 부모 오브젝트")]
        private Transform m_segmentParent;

        [SerializeField, Tooltip("스폰된 적들이 배치될 부모 오브젝트")]
        private Transform m_enemyParent;

        [Header("좌표 설정")]
        [SerializeField, Tooltip("플레이어의 기본 착지(전투 시) Y 좌표 (화면 중심 기준)")]
        private float m_defaultLandingY = -1.3f;

        [SerializeField, Tooltip("플레이어 대시 시작 X 오프셋")]
        private float m_playerSpawnOffsetX = -12.0f;

        [SerializeField, Tooltip("플레이어 착지(전투 시작) X 오프셋")]
        private float m_playerLandingOffsetX = 2.0f;

        [SerializeField, Tooltip("백플립 기점 X 오프셋 (세그먼트 원점 기준)")]
        private float m_backflipThresholdOffsetX = 3.76f;

        [SerializeField, Tooltip("적 스폰 X 오프셋")]
        private float m_enemySpawnOffsetX = 9.0f;
        #endregion

        #region 내부 필드
        private IEventBus m_eventBus;
        private TowerManager m_towerManager;
        private Player.Logic.PlayerPushReceiver m_playerReceiver;
        private Enemy.Logic.EnemySpawner m_enemySpawner;

        private readonly List<MapSegment> m_activeSegments = new List<MapSegment>();
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

        #region 초기화 및 바인딩 로직
        /// <summary>
        /// [설명]: 의존성을 주입받고 이벤트를 구독합니다.
        /// </summary>
        [Inject]
        public void Construct(
            IEventBus eventBus, 
            TowerManager towerManager, 
            Player.Logic.PlayerPushReceiver playerReceiver, 
            Enemy.Logic.EnemySpawner enemySpawner)
        {
            m_eventBus = eventBus;
            m_towerManager = towerManager;
            m_playerReceiver = playerReceiver;
            m_enemySpawner = enemySpawner;
            
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
            m_activeSegments.Clear();

            for (int i = 0; i < m_initialSegmentCount; i++)
            {
                CreateNextSegment();
            }

            UpdatePlayerBoundary();
        }
        #endregion

        #region 비즈니스 로직
        /// <summary>
        /// [설명]: 층 클리어 이벤트 핸들러입니다. 새로운 세그먼트를 생성하고 이전 세그먼트를 정리합니다.
        /// </summary>
        private void OnFloorCleared(OnFloorCleared evt)
        {
            CreateNextSegment();
            UpdatePlayerBoundary();

            // 메모리 최적화: 시야에서 완전히 사라진 과거 세그먼트 삭제
            if (m_activeSegments.Count > m_initialSegmentCount + 2)
            {
                var oldSegment = m_activeSegments[0];
                m_activeSegments.RemoveAt(0);
                if (oldSegment != null)
                {
                    Destroy(oldSegment.gameObject);
                }
            }
        }

        /// <summary>
        /// [설명]: 현재 층에 맞춰 플레이어의 월드 경계(좌측 벽, 백플립 지점)를 갱신합니다.
        /// </summary>
        private void UpdatePlayerBoundary()
        {
            if (m_playerReceiver == null || m_towerManager == null) return;

            var currentSegment = GetSegmentForFloor(m_towerManager.CurrentFloorIndex);
            if (currentSegment != null)
            {
                m_playerReceiver.SetMapLimit(currentSegment.LeftBoundaryX);
                
                // 세그먼트 위치를 기준으로 백플립 기준점 X 좌표 계산
                float backflipWorldX = currentSegment.transform.position.x + m_backflipThresholdOffsetX;
                m_playerReceiver.SetBackflipThreshold(backflipWorldX);
            }
        }

        /// <summary>
        /// [설명]: 다음 세그먼트를 생성하고 수직 위치를 설정합니다.
        /// </summary>
        private void CreateNextSegment()
        {
            if (m_segmentPrefab == null) return;

            Transform parent = (m_segmentParent != null) ? m_segmentParent : transform;
            MapSegment newSegment = Instantiate(m_segmentPrefab, parent);
            
            newSegment.SetPosition(new Vector2(0f, m_currentOffset));
            m_activeSegments.Add(newSegment);
            
            m_currentOffset += newSegment.SegmentHeight;
            m_spawnedSegmentCount++;
        }
        #endregion

        #region 공개 API
        /// <summary>
        /// [설명]: 특정 층의 플레이어 스폰(대시 시작) 위치를 반환합니다.
        /// </summary>
        public Vector2 GetPlayerSpawnPosition(int floorIndex)
        {
            var segment = GetSegmentForFloor(floorIndex);
            if (segment == null) return new Vector2(m_playerSpawnOffsetX, m_defaultLandingY);
            
            Vector3 segPos = segment.transform.position;
            return new Vector2(segPos.x + m_playerSpawnOffsetX, segPos.y + m_defaultLandingY);
        }

        /// <summary>
        /// [설명]: 특정 층의 플레이어 착지(전투 시작) 위치를 반환합니다.
        /// </summary>
        public Vector2 GetPlayerLandingPosition(int floorIndex)
        {
            var segment = GetSegmentForFloor(floorIndex);
            if (segment == null) return new Vector2(m_playerLandingOffsetX, m_defaultLandingY);

            Vector3 segPos = segment.transform.position;
            return new Vector2(segPos.x + m_playerLandingOffsetX, segPos.y + m_defaultLandingY);
        }

        /// <summary>
        /// [설명]: 특정 층의 세그먼트 Transform을 반환합니다.
        /// </summary>
        public Transform GetSegmentTransform(int floorIndex)
        {
            var segment = GetSegmentForFloor(floorIndex);
            return (segment != null) ? segment.transform : null;
        }

        /// <summary>
        /// [설명]: 특정 층의 적 스폰 위치를 반환합니다.
        /// </summary>
        public Vector2 GetSpawnPosition(int floorIndex)
        {
            var segment = GetSegmentForFloor(floorIndex);
            if (segment == null) return new Vector2(m_enemySpawnOffsetX, m_defaultLandingY);

            Vector3 segPos = segment.transform.position;
            return new Vector2(segPos.x + m_enemySpawnOffsetX, segPos.y + m_defaultLandingY);
        }
        #endregion

        #region 내부 로직
        /// <summary>
        /// [설명]: 층 인덱스에 해당하는 세그먼트를 리스트에서 찾아 반환합니다.
        /// 인덱스 범위를 벗어나면 자동으로 세그먼트를 추가 생성하여 가용성을 보장합니다.
        /// </summary>
        private MapSegment GetSegmentForFloor(int floorIndex)
        {
            if (m_activeSegments.Count == 0) return null;

            int firstSegmentFloorIndex = m_spawnedSegmentCount - m_activeSegments.Count;
            int targetIndex = floorIndex - firstSegmentFloorIndex;

            if (targetIndex < 0) targetIndex = 0;
            
            // 부족한 세그먼트 자동 생성 로직 (Batching 방지 위해 최소 필요한 만큼만)
            while (targetIndex >= m_activeSegments.Count && m_spawnedSegmentCount < floorIndex + 2)
            {
                CreateNextSegment();
            }

            if (targetIndex >= m_activeSegments.Count)
            {
                targetIndex = m_activeSegments.Count - 1;
            }

            return m_activeSegments[targetIndex];
        }
        #endregion

        #region 유니티 생명주기
        private void OnDestroy()
        {
            if (m_eventBus != null)
            {
                m_eventBus.Unsubscribe<OnFloorCleared>(OnFloorCleared);
            }
        }
        #endregion
    }
}

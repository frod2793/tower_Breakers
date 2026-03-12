using UnityEngine;
using System.Collections.Generic;
using TowerBreakers.Core.Events;
using TowerBreakers.Tower.Logic;
using VContainer;

namespace TowerBreakers.Environment
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
        #endregion

        #region 내부 필드
        private IEventBus m_eventBus;
        private TowerManager m_towerManager;
        private List<MapSegment> m_activeSegments = new List<MapSegment>();
        private float m_currentOffset = 0f;
        private int m_spawnedSegmentCount = 0;
        #endregion

        #region 초기화
        [Inject]
        public void Construct(IEventBus eventBus, TowerManager towerManager)
        {
            m_eventBus = eventBus;
            m_towerManager = towerManager;
            
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
        }
        #endregion

        #region 비즈니스 로직
        private void OnFloorCleared(OnFloorCleared evt)
        {
            // 새로운 층으로 갈 때마다 새 세그먼트 미리 추가
            CreateNextSegment();
            
            // 최적화: 너무 멀어진 이전 세그먼트 정리
            if (m_activeSegments.Count > m_initialSegmentCount + 2)
            {
                var oldSegment = m_activeSegments[0];
                m_activeSegments.RemoveAt(0);
                if (oldSegment != null) Destroy(oldSegment.gameObject);
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
            
            // TowerManager의 데이터를 기반으로 공통 지면 비주얼 적용
            if (m_towerManager != null && m_towerManager.CommonGroundPrefab != null)
            {
                newSegment.AttachGround(m_towerManager.CommonGroundPrefab);
            }
            
            m_activeSegments.Add(newSegment);
            // 다음 세그먼트 위치를 위해 높이만큼 오프셋 누적
            m_currentOffset += newSegment.SegmentHeight;
            m_spawnedSegmentCount++;
        }

        /// <summary>
        /// [설명]: 현재 TowerManager의 진행도에 맞는 세그먼트의 스폰 위치를 반환합니다.
        /// </summary>
        public Vector2 GetCurrentSpawnPosition()
        {
            if (m_towerManager == null || m_activeSegments.Count == 0)
            {
                return new Vector2(5f, -1f); // 기본/폴백 좌표
            }

            int currentIndex = m_towerManager.CurrentFloorIndex;
            
            // 시스템에서 지워진(가비지 콜렉트된) 세그먼트 수를 계산하여 현재 층의 시각적 세그먼트 찾기
            int firstSegmentFloorIndex = m_spawnedSegmentCount - m_activeSegments.Count;
            int targetIndex = currentIndex - firstSegmentFloorIndex;

            // 방어 코드: 계산된 인덱스가 유효 범위를 벗어나면, 가장 근사한 세그먼트로 보정
            if (targetIndex < 0) targetIndex = 0;
            if (targetIndex >= m_activeSegments.Count) targetIndex = m_activeSegments.Count - 1;

            var currentSegment = m_activeSegments[targetIndex];
            
            return currentSegment.EnemySpawnPosition;
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

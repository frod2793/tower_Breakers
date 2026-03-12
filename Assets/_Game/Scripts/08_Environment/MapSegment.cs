using UnityEngine;

namespace TowerBreakers.Environment
{
    /// <summary>
    /// [설명]: 단일 층(섹션)의 시각적 요소(바닥, 배경, 장식 등)를 관리하는 클래스입니다.
    /// 프리팹으로 제작되어 EnvironmentManager에 의해 생성 및 관리됩니다.
    /// </summary>
    public class MapSegment : MonoBehaviour
    {
        #region 에디터 설정
        [SerializeField, Tooltip("이 세그먼트의 가로 길이 (전투 구역)")]
        private float m_segmentWidth = 20.0f;

        [SerializeField, Tooltip("이 세그먼트의 세로 길이 (타워 적층 높이)")]
        private float m_segmentHeight = 15.0f;
        
        [SerializeField, Tooltip("지면(Ground) 프리팹이 생성되어 부착될 위치")]
        private Transform m_groundAnchor;
        
        [SerializeField, Tooltip("적 스폰 지점 (오프셋)")]
        private Transform m_enemySpawnPoint;
        #endregion

        #region 내부 필드
        [SerializeField, Tooltip("현재 세그먼트의 지면(Ground) 오브젝트. 프리팹에 미리 배치한 경우 여기에 연결하세요.")]
        private GameObject m_currentGround;
        #endregion

        #region 프로퍼티
        public float SegmentWidth => m_segmentWidth;
        public float SegmentHeight => m_segmentHeight;
        public Vector2 EnemySpawnPosition => m_enemySpawnPoint != null ? (Vector2)m_enemySpawnPoint.position : (Vector2)transform.position;
        #endregion

        #region 공개 메서드
        /// <summary>
        /// [설명]: 이 세그먼트에 지면 시각 요소를 생성하여 부착합니다.
        /// </summary>
        /// <param name="groundPrefab">생성할 지면 프리팹</param>
        public void AttachGround(GameObject groundPrefab)
        {
            if (groundPrefab == null || m_groundAnchor == null) return;

            // 기존 지면이 있다면 제거
            if (m_currentGround != null)
            {
                Destroy(m_currentGround);
            }

            m_currentGround = Instantiate(groundPrefab, m_groundAnchor);
            m_currentGround.transform.localPosition = Vector3.zero;
            m_currentGround.transform.localRotation = Quaternion.identity;
        }
        /// <summary>
        /// [설명]: 세그먼트를 특정 위치에 배치합니다.
        /// </summary>
        public void SetPosition(Vector2 position)
        {
            transform.position = position;
        }
        #endregion
    }
}

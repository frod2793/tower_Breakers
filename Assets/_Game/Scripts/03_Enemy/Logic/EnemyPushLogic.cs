using UnityEngine;
using TowerBreakers.Player.Logic;

namespace TowerBreakers.Enemy.Logic
{
    /// <summary>
    /// [설명]: 적이 플레이어와 접촉했을 때 밀기 힘을 전달하는 로직 클래스입니다.
    /// </summary>
    public class EnemyPushLogic : MonoBehaviour
    {
        #region 에디터 설정
        [SerializeField, Tooltip("밀기 감지 거리")]
        private float m_pushDistance = 1.0f;
        #endregion

        #region 내부 필드
        private float m_force;
        private PlayerPushReceiver m_target;

        // 기차 행렬 이동을 위한 링크드 리스트 참조
        private EnemyPushLogic m_aheadEnemy;
        private EnemyPushLogic m_followerEnemy;
        
        public float TrainSpacing { get; set; } = 1.5f;
        #endregion

        #region 초기화
        public void Initialize(float force, PlayerPushReceiver target)
        {
            m_force = force;
            m_target = target;

            // 적이 물리적으로 구르거나 중력의 영향을 받지 않도록 설정 (최적화)
            var rb = GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.bodyType = RigidbodyType2D.Kinematic;
                rb.linearVelocity = Vector2.zero;
                // Kinematic 상태에서는 Z축 회전 고정 등 constraint 설정이 의미 없지만
                // 혹시 모를 Dynamic 전환 대비 설정 유지
                rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            }
        }

        /// <summary>
        /// [설명]: 오브젝트 풀링으로 재사용될 때 논리적 상태를 초기화합니다.
        /// </summary>
        public void ResetState()
        {
            m_aheadEnemy = null;
            m_followerEnemy = null;
            UpdateCollider();
            enabled = true;
        }
        #endregion

        #region 링크드 리스트 및 기차 이동 로직
        /// <summary>
        /// [설명]: 내 앞의 적을 설정하고 콜라이더 상태를 갱신합니다.
        /// </summary>
        public void SetAheadEnemy(EnemyPushLogic ahead)
        {
            m_aheadEnemy = ahead;
            if (m_aheadEnemy != null)
            {
                m_aheadEnemy.SetFollower(this);
            }
            UpdateCollider();
        }

        private void SetFollower(EnemyPushLogic follower)
        {
            m_followerEnemy = follower;
        }

        /// <summary>
        /// [설명]: 맨 앞의 적(리더)일 때만 콜라이더를 켜서 겹침 방지 최적화를 반영합니다.
        /// </summary>
        public void UpdateCollider()
        {
            var col = GetComponent<Collider2D>();
            if (col != null)
            {
                col.enabled = (m_aheadEnemy == null);
            }
        }

        /// <summary>
        /// [설명]: 적 파괴(Pool 회수 등) 시 앞뒤 대열을 이어줍니다.
        /// </summary>
        public void HandleDeath()
        {
            if (m_followerEnemy != null)
            {
                m_followerEnemy.SetAheadEnemy(m_aheadEnemy);
            }
            else if (m_aheadEnemy != null)
            {
                m_aheadEnemy.SetFollower(null);
            }

            m_aheadEnemy = null;
            m_followerEnemy = null;
            UpdateCollider();
        }
        #endregion

        #region 비즈니스 로직
        /// <summary>
        /// [설명]: 타겟 플레이어를 밀어내기 시도합니다. (거리 체크 기반)
        /// </summary>
        public void TryPushPlayer()
        {
            if (m_target == null) return;

            float distance = Vector2.Distance(transform.position, m_target.transform.position);
            if (distance <= m_pushDistance)
            {
                m_target.ReceivePush(m_force);
            }
        }

        /// <summary>
        /// [설명]: 레이캐스트 물리 연산 대신 앞 연대의 좌표만 판독하여 전진 가능 여부를 확인합니다.
        /// </summary>
        /// <param name="gap">유지할 최소 간격</param>
        /// <returns>막혀있으면 true</returns>
        public bool IsBlocked(float gap)
        {
            // 1. 전방에 위치한 적(동료)의 공간 체크 (최우선)
            if (m_aheadEnemy != null && m_aheadEnemy.gameObject.activeInHierarchy)
            {
                // 적은 왼쪽으로 전진하므로 내 X가 클 수록 뒤에 있음
                float distance = transform.position.x - m_aheadEnemy.transform.position.x;
                if (distance <= gap) return true;
                
                return false;
            }

            // 2. 앞선 적이 없다면(리더 적일 경우) 플레이어와의 접촉 체크
            if (m_target != null)
            {
                float distToPlayer = transform.position.x - m_target.transform.position.x;
                if (distToPlayer <= gap) return true;
            }

            return false;
        }
        #endregion
        
    }
}

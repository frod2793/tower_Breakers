using UnityEngine;
using TowerBreakers.Player.Logic;

namespace TowerBreakers.Enemy.Logic
{
    /// <summary>
    /// [설명]: 적이 플레이어와 접촉했을 때 밀기 힘을 전달하는 로직 클래스입니다.
    /// 기차 대열(Linked List) 형태의 군집 이동을 관리합니다.
    /// </summary>
    public class EnemyPushLogic : MonoBehaviour
    {
        #region 내부 필드
        private float m_force;
        private PlayerPushReceiver m_target;
        private Transform m_targetTransform;
        private Transform m_cachedTransform;

        // 기차 행렬 이동을 위한 링크드 리스트 참조
        private EnemyPushLogic m_aheadEnemy;
        private EnemyPushLogic m_followerEnemy;
        
        // [최적화]: 리더 및 상태 캐싱을 위한 필드
        private static int s_generation = 0;
        private EnemyPushLogic m_cachedLeader;
        private int m_cachedLeaderGen = -1;

        // [최적화]: 그룹 차단 여부 캐싱 (프레임 단위)
        private bool m_cachedGroupBlocked;
        private int m_cachedBlockedGen = -1;
        #endregion

        #region 프로퍼티
        /// <summary>
        /// [설명]: 대열 간격 설정입니다.
        /// </summary>
        public float TrainSpacing { get; set; } = 1.5f;

        /// <summary>
        /// [설명]: 현재 추적 중인 플레이어 밀림 수신 객체입니다.
        /// </summary>
        public PlayerPushReceiver PlayerReceiver => m_target;
        #endregion

        #region 초기화 및 바인딩 로직
        /// <summary>
        /// [설명]: 미는 힘과 타겟을 설정하여 로직을 초기화합니다.
        /// </summary>
        /// <param name="force">미는 힘의 크기</param>
        /// <param name="target">타겟 플레이어 객체</param>
        public void Initialize(float force, PlayerPushReceiver target)
        {
            m_force = force;
            m_target = target;
            
            // [최적화]: 빈번히 사용되는 참조 캐싱
            m_cachedTransform = transform;
            if (m_target != null)
            {
                m_targetTransform = m_target.transform;
            }

            // 적이 물리적으로 구르거나 중력의 영향을 받지 않도록 설정
            if (TryGetComponent<Rigidbody2D>(out var rb))
            {
                rb.bodyType = RigidbodyType2D.Kinematic;
                rb.linearVelocity = Vector2.zero;
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
            InvalidateLeaderCache(); // 캐시 무효화
            UpdateCollider();
            enabled = true;
        }

        /// <summary>
        /// [설명]: 대열이 변경될 때 모든 적 캐릭터의 리더 캐시를 무효화합니다.
        /// </summary>
        public static void InvalidateLeaderCache()
        {
            s_generation++;
        }
        #endregion

        #region 대열 관리 로직
        /// <summary>
        /// [설명]: 내 앞의 적을 설정하고 콜라이더 상태를 갱신합니다.
        /// </summary>
        /// <param name="ahead">내 앞의 적 객체</param>
        public void SetAheadEnemy(EnemyPushLogic ahead)
        {
            m_aheadEnemy = ahead;
            if (m_aheadEnemy != null)
            {
                m_aheadEnemy.SetFollower(this);
            }
            InvalidateLeaderCache();
            UpdateCollider();
        }

        /// <summary>
        /// [설명]: 내 뒤의 적을 설정합니다.
        /// </summary>
        /// <param name="follower">내 뒤의 적 객체</param>
        private void SetFollower(EnemyPushLogic follower)
        {
            m_followerEnemy = follower;
        }

        /// <summary>
        /// [설명]: 맨 앞의 적(리더)일 때만 콜라이더를 켜서 겹침 방지 최적화를 반영합니다.
        /// </summary>
        public void UpdateCollider()
        {
            if (TryGetComponent<Collider2D>(out var col))
            {
                col.enabled = (m_aheadEnemy == null);
            }
        }

        /// <summary>
        /// [설명]: 적 파괴 시 앞뒤 대열을 다시 연결해줍니다.
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
            InvalidateLeaderCache();
            UpdateCollider();
        }

        /// <summary>
        /// [설명]: 현재 나를 포함한 이 대열의 최전방 리더를 찾아 반환합니다.
        /// </summary>
        /// <returns>리더 객체</returns>
        public EnemyPushLogic GetLeader()
        {
            // [최적화]: 세대 번호가 동일하면 캐싱된 리더 즉시 반환
            if (m_cachedLeaderGen == s_generation && m_cachedLeader != null)
            {
                return m_cachedLeader;
            }

            EnemyPushLogic current = this;
            while (current.m_aheadEnemy != null && current.m_aheadEnemy.gameObject.activeInHierarchy)
            {
                current = current.m_aheadEnemy;
            }

            m_cachedLeader = current;
            m_cachedLeaderGen = s_generation;
            return current;
        }
        #endregion

        #region 비즈니스 로직
        /// <summary>
        /// [설명]: 타겟 플레이어를 밀어내기 시도합니다.
        /// </summary>
        public void TryPushPlayer()
        {
            if (m_target == null || m_targetTransform == null) return;

            float xDist = m_cachedTransform.position.x - m_targetTransform.position.x;
            
            // 유효 사거리 내에 있을 때 힘 전달
            if (xDist >= -1.0f && xDist <= 1.2f)
            {
                m_target.ApplyPushForce(m_force);
            }
        }

        /// <summary>
        /// [설명]: 플레이어와 밀착해 있는지 여부를 반환합니다.
        /// </summary>
        /// <param name="gap">판정 거리</param>
        public bool IsTouchingPlayer(float gap)
        {
            if (m_target == null || m_targetTransform == null) return false;
            
            float distToPlayer = m_cachedTransform.position.x - m_targetTransform.position.x;
            return distToPlayer >= -1.0f && distToPlayer <= gap;
        }

        /// <summary>
        /// [설명]: 현재 적이 속한 그룹 전체가 막혀있는지 확인합니다.
        /// </summary>
        /// <param name="gap">판정 거리</param>
        public bool IsGroupBlocked(float gap)
        {
            var leader = GetLeader();
            
            // 리더가 이미 같은 프레임에 계산을 마쳤다면 캐시 사용
            if (leader.m_cachedBlockedGen == Time.frameCount)
            {
                return leader.m_cachedGroupBlocked;
            }

            leader.m_cachedGroupBlocked = leader.CheckLeaderBlocked(gap);
            leader.m_cachedBlockedGen = Time.frameCount;
            
            return leader.m_cachedGroupBlocked;
        }

        /// <summary>
        /// [설명]: 리더 본인이 막혀있는지 확인하는 내부 로직입니다.
        /// </summary>
        private bool CheckLeaderBlocked(float gap)
        {
            if (m_target == null) return false;
            
            // 리더가 플레이어와 닿아 있고, 플레이어가 벽에 막혀 있으면 그룹 전체 정지
            bool isTouching = IsTouchingPlayer(gap + 0.2f); 
            bool isPlayerAtWall = m_target.IsAtWall;

            return isTouching && isPlayerAtWall;
        }

        /// <summary>
        /// [설명]: 리더가 플레이어를 벽으로 밀어붙이고 있는지 여부를 판정합니다.
        /// </summary>
        /// <param name="gap">판정 거리</param>
        public bool IsLeaderPushingAtWall(float gap)
        {
            var leader = GetLeader();
            if (leader != this) return false; 
            
            return IsGroupBlocked(gap) && IsTouchingPlayer(gap + 0.2f);
        }

        /// <summary>
        /// [설명]: 개별적으로 전진 가능 여부를 확인합니다.
        /// </summary>
        /// <param name="gap">판정 거리</param>
        public bool IsBlocked(float gap)
        {
            // 기차 대열 방식에서는 그룹 상태와 본인 상태를 동기화하여 부드러운 움직임 유도
            return IsGroupBlocked(gap);
        }
        #endregion
    }
}

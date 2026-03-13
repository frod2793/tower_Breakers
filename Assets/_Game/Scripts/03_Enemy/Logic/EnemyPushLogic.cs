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
        #endregion

        #region 내부 필드
        private float m_force;
        private PlayerPushReceiver m_target;

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

        public float TrainSpacing { get; set; } = 1.5f;

        /// <summary>
        /// [설명]: 현재 추적 중인 플레이어 밀림 수신 객체입니다.
        /// </summary>
        public PlayerPushReceiver PlayerReceiver => m_target;
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
            InvalidateLeaderCache(); // 캐시 무효화
            UpdateCollider();
            enabled = true;
        }

        /// <summary>
        /// [설명]: 대열이 변경될 때 모든 적 캐릭터의 리더 캐시를 무효화합니다.
        /// </summary>
        public static void InvalidateLeaderCache() => s_generation++;
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
            InvalidateLeaderCache(); // 대열 변경 시 무효화
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
            InvalidateLeaderCache(); // 대열 변경 시 무효화
            UpdateCollider();
        }

        /// <summary>
        /// [설명]: 현재 나를 포함한 이 대열의 맨 앞 리더를 찾아 반환합니다.
        /// </summary>
        public EnemyPushLogic GetLeader()
        {
            // [최적화]: 세대 번호가 같으면 캐싱된 리더 반환
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
        /// [설명]: 타겟 플레이어를 밀어내기 시도합니다. (거리 체크 기반)
        /// </summary>
        public void TryPushPlayer()
        {
            if (m_target == null) return;

            // X축 거리 기반으로 밀기 판정
            float xDist = transform.position.x - m_target.transform.position.x;
            
            // 적이 플레이어 근처에 있거나 이미 겹쳐진 상태(Overlapping)에서도 밀기 작동
            if (xDist >= -1.0f && xDist <= 1.2f)
            {
                m_target.ApplyPushForce(m_force);
            }
        }

        /// <summary>
        /// [설명]: 플레이어와 밀착(밀기 범위 내)해 있는지 여부를 반환합니다.
        /// </summary>
        public bool IsTouchingPlayer(float gap)
        {
            if (m_target == null) return false;
            float distToPlayer = transform.position.x - m_target.transform.position.x;
            
            return distToPlayer >= -1.0f && distToPlayer <= gap;
        }

        /// <summary>
        /// [설명]: 현재 적이 속한 그룹 전체가 막혀있는지 확인합니다. (리더의 상태를 따름)
        /// </summary>
        public bool IsGroupBlocked(float gap)
        {
            // [최적화]: 리더를 찾아서 그 결과를 재사용
            var leader = GetLeader();
            
            // 리더가 현재 프레임에 이미 계산했다면 그 결과 반환
            if (leader.m_cachedBlockedGen == Time.frameCount)
            {
                return leader.m_cachedGroupBlocked;
            }

            // 리더가 직접 계산하여 캐싱
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
            
            // [통합 판정]: 정지 판정 기준 거리를 1.3f(gap + 0.2f)로 통일
            bool isTouching = IsTouchingPlayer(gap + 0.2f); 
            bool isPlayerAtWall = m_target.IsAtWall;

            // 리더가 플레이어와 닿아 있고, 플레이어가 벽 때문에 더 밀릴 수 없을 때 그룹 전체 정지
            return isTouching && isPlayerAtWall;
        }

        /// <summary>
        /// [설명]: 리더가 플레이어를 벽으로 밀어붙이고 있는지 여부를 정확히 판정합니다. (데미지용)
        /// </summary>
        public bool IsLeaderPushingAtWall(float gap)
        {
            var leader = GetLeader();
            if (leader != this) return false; 
            
            // IsGroupBlocked 상태이면서 플레이어와 밀착(정지 판정과 동일한 1.3f)해 있는지 확인
            return IsGroupBlocked(gap) && IsTouchingPlayer(gap + 0.2f);
        }

        /// <summary>
        /// [설명]: 개별적으로 전진 가능 여부를 확인합니다. (기존 대열 간격 유지용 보조 로직)
        /// </summary>
        public bool IsBlocked(float gap)
        {
            // [최적화]: IsGroupBlocked()와 IsBlocked()의 로직을 통합하여 O(n) 재귀 제거
            // 기차 대열 방식에서는 "그룹이 막혀있는지"와 "내 앞이 막혀있는지"가 동일한 결과여야 부드럽게 움직임
            return IsGroupBlocked(gap);
        }
        #endregion
        
    }
}

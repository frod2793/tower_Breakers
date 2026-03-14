using UnityEngine;
using TowerBreakers.Core;
using TowerBreakers.Player.Logic;

namespace TowerBreakers.Enemy.Logic
{
    /// <summary>
    /// [설명]: 적이 플레이어와 접촉했을 때 밀기 힘을 전달하는 로직 클래스입니다.
    /// 기차 대열(Linked List) 형태의 군집 이동을 관리합니다.
    /// </summary>
    public class EnemyPushLogic : MonoBehaviour
    {
        // [최적화]: 활성 적 컬렉션 (FindObjectsOfType 대체)
        private static readonly System.Collections.Generic.List<EnemyPushLogic> s_activeEnemies = new();

        // [최적화]: 군집의 Collider 활성화 정책: 스킬 사용 시 전체 활성화, 종료 시 리더만 활성화
        public static System.Collections.Generic.IReadOnlyList<EnemyPushLogic> ActiveEnemies => s_activeEnemies;

        public static void RegisterEnemy(EnemyPushLogic enemy)
        {
            if (enemy != null && !s_activeEnemies.Contains(enemy))
            {
                s_activeEnemies.Add(enemy);
            }
        }

        public static void UnregisterEnemy(EnemyPushLogic enemy)
        {
            s_activeEnemies.Remove(enemy);
        }

        /// <summary>
        /// [설명]: 스킬 판정을 위해 모든 활성 적의 콜라이더를 일시적으로 활성화합니다.
        /// </summary>
        public static void EnableAllGroupCollidersForSkill()
        {
            for (int i = s_activeEnemies.Count - 1; i >= 0; i--)
            {
                var ep = s_activeEnemies[i];
                if (ep == null)
                {
                    s_activeEnemies.RemoveAt(i);
                    continue;
                }
                if (!ep.gameObject.activeInHierarchy) continue;
                var c = ep.GetComponent<Collider2D>();
                if (c != null) c.enabled = true;
            }
        }

        /// <summary>
        /// [설명]: 스킬 판정 종료 후 콜라이더를 원래 상태(리더만 활성)로 복원합니다.
        /// </summary>
        public static void DisableAllGroupCollidersForSkill()
        {
            for (int i = s_activeEnemies.Count - 1; i >= 0; i--)
            {
                var ep = s_activeEnemies[i];
                if (ep == null)
                {
                    s_activeEnemies.RemoveAt(i);
                    continue;
                }
                if (!ep.gameObject.activeInHierarchy) continue;
                var c = ep.GetComponent<Collider2D>();
                if (c != null)
                {
                    // [설명]: 리더이거나 특수 개체만 콜라이더 활성 유지
                    c.enabled = ep.m_isSpecialType || (ep.GetLeader() == ep);
                }
            }
        }
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

        /// <summary>
        /// [설명]: 특수 개체 여부 (항시 콜라이더 활성화 대상)
        /// </summary>
        private bool m_isSpecialType;

        // [최적화]: 개별 차단 여부 캐싱 (프레임 단위)
        private bool m_cachedBlocked;
        private int m_lastBlockedFrame = -1;
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

        /// <summary>
        /// [설명]: 내 앞에 있는 적 유닛을 반환합니다.
        /// </summary>
        public EnemyPushLogic AheadEnemy => m_aheadEnemy;
        #endregion

      
        #region 유니티 생명주기
        private void OnEnable()
        {
            RegisterEnemy(this);
        }

        private void OnDisable()
        {
            UnregisterEnemy(this);
        }
        #endregion

        #region 초기화 및 바인딩 로직
        /// <summary>
        /// [설명]: 미는 힘과 타겟을 설정하여 로직을 초기화합니다.
        /// </summary>
        /// <param name="force">미는 힘의 크기</param>
        /// <param name="target">타겟 플레이어 객체</param>
        /// <param name="isSpecial">특수 개체 여부 (항시 콜라이더 활성화)</param>
        public void Initialize(float force, PlayerPushReceiver target, bool isSpecial)
        {
            m_force = force;
            m_target = target;
            m_isSpecialType = isSpecial;
            
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
            m_lastBlockedFrame = -1; // 차단 캐시 무효화
            UpdateCollider();
            enabled = true;
            // RegisterEnemy(this); // OnEnable에서 처리함
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
        /// [설명]: 일반 개체는 리더일 때만, 특수 개체는 상시 콜라이더를 활성화하여 물리 연산을 최적화합니다.
        /// </summary>
        public void UpdateCollider()
        {
            if (TryGetComponent<Collider2D>(out var col))
            {
                // 특수 개체이거나, 내 앞에 아무도 없는 리더일 경우에만 콜라이더 활성화
                col.enabled = m_isSpecialType || (m_aheadEnemy == null);
            }
        }

        /// <summary>
        /// [설명]: 적 파괴 시 앞뒤 대열을 다시 연결해줍니다.
        /// </summary>
        /// <summary>
        /// [설명]: 사망 시 대열에서 이탈하고 등록을 해제합니다.
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
            // UnregisterEnemy(this); // OnDisable에서 처리함
            enabled = false;
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
            float effectiveForce = m_force;
            
            if (xDist >= -1.0f && xDist <= 1.2f)
            {
                m_target.ApplyPushForce(effectiveForce);
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
        /// 본인이 플레이어와 닿아 있거나, 바로 앞의 적이 막혀 있는 경우 정지합니다.
        /// [최적화]: 프레임 단위 캐싱을 통해 $O(N^2)$ 재귀 연산을 $O(N)$으로 개선했습니다.
        /// </summary>
        /// <param name="gap">판정 거리</param>
        public bool IsBlocked(float gap)
        {
            // [최적화]: 현재 프레임에 이미 계산되었다면 캐시된 결과 반환
            if (m_lastBlockedFrame == Time.frameCount)
            {
                return m_cachedBlocked;
            }

            bool isBlocked = false;

            // 1. 내가 플레이어와 닿아 있고 플레이어가 벽에 있다면 이동 불가
            if (IsTouchingPlayer(gap))
            {
                if (m_target != null && m_target.IsAtWall)
                {
                    isBlocked = true;
                }
            }

            // 2. 내 앞의 적이 막혀 있고 나 역시 간격이 좁다면 이동 불가 (대열 유지)
            if (!isBlocked && m_aheadEnemy != null)
            {
                // 앞의 적이 논리적으로 막혀 있거나, 현재 위치가 앞의 적보다 너무 전진했다면 정지
                float distToAhead = m_cachedTransform.position.x - m_aheadEnemy.transform.position.x;
                if (distToAhead <= TrainSpacing + 0.1f && m_aheadEnemy.IsBlocked(gap))
                {
                    isBlocked = true;
                }
            }

            // 결과 캐싱 및 반환
            m_cachedBlocked = isBlocked;
            m_lastBlockedFrame = Time.frameCount;
            return isBlocked;
        }
        #endregion
    }
}

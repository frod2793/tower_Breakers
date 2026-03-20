using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using TowerBreakers.Tower.Data;
using TowerBreakers.Player.Controller;
using TowerBreakers.Player.Logic;
using TowerBreakers.Tower.Service;

namespace TowerBreakers.Enemy.Service
{
    /// <summary>
    /// [클래스]: 적의 군집 이동 및 플레이어 푸시 로직을 담당하는 컨트롤러입니다.
    /// 이중 연결 리스트 기반의 기차 대열(Train Formation) 알고리즘을 사용합니다.
    /// </summary>
    public class EnemyPushController : MonoBehaviour
    {
        #region 에디터 설정
        [Header("이동 설정")]
        [SerializeField, Tooltip("기본 이동 속도")] 
        private float m_moveSpeed = 2f;

        [SerializeField, Tooltip("플레이어 감지 및 푸시 범위")] 
        private float m_pushRange = 1.5f;

        [SerializeField, Tooltip("플레이어 태그")] 
        private string m_playerTag = "Player";

        [Header("데이터")]
        [SerializeField, Tooltip("적 데이터 에셋")] 
        private EnemyData m_enemyData;
        #endregion

        #region 내부 필드
        // 대열 관리를 위한 이중 연결 리스트
        private EnemyPushController m_aheadEnemy;    // 내 앞의 적 (리더 방향)
        private EnemyPushController m_followerEnemy; // 내 뒤의 적 (꼬리 방향)
        
        // 캐싱 시스템
        private static int s_generation = 0;         // 대열 변화 시 증가하는 전역 세대 번호
        private EnemyPushController m_cachedLeader;  // 캐시된 리더
        private int m_cachedLeaderGen = -1;           // 캐시 시점의 세대 번호

        private bool m_cachedBlocked;                // 현재 프레임의 차단 여부 캐시
        private int m_lastBlockedFrame = -1;         // 차단 판정 수행 프레임 번호

        // 상태 변수
        private bool m_isMoving = false;
        private bool m_canAdvance = false;
        private bool m_isStunned = false;
        private bool m_isDead = false;
        private float m_stunTimer = 0f;
        private float m_trainSpacing = 1.5f;
        private EnemyType m_enemyType = EnemyType.Normal;

        private Vector3 m_lastPos;
        private bool m_shouldStop = false;

        // 의존성
        private IEnemyController m_enemyController;
        private EnemyVFXController m_vfxController;
        private PlayerLogic m_playerLogic;
        private Collider2D m_collider;
        #endregion

        #region 프로퍼티
        public float MoveSpeed { get => m_moveSpeed; set => m_moveSpeed = value; }
        public bool CanAdvance => m_canAdvance;
        public bool IsActivelyMoving => m_isMoving && m_canAdvance && !m_shouldStop && !m_isStunned && !m_isDead;
        public EnemyPushController AheadEnemy => m_aheadEnemy;
        public EnemyType Type => m_enemyType; // [추가]
        #endregion

        #region 초기화 및 설정
        private void Awake()
        {
            m_enemyController = GetComponent<IEnemyController>();
            m_vfxController = GetComponent<EnemyVFXController>();
            m_collider = GetComponent<Collider2D>();
            m_lastPos = transform.position;
        }

        /// <summary>
        /// [설명]: 적 푸시 컨트롤러를 초기화합니다.
        /// </summary>
        public void Initialize(EnemyData data, List<EnemyPushController> ignoredList, EnemyType type, int trainIndex, float spacing, PlayerLogic playerLogic)
        {
            m_enemyData = data;
            m_enemyType = type;
            m_trainSpacing = spacing;
            m_playerLogic = playerLogic;
            m_isDead = false;
            m_isStunned = false;
            m_stunTimer = 0f;

            // [리팩토링]: 이전 리스트 기반에서 연결 리스트 기반으로 전환 준비
            // 연결은 외부(SpawnService)에서 SetAheadEnemy를 통해 수행됩니다.
            
            if (m_enemyController != null)
            {
                m_enemyController.OnDeath += OnDeath;
            }

            UpdateColliderState();
        }

        /// <summary>
        /// [설명]: 내 앞의 적을 설정하여 연결 리스트를 구축합니다.
        /// </summary>
        public void SetAheadEnemy(EnemyPushController ahead)
        {
            m_aheadEnemy = ahead;
            if (m_aheadEnemy != null)
            {
                m_aheadEnemy.m_followerEnemy = this;
            }
            
            InvalidateLeaderCache();
            UpdateColliderState();
        }

        public void SetCanAdvance(bool canAdvance) => m_canAdvance = canAdvance;

        public void SetPushSettings(float moveSpeed, float pushRange)
        {
            m_moveSpeed = moveSpeed;
            m_pushRange = pushRange;
        }
        #endregion

        #region 유니티 생명주기
        private void Update()
        {
            if (m_isDead) return;

            if (HandleStun()) return;
            if (!m_isMoving || !m_canAdvance)
            {
                UpdateAnimationState();
                return;
            }

            ExecuteMovement();
            UpdateAnimationState();
            HandlePushing();
        }
        #endregion

        #region 핵심 로직 (리팩토링됨)
        private void ExecuteMovement()
        {
            if (m_playerLogic == null) return;

            float playerX = m_playerLogic.State.Position.x;
            float myX = transform.position.x;
            
            // [최적화]: 프레임 캐싱 기반 차단 판정
            m_shouldStop = CheckBlocked(playerX, myX);

            if (!m_shouldStop)
            {
                MoveForward();
                EnforceSpacing(playerX);
            }
        }

        private bool CheckBlocked(float playerX, float myX)
        {
            if (m_lastBlockedFrame == Time.frameCount) return m_cachedBlocked;

            bool blocked = false;
            float leftWallX = m_playerLogic.Config.LeftWallX;
            float distToPlayer = myX - playerX;

            // 1. 리더 조건: 플레이어와 닿아있고 플레이어가 벽에 있음
            if (m_aheadEnemy == null)
            {
                if (distToPlayer <= 0.85f && playerX <= leftWallX + 0.05f)
                {
                    blocked = true;
                }
            }
            // 2. 팔로워 조건: 앞의 적이 막혀있고 간격이 좁음
            else
            {
                if (m_aheadEnemy.CheckBlocked(playerX, m_aheadEnemy.transform.position.x))
                {
                    float distToAhead = myX - m_aheadEnemy.transform.position.x;
                    if (distToAhead <= m_trainSpacing + 0.1f)
                    {
                        blocked = true;
                    }
                }
            }

            m_cachedBlocked = blocked;
            m_lastBlockedFrame = Time.frameCount;
            return blocked;
        }

        private void MoveForward()
        {
            float targetX = -100f; // 기본적으로는 계속 왼쪽으로 전진 시도
            
            // 일반 몹인 경우 앞 적과의 간격을 유지하며 전진
            if (m_enemyType == EnemyType.Normal && m_aheadEnemy != null)
            {
                targetX = m_aheadEnemy.transform.position.x + m_trainSpacing;
            }

            transform.position = Vector3.MoveTowards(
                transform.position,
                new Vector3(targetX, transform.position.y, transform.position.z),
                m_moveSpeed * Time.deltaTime
            );
        }

        private void EnforceSpacing(float playerX)
        {
            // 플레이어를 뚫고 가지 못하게 최소 X값 강제
            float minAllowedX = playerX + 0.65f;
            if (transform.position.x < minAllowedX)
            {
                transform.position = new Vector3(minAllowedX, transform.position.y, transform.position.z);
            }
        }

        private void HandlePushing()
        {
            // [최적화]: 대열의 리더이거나 특수 개체만 플레이어를 실제로 밉니다 (힘 중첩 방지)
            if (m_aheadEnemy == null || m_enemyType != EnemyType.Normal)
            {
                PushPlayer();
            }
        }

        private void PushPlayer()
        {
            if (m_playerLogic == null) return;

            float playerX = m_playerLogic.State.Position.x;
            float myX = transform.position.x;
            float distX = myX - playerX;

            // [핵심 해결]: 적의 이동 경로 내에 플레이어가 들어온 경우
            if (distX > 0 && distX <= m_pushRange + 0.2f)
            {
                // 1. 속도 기반 푸시 (기존 유지, 저항 등 연출용)
                m_playerLogic.ApplyExternalPush(Vector2.left * m_moveSpeed);

                // 2. [추가]: 위치 기반 강제 푸시 (내동댕이 방지 핵심)
                // 적의 현재 위치를 기준으로 플레이어가 있어야 할 '최소 X 좌표'를 강제함
                // 0.65f는 적과 플레이어 사이의 시각적 간격 유지용 오프셋
                m_playerLogic.ForcePushPosition(myX - 0.65f);
            }
        }
        #endregion

        #region 상태 및 연출
        private bool HandleStun()
        {
            if (!m_isStunned) return false;

            m_stunTimer -= Time.deltaTime;
            UpdateAnimationState();

            if (m_stunTimer <= 0f)
            {
                m_isStunned = false;
            }
            return true;
        }

        private void UpdateAnimationState()
        {
            if (m_enemyController == null) return;

            if (m_isStunned)
            {
                m_enemyController.PlayAnimation(PlayerState.DAMAGED);
                return;
            }

            bool shouldMove = IsActivelyMoving && !IsLeaderStopped();
            m_enemyController.PlayAnimation(shouldMove ? PlayerState.MOVE : PlayerState.IDLE);
            m_lastPos = transform.position;
        }

        private bool IsLeaderStopped()
        {
            var leader = GetLeader();
            return leader != null && leader != this && !leader.IsActivelyMoving;
        }

        private void OnDeath(GameObject obj)
        {
            m_isDead = true;
            HandleDeathCleanup();
        }

        private void HandleDeathCleanup()
        {
            // [리팩토링]: 사망 시 대열 재연결 (중간 노드 제거 시 앞뒤 연결)
            if (m_followerEnemy != null)
            {
                m_followerEnemy.SetAheadEnemy(m_aheadEnemy);
            }
            else if (m_aheadEnemy != null)
            {
                // 내가 꼬리였다면 리더의 follower 참조 제거 (리더가 꼬리가 될 수도 있음)
                m_aheadEnemy.m_followerEnemy = null;
            }

            m_aheadEnemy = null;
            m_followerEnemy = null;
            InvalidateLeaderCache();
        }

        private void UpdateColliderState()
        {
            if (m_collider == null) return;
            
            // [최적화]: 리더이거나 특수 개체(엘리트/보스)만 물리 콜라이더 활성화
            // 일반 대열의 추종자들은 물리 연산을 하지 않음
            m_collider.enabled = (m_aheadEnemy == null) || (m_enemyType != EnemyType.Normal);
        }
        #endregion

        #region 유틸리티
        public EnemyPushController GetLeader()
        {
            if (m_cachedLeaderGen == s_generation && m_cachedLeader != null)
                return m_cachedLeader;

            var current = this;
            while (current.m_aheadEnemy != null)
            {
                current = current.m_aheadEnemy;
            }

            m_cachedLeader = current;
            m_cachedLeaderGen = s_generation;
            return current;
        }

        private static void InvalidateLeaderCache() => s_generation++;

        public void Stun(float duration)
        {
            m_isStunned = true;
            m_stunTimer += duration;
            UpdateAnimationState();
            if (m_vfxController != null) m_vfxController.FlashColor();
        }

        public void ApplyKnockback(Vector2 direction, float force)
        {
            if (m_isDead || m_enemyData == null) return;

            // [핵심 해결]: 넉백 저항성 반영 (1.0이면 완전 면역, 0.0이면 전체 밀림)
            float resistanceMultiplier = 1f - m_enemyData.KnockbackResistance;
            float finalForce = force * resistanceMultiplier;

            if (finalForce <= 0.05f) return; // 너무 작으면 무시

            Vector3 targetPos = transform.position + (Vector3)(direction.normalized * finalForce * 0.3f);
            targetPos.z = transform.position.z;

            transform.DOComplete();
            transform.DOMove(targetPos, 0.25f).SetEase(Ease.OutQuad);

            UpdateAnimationState();
            if (m_vfxController != null) m_vfxController.FlashColor();
        }

        public void StartMoving() => m_isMoving = true;
        public void StopMoving() => m_isMoving = false;
        #endregion

        #region 에디터 지원
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = (m_aheadEnemy == null) ? Color.red : Color.yellow;
            Gizmos.DrawWireSphere(transform.position, m_pushRange);
            
            if (m_aheadEnemy != null)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawLine(transform.position, m_aheadEnemy.transform.position);
            }
        }
        #endregion
    }
}

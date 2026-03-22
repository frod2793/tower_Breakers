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
        private EnemyPushController m_aheadEnemy;    // 내 앞의 적 (리더 방향)
        private EnemyPushController m_followerEnemy; // 내 뒤의 적 (꼬리 방향)
        
        private static int s_generation = 0;         
        private EnemyPushController m_cachedLeader;  
        private int m_cachedLeaderGen = -1;           

        private bool m_cachedBlocked;                
        private int m_lastBlockedFrame = -1;         

        private bool m_isMoving = false;
        private bool m_canAdvance = false;
        private bool m_isStunned = false;
        private bool m_isDead = false;
        private float m_stunTimer = 0f;
        private float m_trainSpacing = 1.5f;
        private EnemyType m_enemyType = EnemyType.Normal;

        private Vector3 m_lastPos;
        private bool m_shouldStop = false;

        private IEnemyController m_enemyController;
        private EnemyVFXController m_vfxController;
        private PlayerLogic m_playerLogic;
        private Collider2D m_collider;

        private Tween m_knockbackTween;
        #endregion

        #region 프로퍼티
        public float MoveSpeed { get => m_moveSpeed; set => m_moveSpeed = value; }
        public bool CanAdvance => m_canAdvance;
        public bool IsActivelyMoving => m_isMoving && m_canAdvance && !m_shouldStop && !m_isStunned && !m_isDead;
        public EnemyPushController AheadEnemy => m_aheadEnemy;
        public EnemyPushController FollowerEnemy => m_followerEnemy;
        public EnemyType Type => m_enemyType; 
        #endregion

        #region 초기화 및 설정
        private void Awake()
        {
            m_enemyController = GetComponent<IEnemyController>();
            m_vfxController = GetComponent<EnemyVFXController>();
            m_collider = GetComponent<Collider2D>();
            m_lastPos = transform.position;
        }

        public void Initialize(EnemyData data, List<EnemyPushController> ignoredList, EnemyType type, int trainIndex, float spacing, PlayerLogic playerLogic)
        {
            m_enemyData = data;
            m_enemyType = type;
            m_trainSpacing = spacing;
            m_playerLogic = playerLogic;
            m_isDead = false;
            m_isStunned = false;
            m_stunTimer = 0f;

            if (m_enemyController != null) m_enemyController.OnDeath += OnDeath;
            UpdateColliderState();
        }

        public void SetAheadEnemy(EnemyPushController ahead)
        {
            m_aheadEnemy = ahead;
            if (m_aheadEnemy != null) m_aheadEnemy.m_followerEnemy = this;
            InvalidateLeaderCache();
            UpdateColliderState();
        }

        public void SetCanAdvance(bool canAdvance) => m_canAdvance = canAdvance;

        // [컴파일 오류 복구]: EnemySpawnService에서 호출하는 설정 메서드 재추가
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

        #region 핵심 로직
        private void ExecuteMovement()
        {
            if (m_playerLogic == null) return;

            float playerX = m_playerLogic.State.Position.x;
            float myX = transform.position.x;
            
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

            if (m_aheadEnemy == null)
            {
                if (distToPlayer <= 0.85f && playerX <= leftWallX + 0.05f) blocked = true;
            }
            else
            {
                if (m_aheadEnemy.CheckBlocked(playerX, m_aheadEnemy.transform.position.x))
                {
                    float distToAhead = myX - m_aheadEnemy.transform.position.x;
                    if (distToAhead <= m_trainSpacing + 0.1f) blocked = true;
                }
            }

            m_cachedBlocked = blocked;
            m_lastBlockedFrame = Time.frameCount;
            return blocked;
        }

        private void MoveForward()
        {
            float targetX = -100f; 
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
            float minAllowedX = playerX + 0.65f;
            if (transform.position.x < minAllowedX)
            {
                transform.position = new Vector3(minAllowedX, transform.position.y, transform.position.z);
            }
        }

        private void HandlePushing()
        {
            if (m_aheadEnemy == null || m_enemyType != EnemyType.Normal) PushPlayer();
        }

        private void PushPlayer()
        {
            if (m_playerLogic == null) return;

            float playerX = m_playerLogic.State.Position.x;
            float myX = transform.position.x;
            float distX = myX - playerX;

            if (distX > 0 && distX <= m_pushRange + 0.2f)
            {
                m_playerLogic.ApplyExternalPush(Vector2.left * m_moveSpeed);
                m_playerLogic.ForcePushPosition(myX - 0.65f);
            }
        }
        #endregion

        #region 상태 및 연출
        private bool HandleStun()
        {
            if (!m_isStunned) return false;
            m_stunTimer -= Time.deltaTime;
            if (m_stunTimer <= 0f) m_isStunned = false;
            UpdateAnimationState();
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

            float dist = Vector3.Distance(transform.position, m_lastPos);
            bool isActuallyMoving = dist > 0.001f;
            m_lastPos = transform.position;

            m_enemyController.PlayAnimation(isActuallyMoving ? PlayerState.MOVE : PlayerState.IDLE);
        }

        private void OnDeath(GameObject obj)
        {
            m_isDead = true;
            if (m_knockbackTween != null) m_knockbackTween.Kill();
            HandleDeathCleanup();
        }

        private void HandleDeathCleanup()
        {
            if (m_followerEnemy != null) m_followerEnemy.SetAheadEnemy(m_aheadEnemy);
            else if (m_aheadEnemy != null) m_aheadEnemy.m_followerEnemy = null;
            m_aheadEnemy = null; m_followerEnemy = null;
            InvalidateLeaderCache();
        }

        private void UpdateColliderState()
        {
            if (m_collider == null) return;
            m_collider.enabled = (m_aheadEnemy == null) || (m_enemyType != EnemyType.Normal);
        }
        #endregion

        #region 유틸리티
        public EnemyPushController GetLeader()
        {
            if (m_cachedLeaderGen == s_generation && m_cachedLeader != null) return m_cachedLeader;
            var current = this;
            while (current.m_aheadEnemy != null) current = current.m_aheadEnemy;
            m_cachedLeader = current; m_cachedLeaderGen = s_generation;
            return current;
        }

        private static void InvalidateLeaderCache() => s_generation++;

        public void Stun(float duration)
        {
            m_isStunned = true;
            m_stunTimer = Mathf.Max(m_stunTimer, duration);
            UpdateAnimationState();
            if (m_vfxController != null) m_vfxController.FlashColor();
        }

        public void ApplyKnockback(Vector2 direction, float force)
        {
            if (m_isDead || m_enemyData == null) return;

            float resistanceMultiplier = 1f - m_enemyData.KnockbackResistance;
            float finalForce = force * resistanceMultiplier;
            if (finalForce <= 0.05f) return;

            Vector3 jumpOffset = (Vector3)(direction.normalized * finalForce * 0.4f);
            
            if (m_knockbackTween != null) m_knockbackTween.Kill();
            m_knockbackTween = transform.DOBlendableMoveBy(jumpOffset, 0.3f).SetEase(Ease.OutQuad);

            UpdateAnimationState();
            if (m_vfxController != null) m_vfxController.FlashColor();
        }

        public void StartMoving() => m_isMoving = true;
        public void StopMoving() => m_isMoving = false;
        #endregion
    }
}

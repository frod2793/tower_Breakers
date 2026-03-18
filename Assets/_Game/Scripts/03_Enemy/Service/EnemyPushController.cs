using System.Collections.Generic;
using UnityEngine;
using TowerBreakers.Tower.Data;
using TowerBreakers.Player.Controller;

namespace TowerBreakers.Tower.Service
{
    /// <summary>
    /// [기능]: 적 푸시 컨트롤러 (일반 몹: 기차 행렬, 엘리트/보스: 독자적 이동)
    /// </summary>
    public class EnemyPushController : MonoBehaviour
    {
        [Header("설정")]
        [Tooltip("이동 속도")]
        [SerializeField] private float m_moveSpeed = 2f;

        [Tooltip("플레이어 밀어내기 힘")]
        [SerializeField] private float m_pushForce = 5f;

        [Tooltip("밀어내기 범위")]
        [SerializeField] private float m_pushRange = 1.5f;

        [Tooltip("플레이어 태그")]
        [SerializeField] private string m_playerTag = "Player";

        [Tooltip("적 데이터")]
        [SerializeField] private EnemyData m_enemyData;

        private List<EnemyPushController> m_trainFormation = new List<EnemyPushController>();
        private Transform m_followTarget;
        private bool m_isMoving = false;
        private bool m_isStunned = false;
        private float m_stunTimer = 0f;
        private EnemyType m_enemyType = EnemyType.Normal;
        private int m_trainIndex = -1;
        private float m_trainSpacing = 1.5f;

        public float MoveSpeed
        {
            get => m_moveSpeed;
            set => m_moveSpeed = value;
        }

        private IEnemyController m_enemyController;
        private EnemyVFXController m_vfxController;
        private Vector3 m_lastPos;

        public void Initialize(EnemyData data, List<EnemyPushController> formation, EnemyType type, int trainIndex, float spacing)
        {
            m_enemyData = data;
            m_trainFormation = formation;
            m_enemyType = type;
            m_trainIndex = trainIndex;
            m_trainSpacing = spacing;

            if (type == EnemyType.Normal && formation != null && trainIndex >= 0)
            {
                m_trainFormation.Add(this);
            }

            m_enemyController = GetComponent<IEnemyController>();
            m_vfxController = GetComponent<EnemyVFXController>();
            m_lastPos = transform.position;
        }

        public void SetFollowTarget(Transform target)
        {
            m_followTarget = target;
        }

        public void SetPushSettings(float moveSpeed, float pushForce, float pushRange)
        {
            m_moveSpeed = moveSpeed;
            m_pushForce = pushForce;
            m_pushRange = pushRange;
            
            Debug.Log($"[EnemyPushController] SetPushSettings - MoveSpeed: {moveSpeed}, PushForce: {pushForce}, PushRange: {pushRange}");
        }

        private void Update()
        {
            if (m_isStunned)
            {
                m_stunTimer -= Time.deltaTime;
                
                // [추가]: 스턴 상태에서도 애니메이션은 DAMAGED로 유지되도록 갱신
                UpdateAnimationState();

                if (m_stunTimer <= 0f)
                {
                    m_isStunned = false;
                    Debug.Log("[EnemyPushController] 경직 해제");
                }
                return;
            }

            if (!m_isMoving)
            {
                return;
            }

            if (m_enemyType == EnemyType.Normal)
            {
                MoveAsTrain();
            }
            else
            {
                MoveIndependently();
            }

            UpdateAnimationState();
            PushPlayer();
        }

        private void UpdateAnimationState()
        {
            if (m_enemyController == null) return;

            if (m_isStunned)
            {
                m_enemyController.PlayAnimation(PlayerState.DAMAGED);
                return;
            }

            // 실제 위치 변화량을 체크하여 이동 애니메이션 재생 여부 결정
            float dist = Vector3.Distance(transform.position, m_lastPos);
            m_lastPos = transform.position;

            if (dist > 0.001f)
            {
                m_enemyController.PlayAnimation(PlayerState.MOVE);
            }
            else
            {
                m_enemyController.PlayAnimation(PlayerState.IDLE);
            }
        }

        public void Stun(float duration)
        {
            m_isStunned = true;
            // [개선]: 기절 시간 중첩(+=) 적용. 연속 패링 시 기절 시간이 합산됩니다.
            m_stunTimer += duration;
            
            // 스턴 즉시 피격 애니메이션 및 VFX 재생
            UpdateAnimationState();
            if (m_vfxController != null) m_vfxController.FlashColor();
            
            Debug.Log($"[EnemyPushController] 경직(중첩) 적용 - 추가 시간: {duration}s, 총 남은 시간: {m_stunTimer:F2}s");
        }

        /// <summary>
        /// [설명]: 적에게 넉백(밀어내기)을 적용합니다. 
        /// </summary>
        /// <param name="direction">밀려날 방향</param>
        /// <param name="force">밀려날 힘/거리</param>
        public void ApplyKnockback(Vector2 direction, float force)
        {
            Vector3 beforePos = transform.position;
            // [개선]: Rigidbody 유무와 상관없이 확실히 밀려나도록 위치 기반으로 처리
            // force가 넉백 거리(Distance)의 역할을 하도록 구성
            Vector3 knockbackVector = (Vector3)(direction.normalized * force * 0.1f);
            
            // Z축 고정 및 위치 이동
            knockbackVector.z = 0;
            transform.position += knockbackVector;

            // 넉백 발생 시 즉시 피격 애니메이션 및 VFX 재생
            UpdateAnimationState();
            if (m_vfxController != null) m_vfxController.FlashColor();
            
            Debug.Log($"[EnemyPushController] 넉백 적용 - 대상: {name}, 방향: {direction}, 힘: {force}, 위치: {beforePos} -> {transform.position}");
        }

        private void MoveAsTrain()
        {
            float targetX;

            if (m_followTarget != null)
            {
                targetX = m_followTarget.position.x + m_trainSpacing;
            }
            else
            {
                int myIndexInFormation = m_trainFormation.IndexOf(this);
                if (myIndexInFormation > 0)
                {
                    var frontEnemy = m_trainFormation[myIndexInFormation - 1];
                    if (frontEnemy != null)
                    {
                        targetX = frontEnemy.transform.position.x + m_trainSpacing;
                    }
                    else
                    {
                        targetX = GetSafeTargetX();
                    }
                }
                else
                {
                    targetX = GetSafeTargetX();
                }
            }

            transform.position = Vector3.MoveTowards(
                transform.position,
                new Vector3(targetX, transform.position.y, transform.position.z),
                m_moveSpeed * Time.deltaTime
            );
        }

        private void MoveIndependently()
        {
            transform.position = Vector3.MoveTowards(
                transform.position,
                new Vector3(transform.position.x - 10f, transform.position.y, transform.position.z),
                m_moveSpeed * Time.deltaTime
            );
        }

        private float GetSafeTargetX()
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                return player.transform.position.x + 2f;
            }
            return transform.position.x;
        }

        private void PushPlayer()
        {
            var colliders = Physics2D.OverlapCircleAll(transform.position, m_pushRange);
            
            foreach (var collider in colliders)
            {
                if (collider.CompareTag(m_playerTag))
                {
                    var pushReceiver = collider.GetComponent<PlayerPushReceiver>();
                    if (pushReceiver != null)
                    {
                        Vector2 pushDirection = (collider.transform.position - transform.position).normalized;
                        pushDirection.y = 0; // Y축 이동 방지
                        pushReceiver.Push(pushDirection * m_pushForce);
                    }
                }
            }
        }

        public void StopMoving()
        {
            m_isMoving = false;
        }

        public void StartMoving()
        {
            m_isMoving = true;
        }

        private void OnDestroy()
        {
            if (m_enemyType == EnemyType.Normal && m_trainFormation != null && m_trainFormation.Contains(this))
            {
                m_trainFormation.Remove(this);
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = GetGizmoColor();
            Gizmos.DrawWireSphere(transform.position, m_pushRange);
        }

        private Color GetGizmoColor()
        {
            switch (m_enemyType)
            {
                case EnemyType.Normal:
                    return Color.yellow;
                case EnemyType.Elite:
                    return Color.magenta;
                case EnemyType.Boss:
                    return Color.red;
                default:
                    return Color.white;
            }
        }
    }
}

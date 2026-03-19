using System.Collections.Generic;
using DG.Tweening;
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

        [Tooltip("밀어내기 범위")]
        [SerializeField] private float m_pushRange = 1.5f;

        [Tooltip("플레이어 태그")]
        [SerializeField] private string m_playerTag = "Player";

        [Tooltip("적 데이터")]
        [SerializeField] private EnemyData m_enemyData;

        private List<EnemyPushController> m_trainFormation = new List<EnemyPushController>();
        private Transform m_followTarget;
        private bool m_isMoving = false;
        private bool m_canAdvance = false; // [추가]: 플레이어 도착 후 진격 허용 여부
        private bool m_isStunned = false;
        private bool m_isDead = false;
        private float m_stunTimer = 0f;
        private EnemyType m_enemyType = EnemyType.Normal;
        private int m_trainIndex = -1;
        private float m_trainSpacing = 1.5f;

        public float MoveSpeed
        {
            get => m_moveSpeed;
            set => m_moveSpeed = value;
        }

        public bool CanAdvance => m_canAdvance;

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
            if (m_enemyController != null)
            {
                m_enemyController.OnDeath += _ => m_isDead = true;
            }
            m_vfxController = GetComponent<EnemyVFXController>();
            m_lastPos = transform.position;
        }

        public void SetCanAdvance(bool canAdvance)
        {
            m_canAdvance = canAdvance;
        }

        public void SetFollowTarget(Transform target)
        {
            m_followTarget = target;
        }

        public void SetPushSettings(float moveSpeed, float pushRange)
        {
            m_moveSpeed = moveSpeed;
            m_pushRange = pushRange;
            
            Debug.Log($"[EnemyPushController] SetPushSettings - MoveSpeed: {moveSpeed}, PushRange: {pushRange}");
        }

        private void Update()
        {
            if (m_isDead) return;

            if (m_isStunned)
            {
                m_stunTimer -= Time.deltaTime;
                UpdateAnimationState(false); // 스턴 중에는 이동 애니메이션 불필요

                if (m_stunTimer <= 0f)
                {
                    m_isStunned = false;
                    Debug.Log("[EnemyPushController] 경직 해제");
                }
                return;
            }

            // [수정]: m_isMoving이 true이고, 실질적인 진격 허가(m_canAdvance)가 떨어졌을 때만 이동
            if (!m_isMoving || !m_canAdvance)
            {
                UpdateAnimationState(false);
                return;
            }

            // [개선]: 플레이어가 왼쪽 벽에 압착되었을 때만 멈추도록 체크
            bool shouldStop = false;
            var playerView = GameObject.FindFirstObjectByType<TowerBreakers.Player.View.PlayerView>();
            if (playerView != null)
            {
                var playerLogicField = typeof(TowerBreakers.Player.View.PlayerView).GetField("m_logic", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var playerLogic = playerLogicField?.GetValue(playerView) as TowerBreakers.Player.Logic.PlayerLogic;

                float leftWallX = (playerLogic != null) ? playerLogic.Config.LeftWallX : -8f;
                float playerX = playerView.transform.position.x;
                float distToPlayer = transform.position.x - playerX;

                // 조건: (플레이어와의 거리가 0.8f 이하) AND (플레이어가 왼쪽 벽에 거의 닿음)
                if (distToPlayer <= 0.8f && playerX <= leftWallX + 0.2f)
                {
                    shouldStop = true;
                }

                if (!shouldStop)
                {
                    if (m_enemyType == EnemyType.Normal) MoveAsTrain();
                    else MoveIndependently();

                    // [핵심]: 물리적 겹침 방지 강제 클램핑 (플레이어를 절대로 뚫고 지나가지 못함)
                    float minAllowedX = playerX + 0.6f;
                    if (transform.position.x < minAllowedX)
                    {
                        transform.position = new Vector3(minAllowedX, transform.position.y, transform.position.z);
                    }
                }
            }
            else
            {
                // 플레이어가 없는 경우에도 계속 진격
                if (m_enemyType == EnemyType.Normal) MoveAsTrain();
                else MoveIndependently();
            }

            UpdateAnimationState(shouldStop);
            PushPlayer();
        }

        /// <summary>
        /// [설명]: 이 적이 현재 플레이어 압착이나 기획적 이유로 실질적으로 멈춰있는 상태인지 반환합니다.
        /// </summary>
        public bool IsActuallyStopping => m_isStunned || m_isDead || !m_isMoving;

        /// <summary>
        /// [설명]: 군집(기차 대열) 내에서 누가 하나라도 멈춰야 하는 상황(압착 등)인지 확인합니다.
        /// </summary>
        private bool IsAnyInFormationStopping()
        {
            if (m_trainFormation == null || m_trainFormation.Count == 0) return false;

            // [기반 수정]: 대열의 리더(Index 0)가 플레이어에 가로막혀 멈췄다면 대열 전체가 멈춘 것으로 간주
            var leader = m_trainFormation[0];
            if (leader != null && leader != this)
            {
                // 플레이어 뷰를 찾아 현재 압착 상태인지 간접적으로 판단 (리더의 로직과 동일 조건)
                var playerView = GameObject.FindFirstObjectByType<TowerBreakers.Player.View.PlayerView>();
                if (playerView != null)
                {
                    float distToPlayer = leader.transform.position.x - playerView.transform.position.x;
                    // 리더가 플레이어와 밀착(0.85f 이하)했다면 대열 전체 애니메이션 정지
                    if (distToPlayer <= 0.85f) return true;
                }
            }
            return false;
        }

        private void UpdateAnimationState(bool shouldStop)
        {
            if (m_enemyController == null) return;

            if (m_isStunned)
            {
                m_enemyController.PlayAnimation(PlayerState.DAMAGED);
                return;
            }

            // [개선]: 물리적으로 막혀 있더라도 진격 의지가 있다면(Moving 중 && !shouldStop) MOVE 애니메이션 유지
            // [수정]: 군집 전체 동기화를 위해 리더가 멈췄는지도 함께 체크
            bool isFormationStopping = IsAnyInFormationStopping();

            if (m_isMoving && !shouldStop && !isFormationStopping)
            {
                m_enemyController.PlayAnimation(PlayerState.MOVE);
            }
            else
            {
                m_enemyController.PlayAnimation(PlayerState.IDLE);
            }

            m_lastPos = transform.position;
        }

        public void Stun(float duration)
        {
            m_isStunned = true;
            // [개선]: 기절 시간 중첩(+=) 적용. 연속 패링 시 기절 시간이 합산됩니다.
            m_stunTimer += duration;
            
            // 스턴 즉시 피격 애니메이션 및 VFX 재생
            UpdateAnimationState(false);
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
            // [수정]: 순간이동 방식에서 DOTween을 사용한 부드러운 넉백으로 변경
            Vector3 targetPos = transform.position + (Vector3)(direction.normalized * force * 0.3f);
            targetPos.z = transform.position.z; // Z축 유지

            transform.DOComplete(); // 이전 넉백 취소
            transform.DOMove(targetPos, 0.25f).SetEase(Ease.OutQuad);

            // 넉백 발생 시 즉시 피격 애니메이션 및 VFX 재생
            UpdateAnimationState(false);
            if (m_vfxController != null) m_vfxController.FlashColor();
            
            Debug.Log($"[EnemyPushController] 부드러운 넉백 적용 - 대상: {name}, 힘: {force}");
        }

        private void MoveAsTrain()
        {
            float targetX = 0;

            if (m_followTarget != null)
            {
                targetX = m_followTarget.position.x + m_trainSpacing;
            }
            else
            {
                // [개선]: 공유 리스트를 통해 실시간 대열에서의 내 위치(Index)를 확인
                int myIndexInFormation = m_trainFormation != null ? m_trainFormation.IndexOf(this) : -1;
                
                if (myIndexInFormation > 0)
                {
                    // 앞의 적이 있는지 확인 (공유 리스트이므로 실시간 반영됨)
                    var frontEnemy = m_trainFormation[myIndexInFormation - 1];
                    if (frontEnemy != null)
                    {
                        targetX = frontEnemy.transform.position.x + m_trainSpacing;
                    }
                    else
                    {
                        // 예외 상황: 앞의 적이 null이면(파괴 중 등) 왼쪽 끝까지 진격
                        targetX = -100f;
                    }
                }
                else
                {
                    // 내가 리더(index 0)라면 플레이어를 넘어서 왼쪽 끝까지 진격 시도
                    targetX = -100f;
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
            // 독립 몹도 마찬가지로 왼쪽 끝까지 진격 시도
            transform.position = Vector3.MoveTowards(
                transform.position,
                new Vector3(-100f, transform.position.y, transform.position.z),
                m_moveSpeed * Time.deltaTime
            );
        }

        private void PushPlayer()
        {
            var colliders = Physics2D.OverlapCircleAll(transform.position, m_pushRange);
            
            foreach (var collider in colliders)
            {
                if (collider.CompareTag(m_playerTag))
                {
                    // [개선]: 단순히 Range 안에 있는 것뿐만 아니라, 적이 플레이어보다 오른쪽에 있을 때만(진격 방향) 푸시 적용
                    float distX = transform.position.x - collider.transform.position.x;
                    if (distX > 0 && distX <= m_pushRange + 0.2f)
                    {
                        var pushReceiver = collider.GetComponent<PlayerPushReceiver>();
                        if (pushReceiver != null)
                        {
                            Vector2 pushDirection = Vector2.left; // 항상 왼쪽으로 밀어냄
                            // [리팩토링]: 별도의 PushForce 대신 적의 이동 속도(MoveSpeed)를 푸시 속도로 전달
                            pushReceiver.Push(pushDirection * m_moveSpeed);
                        }
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

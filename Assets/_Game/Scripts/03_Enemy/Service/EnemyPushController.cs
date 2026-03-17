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
        private EnemyType m_enemyType = EnemyType.Normal;
        private int m_trainIndex = -1;
        private float m_trainSpacing = 1.5f;

        public float MoveSpeed
        {
            get => m_moveSpeed;
            set => m_moveSpeed = value;
        }

        public void Initialize(EnemyData data, List<EnemyPushController> formation, EnemyType type, int trainIndex, float spacing)
        {
            m_enemyData = data;
            m_trainFormation = formation;
            m_enemyType = type;
            m_trainIndex = trainIndex;
            m_trainSpacing = spacing;

            if (m_enemyData != null)
            {
                m_moveSpeed = m_enemyData.MoveSpeed;
            }

            if (type == EnemyType.Normal && formation != null && trainIndex >= 0)
            {
                m_trainFormation.Add(this);
            }

            Debug.Log($"[EnemyPushController] 초기화 - 타입: {type}, 인덱스: {trainIndex}");
        }

        public void SetFollowTarget(Transform target)
        {
            m_followTarget = target;
        }

        private void Update()
        {
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

            PushPlayer();
        }

        private void MoveAsTrain()
        {
            float targetX;

            if (m_followTarget != null)
            {
                targetX = m_followTarget.position.x - m_trainSpacing;
            }
            else
            {
                int myIndexInFormation = m_trainFormation.IndexOf(this);
                if (myIndexInFormation > 0)
                {
                    var frontEnemy = m_trainFormation[myIndexInFormation - 1];
                    if (frontEnemy != null)
                    {
                        targetX = frontEnemy.transform.position.x - m_trainSpacing;
                    }
                    else
                    {
                        targetX = transform.position.x - 10f;
                    }
                }
                else
                {
                    targetX = transform.position.x - 10f;
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

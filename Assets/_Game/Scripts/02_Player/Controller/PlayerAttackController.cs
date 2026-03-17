using System;
using System.Collections.Generic;
using UnityEngine;
using TowerBreakers.Player.Stat;
using TowerBreakers.Tower.Service;

namespace TowerBreakers.Player.Controller
{
    /// <summary>
    /// [기능]: 플레이어 공격 컨트롤러
    /// </summary>
    public class PlayerAttackController : MonoBehaviour
    {
        [Header("설정")]
        [Tooltip("공격 범위")]
        [SerializeField] private float m_attackRange = 2f;

        [Tooltip("공격 쿨타임 (초)")]
        [SerializeField] private float m_attackCooldown = 0.5f;

        [Tooltip("공격 키")]
        [SerializeField] private KeyCode m_attackKey = KeyCode.Z;

        [Header("참조")]
        [Tooltip("공격 효과 위치")]
        [SerializeField] private Transform m_attackPoint;

        [Tooltip("플레이어 스탯 서비스")]
        [SerializeField] private IPlayerStatService m_playerStatService;

        private float m_lastAttackTime;
        private bool m_isAttacking;

        public event Action<GameObject> OnAttackHit;

        public float AttackRange
        {
            get => m_attackRange;
            set => m_attackRange = value;
        }

        public float AttackCooldown
        {
            get => m_attackCooldown;
            set => m_attackCooldown = value;
        }

        public void Initialize(IPlayerStatService playerStatService)
        {
            m_playerStatService = playerStatService;
            m_lastAttackTime = -m_attackCooldown;
        }

        private void Update()
        {
            if (Input.GetKey(m_attackKey) || Input.GetMouseButton(0))
            {
                TryAttack();
            }
        }

        private void TryAttack()
        {
            if (Time.time - m_lastAttackTime < m_attackCooldown)
            {
                return;
            }

            var enemy = FindNearestEnemy();
            if (enemy != null)
            {
                Attack(enemy);
            }
        }

        private GameObject FindNearestEnemy()
        {
            var colliders = Physics2D.OverlapCircleAll(GetAttackPosition(), m_attackRange);
            
            GameObject nearestEnemy = null;
            float nearestDistance = float.MaxValue;

            foreach (var collider in colliders)
            {
                var enemyController = collider.GetComponent<IEnemyController>();
                if (enemyController != null)
                {
                    float distance = Vector2.Distance(transform.position, collider.transform.position);
                    if (distance < nearestDistance)
                    {
                        nearestDistance = distance;
                        nearestEnemy = collider.gameObject;
                    }
                }
            }

            return nearestEnemy;
        }

        private Vector2 GetAttackPosition()
        {
            return m_attackPoint != null ? m_attackPoint.position : transform.position;
        }

        public void Attack(GameObject target)
        {
            if (m_playerStatService == null)
            {
                Debug.LogWarning("[PlayerAttackController] 플레이어 스탯 서비스가 null입니다.");
                return;
            }

            m_lastAttackTime = Time.time;
            m_isAttacking = true;

            var enemyController = target.GetComponent<IEnemyController>();
            if (enemyController != null)
            {
                float damage = m_playerStatService.TotalAttack;
                enemyController.TakeDamage(damage);
                OnAttackHit?.Invoke(target);
            }

            m_isAttacking = false;
        }

        public void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(GetAttackPosition(), m_attackRange);
        }
    }
}

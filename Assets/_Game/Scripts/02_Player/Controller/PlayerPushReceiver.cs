using System;
using UnityEngine;

namespace TowerBreakers.Player.Controller
{
    /// <summary>
    /// [기능]: 플레이어 밀림 수신 컨트롤러
    /// </summary>
    public class PlayerPushReceiver : MonoBehaviour
    {
        [Header("설정")]
        [Tooltip("밀림 저항력")]
        [SerializeField] private float m_resistance = 0.5f;

        [Tooltip("왼쪽 벽 위치 (X 좌표)")]
        [SerializeField] private float m_leftWallX = -8f;

        [Tooltip("벽 도달 시 체력 감소량")]
        [SerializeField] private int m_damagePerHit = 1;

        [Tooltip("밀림 후 쿨다운 (초)")]
        [SerializeField] private float m_damageCooldown = 1f;

        [Header("상태")]
        [SerializeField] private int m_currentHealth;
        [SerializeField] private float m_lastDamageTime;

        public event Action<int> OnHealthChanged;
        public event Action OnPlayerDeath;

        public int CurrentHealth => m_currentHealth;

        public void Initialize(int maxHealth)
        {
            m_currentHealth = maxHealth;
            Debug.Log($"[PlayerPushReceiver] 초기화 - 체력: {m_currentHealth}");
        }

        private void Update()
        {
            CheckLeftWall();
        }

        public void Push(Vector2 force)
        {
            transform.Translate(force * Time.deltaTime);
        }

        private void CheckLeftWall()
        {
            if (transform.position.x <= m_leftWallX)
            {
                if (Time.time - m_lastDamageTime >= m_damageCooldown)
                {
                    TakeDamage();
                }
            }
        }

        private void TakeDamage()
        {
            m_currentHealth -= m_damagePerHit;
            m_lastDamageTime = Time.time;

            Debug.Log($"[PlayerPushReceiver] 피해! 남은 체력: {m_currentHealth}");

            OnHealthChanged?.Invoke(m_currentHealth);

            if (m_currentHealth <= 0)
            {
                Die();
            }
        }

        private void Die()
        {
            Debug.Log("[PlayerPushReceiver] 플레이어 사망!");
            OnPlayerDeath?.Invoke();
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(
                new Vector3(m_leftWallX, transform.position.y - 10f, 0),
                new Vector3(m_leftWallX, transform.position.y + 10f, 0)
            );
        }
    }
}

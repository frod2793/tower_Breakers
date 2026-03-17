using System;
using UnityEngine;
using TowerBreakers.Player.Logic;

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

        [Header("참조")]
        [Tooltip("플레이어 로직")]
        [SerializeField] private PlayerLogic m_playerLogic;

        [Header("상태")]
        [SerializeField] private int m_currentHealth;
        [SerializeField] private float m_lastDamageTime;

        public event Action<int> OnHealthChanged;
        public event Action OnPlayerDeath;

        public int CurrentHealth => m_currentHealth;

        public void Initialize(int maxHealth, PlayerLogic playerLogic = null)
        {
            m_playerLogic = playerLogic;
            if (m_playerLogic != null)
            {
                m_playerLogic.InitializeHealth(maxHealth);
            }
            Debug.Log($"[PlayerPushReceiver] 초기화 완료");
        }

        private void Update()
        {
            CheckLeftWall();
        }

        public void Push(Vector2 force)
        {
            if (m_playerLogic != null)
            {
                // 밀림 저항력을 적용하여 로직에 전달
                m_playerLogic.ApplyExternalPush(force * m_resistance);
            }
            else
            {
                // 로직이 없는 경우의 폴백
                transform.Translate(force * m_resistance * Time.deltaTime);
            }
        }

        private void CheckLeftWall()
        {
            // 좌측 벽 충돌 체크 (센서 역할)
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
            m_lastDamageTime = Time.time;

            if (m_playerLogic != null)
            {
                m_playerLogic.TakeDamage(m_damagePerHit);
                Debug.Log($"[PlayerPushReceiver] 벽 충돌 피해 발생 (남은 체력: {m_playerLogic.State.Health})");
            }

            OnHealthChanged?.Invoke(m_playerLogic?.State.Health ?? 0);
        }

        private void Die()
        {
            // 실제 사망 처리는 PlayerLogic에서 수행하고 View가 반응함
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

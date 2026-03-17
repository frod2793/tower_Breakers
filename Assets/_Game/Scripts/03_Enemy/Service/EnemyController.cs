using System;
using UnityEngine;
using TowerBreakers.Tower.Data;

namespace TowerBreakers.Tower.Service
{
    /// <summary>
    /// [기능]: 적 컨트롤러 (적 프리팹에 부착)
    /// </summary>
    public class EnemyController : MonoBehaviour, IEnemyController
    {
        [Header("데이터")]
        [SerializeField] private EnemyData m_data;

        [Header("현재 스탯")]
        [SerializeField] private float m_currentHealth;

        public event Action<GameObject> OnDeath;

        public float CurrentHealth => m_currentHealth;
        public float MaxHealth => m_data != null ? m_data.Health : 0f;
        public float Attack => m_data != null ? m_data.Attack : 0f;

        public void Initialize(EnemyData data)
        {
            m_data = data;
            m_currentHealth = data.Health;

            Debug.Log($"[EnemyController] 초기화 - {data.EnemyName}, 체력: {m_currentHealth}");
        }

        public void TakeDamage(float damage)
        {
            if (m_currentHealth <= 0)
            {
                return;
            }

            m_currentHealth -= damage;

            Debug.Log($"[EnemyController] 피해 입음 - 남은 체력: {m_currentHealth}");

            if (m_currentHealth <= 0)
            {
                Die();
            }
        }

        private void Die()
        {
            Debug.Log($"[EnemyController] 적 사망 - {m_data?.EnemyName}");

            OnDeath?.Invoke(gameObject);
        }

        #region 공개 메서드
        /// <summary>
        /// [설명]: 적의 애니메이션을 0 프레임부터 다시 시작하여 군집 간 싱크를 맞춥니다.
        /// </summary>
        public void SyncAnimation()
        {
            // [설명]: SPUM 프리팹 내부의 Animator를 찾습니다.
            var animator = GetComponentInChildren<Animator>();
            if (animator != null)
            {
                // 현재 재생 중인 모든 레이어의 애니메이션을 0프레임부터 다시 재생
                for (int i = 0; i < animator.layerCount; i++)
                {
                    var stateInfo = animator.GetCurrentAnimatorStateInfo(i);
                    animator.Play(stateInfo.fullPathHash, i, 0f);
                }
            }
        }
        #endregion

        public void OnDestroy()
        {
            OnDeath = null;
        }
    }
}

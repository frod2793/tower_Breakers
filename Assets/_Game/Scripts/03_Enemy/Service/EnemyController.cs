using System;
using UnityEngine;
using TowerBreakers.Tower.Data;

namespace TowerBreakers.Tower.Service
{
    /// <summary>
    /// [기능]: 적 컨트롤러 (일반 몹, 엘리트 전용)
    /// </summary>
    public class EnemyController : MonoBehaviour, IEnemyController
    {
        [Header("데이터")]
        [SerializeField] private EnemyData m_data;

        [Header("현재 스탯")]
        [SerializeField] private float m_currentHealth;

        [Header("애니메이션")]
        [Tooltip("SPUM 프리팹 (자동으로 자식에서 찾음)")]
        [SerializeField] private SPUM_Prefabs m_spumPrefabs;

        public event Action<GameObject> OnDeath;

        private PlayerState m_lastState = PlayerState.IDLE;
        public float CurrentHealth => m_currentHealth;
        public float MaxHealth => m_data != null ? m_data.Health : 0f;
        public float Attack => m_data != null ? m_data.Attack : 0f;

        private void Awake()
        {
            if (m_spumPrefabs == null)
            {
                m_spumPrefabs = GetComponentInChildren<SPUM_Prefabs>();
            }

            if (m_spumPrefabs == null)
            {
                m_spumPrefabs = transform.GetChild(0).GetComponent<SPUM_Prefabs>();
            }

            if (m_spumPrefabs != null)
            {
                m_spumPrefabs.OverrideControllerInit();
            }
        }

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

        /// <summary>
        /// [설명]: 적의 애니메이션 상태를 변경합니다. (중복 호출 방지 포함)
        /// </summary>
        /// <param name="state">변경할 상태</param>
        public void PlayAnimation(PlayerState state)
        {
            if (m_lastState == state) return;
            m_lastState = state;

            if (m_spumPrefabs != null)
            {
                m_spumPrefabs.PlayAnimation(state, 0);
            }
        }

        #region 공개 메서드
        /// <summary>
        /// [설명]: 적의 애니메이션을 0 프레임부터 다시 시작하여 군집 간 싱크를 맞춥니다.
        /// </summary>
        public void SyncAnimation()
        {
            if (m_spumPrefabs != null)
            {
                m_spumPrefabs._anim.Rebind();
                m_spumPrefabs._anim.Play(0, 0, 0f);
            }
            else
            {
                var animator = GetComponentInChildren<Animator>();
                if (animator != null)
                {
                    for (int i = 0; i < animator.layerCount; i++)
                    {
                        var stateInfo = animator.GetCurrentAnimatorStateInfo(i);
                        animator.Play(stateInfo.fullPathHash, i, 0f);
                    }
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

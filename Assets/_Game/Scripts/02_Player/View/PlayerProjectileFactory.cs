using UnityEngine;
using System.Collections.Generic;
using VContainer;

namespace TowerBreakers.Player.View
{
    /// <summary>
    /// [설명]: 플레이어 발사체를 관리하는 풀링 팩토리입니다.
    /// </summary>
    public class PlayerProjectileFactory : MonoBehaviour
    {
        #region 필드
        [SerializeField, Tooltip("미사일 풀 크기")]
        private int m_missilePoolSize = 10;

        [SerializeField, Tooltip("참격 풀 크기")]
        private int m_slashPoolSize = 5;

        private readonly Queue<PlayerGuidedMissile> m_missilePool = new Queue<PlayerGuidedMissile>();
        private readonly Queue<PlayerSlashProjectile> m_slashPool = new Queue<PlayerSlashProjectile>();

        private PlayerGuidedMissile m_missilePrefab;
        private PlayerSlashProjectile m_slashPrefab;
        
        // 의존성 주입
        private TowerBreakers.Effects.EffectManager m_effectManager;
        private TowerBreakers.Core.Events.IEventBus m_eventBus;
        #endregion

        #region 초기화
        /// <summary>
        /// [설명]: VContainer 등을 통해 의존성을 주입받습니다.
        /// </summary>
        [VContainer.Inject]
        public void Construct(TowerBreakers.Effects.EffectManager effectManager, TowerBreakers.Core.Events.IEventBus eventBus)
        {
            m_effectManager = effectManager;
            m_eventBus = eventBus;
        }

        public void Initialize()
        {
            // 수동으로 의존성을 찾는 폴백 (DI 미사용 시)
            if (m_effectManager == null)
            {
                m_effectManager = FindFirstObjectByType<TowerBreakers.Effects.EffectManager>();
            }
            if (m_eventBus == null)
            {
                m_eventBus = FindFirstObjectByType<VContainer.Unity.LifetimeScope>()?.Container.Resolve<TowerBreakers.Core.Events.IEventBus>();
            }

            InitializeMissilePool();
            InitializeSlashPool();
        }
        #endregion

        #region 공개 메서드
        public void SetMissilePrefab(GameObject prefab)
        {
            if (prefab != null) m_missilePrefab = prefab.GetComponent<PlayerGuidedMissile>();
        }

        public void SetSlashPrefab(GameObject prefab)
        {
            if (prefab != null) m_slashPrefab = prefab.GetComponent<PlayerSlashProjectile>();
        }

        public PlayerGuidedMissile GetMissile()
        {
            if (m_missilePool.Count > 0) return m_missilePool.Dequeue();
            return CreateMissile();
        }

        public void ReturnMissile(PlayerGuidedMissile missile)
        {
            if (missile == null) return;
            missile.Deactivate();
            missile.transform.SetParent(transform);
            m_missilePool.Enqueue(missile);
        }

        public PlayerSlashProjectile GetSlash()
        {
            if (m_slashPool.Count > 0) return m_slashPool.Dequeue();
            return CreateSlash();
        }

        public void ReturnSlash(PlayerSlashProjectile slash)
        {
            if (slash == null) return;
            slash.Deactivate();
            slash.transform.SetParent(transform);
            m_slashPool.Enqueue(slash);
        }
        #endregion

        #region 내부 메서드
        private void InitializeMissilePool()
        {
            if (m_missilePrefab == null || m_missilePool.Count >= m_missilePoolSize) return;
            int countToCreate = m_missilePoolSize - m_missilePool.Count;
            for (int i = 0; i < countToCreate; i++)
            {
                m_missilePool.Enqueue(CreateMissile(true));
            }
        }

        private void InitializeSlashPool()
        {
            if (m_slashPrefab == null || m_slashPool.Count >= m_slashPoolSize) return;
            int countToCreate = m_slashPoolSize - m_slashPool.Count;
            for (int i = 0; i < countToCreate; i++)
            {
                m_slashPool.Enqueue(CreateSlash(true));
            }
        }

        private PlayerGuidedMissile CreateMissile(bool deactivate = false)
        {
            if (m_missilePrefab == null) return null;
            var missile = Instantiate(m_missilePrefab, transform);
            if (deactivate) missile.gameObject.SetActive(false);
            return missile;
        }

        private PlayerSlashProjectile CreateSlash(bool deactivate = false)
        {
            if (m_slashPrefab == null) return null;
            var slash = Instantiate(m_slashPrefab, transform);
            if (deactivate) slash.gameObject.SetActive(false);
            return slash;
        }
        #endregion
    }
}

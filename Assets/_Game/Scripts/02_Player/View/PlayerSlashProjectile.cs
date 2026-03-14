using UnityEngine;
using TowerBreakers.Core.Interfaces;
using TowerBreakers.Effects;
using System.Collections.Generic;
using TowerBreakers.Enemy.Logic;

namespace TowerBreakers.Player.View
{
    /// <summary>
    /// [설명]: 플레이어의 참격 발사체입니다. 직선으로 이동하며 적을 관통하고 슬로우 디버프를 적용합니다.
    /// </summary>
    public class PlayerSlashProjectile : PlayerProjectile
    {
        #region 필드
        [SerializeField, Tooltip("최대 이동 거리")]
        private float m_maxDistance = 10f;
        private float m_maxDistanceSq;

        private Vector2 m_startPosition;
        private Vector2 m_direction;
        private Collider2D m_selfCollider;
        private Collider2D[] m_childColliders;
        private readonly HashSet<GameObject> m_hitEnemies = new HashSet<GameObject>();

        private float m_slowMultiplier = 0.5f;
        private float m_slowDuration = 2.0f;
        private float m_knockbackDistance = 2.0f;
        private float m_knockbackDuration = 0.25f;
        private float m_stunDuration = 0.5f;
        #endregion

        #region 유니티 생명주기
        protected override void Awake()
        {
            base.Awake();
            m_selfCollider = GetComponent<Collider2D>();
            if (m_selfCollider != null)
            {
                m_selfCollider.isTrigger = true;
                m_selfCollider.enabled = false;
            }
            m_childColliders = GetComponentsInChildren<Collider2D>();
        }
        #endregion

        #region 공개 메서드
        public void InitializeWithSlow(int damage, float speed, float lifetime, int ownerLayer, float maxDistance, float slowMultiplier, float slowDuration, EffectManager effectManager = null, float knockbackDistance = 2.0f, float knockbackDuration = 0.25f, float stunDuration = 0.5f, Core.Events.IEventBus eventBus = null)
        {
            Initialize(damage, speed, lifetime, ownerLayer, effectManager, eventBus);
            m_maxDistance = maxDistance;
            m_maxDistanceSq = maxDistance * maxDistance;
            m_slowMultiplier = slowMultiplier;
            m_slowDuration = slowDuration;
            m_knockbackDistance = knockbackDistance;
            m_knockbackDuration = knockbackDuration;
            m_stunDuration = stunDuration;
        }

        public void SetDirection(Vector2 direction)
        {
            m_direction = direction.normalized;
            if (m_direction != Vector2.zero)
            {
                transform.rotation = TowerBreakers.Core.Utilities.DirectionHelper.ToRotation(m_direction);
            }
        }

        public override void Activate()
        {
            base.Activate();
            m_startPosition = transform.position;
            m_hitEnemies.Clear();
            if (m_selfCollider != null) m_selfCollider.enabled = true;
            if (m_childColliders != null)
            {
                foreach (var collider in m_childColliders)
                {
                    if (collider != m_selfCollider)
                    {
                        collider.enabled = true;
                    }
                }
            }
        }

        public override void Deactivate()
        {
            base.Deactivate();
            m_hitEnemies.Clear();
            if (m_selfCollider != null) m_selfCollider.enabled = false;
            if (m_childColliders != null)
            {
                foreach (var collider in m_childColliders)
                {
                    if (collider != m_selfCollider)
                    {
                        collider.enabled = false;
                    }
                }
            }
        }
        #endregion

        #region 내부 메서드
        protected override void OnMove()
        {
            float distSq = ((Vector2)transform.position - m_startPosition).sqrMagnitude;
            if (distSq >= m_maxDistanceSq)
            {
                OnLifetimeExpired();
                return;
            }

            transform.Translate(m_direction * (m_speed * Time.deltaTime), Space.World);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!m_isInitialized) return;
            if ((LayerMask.GetMask("Enemy", "Object") & (1 << other.gameObject.layer)) == 0) return;

            GameObject enemyObj = other.gameObject;
            if (m_hitEnemies.Contains(enemyObj)) return;
            
            var damageable = other.GetComponentInParent<IDamageable>();
            if (damageable == null || damageable.IsDead) return;

            m_hitEnemies.Add(enemyObj);
            ApplyDamageAndSlow(other);
        }

        private void ApplyDamageAndSlow(Collider2D target)
        {
            var damageable = target.GetComponentInParent<IDamageable>();
            if (damageable != null && !damageable.IsDead)
            {
                damageable.TakeDamage(m_damage);
            }

            var enemyController = target.GetComponentInParent<EnemyController>();
            if (enemyController != null)
            {
                enemyController.ApplySlow(m_slowMultiplier, m_slowDuration);
                enemyController.ApplyKnockback(m_knockbackDistance, m_knockbackDuration);
                enemyController.ApplyStun(m_stunDuration);
            }

            PlayHitEffect(target.transform.position);
        }

        private void PlayHitEffect(Vector3 position)
        {
            if (m_eventBus != null)
            {
                m_eventBus.Publish(new Core.Events.OnHitEffectRequested(position, effectType: EffectType.Hit));
            }
            else if (m_effectManager != null)
            {
                m_effectManager.PlayEffect(EffectType.Hit, position);
            }
        }
        #endregion
    }
}

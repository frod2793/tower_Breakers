using TowerBreakers.Core.Interfaces;
using TowerBreakers.Effects;
using UnityEngine;

namespace TowerBreakers.Player.View
{
    /// <summary>
    /// [설명]: 플레이어 발사체의 베이스 클래스입니다.
    /// </summary>
    public abstract class PlayerProjectile : MonoBehaviour
    {
        #region 필드
        protected int m_damage;
        protected float m_speed;
        protected float m_lifetime;
        protected float m_elapsedTime;
        protected bool m_isInitialized;
        protected int m_ownerLayer;
        protected EffectManager m_effectManager;
        protected Core.Events.IEventBus m_eventBus;
        #endregion

        #region 프로퍼티
        public bool IsInitialized => m_isInitialized;
        #endregion

        #region 유니티 생명주기
        protected virtual void Awake() { }

        protected virtual void Update()
        {
            if (!m_isInitialized) return;

            m_elapsedTime += Time.deltaTime;
            if (m_elapsedTime >= m_lifetime)
            {
                OnLifetimeExpired();
                return;
            }

            OnMove();
        }
        #endregion

        #region 공개 메서드
        public virtual void Initialize(int damage, float speed, float lifetime, int ownerLayer, EffectManager effectManager = null, Core.Events.IEventBus eventBus = null)
        {
            m_damage = damage;
            m_speed = speed;
            m_lifetime = lifetime;
            m_ownerLayer = ownerLayer;
            m_effectManager = effectManager;
            m_eventBus = eventBus;
            m_elapsedTime = 0f;
            m_isInitialized = true;
            gameObject.layer = ownerLayer;
        }

        public virtual void Activate()
        {
            gameObject.SetActive(true);
            m_elapsedTime = 0f;
        }

        public virtual void Deactivate()
        {
            gameObject.SetActive(false);
            m_isInitialized = false;
        }
        #endregion

        #region 내부 메서드
        protected virtual void OnMove() { }

        protected virtual void OnLifetimeExpired()
        {
            Deactivate();
        }

        protected void ApplyDamage(Collider2D target)
        {
            var damageable = target.GetComponentInParent<IDamageable>();
            if (damageable != null && !damageable.IsDead)
            {
                damageable.TakeDamage(m_damage);
            }
        }
        #endregion
    }
}

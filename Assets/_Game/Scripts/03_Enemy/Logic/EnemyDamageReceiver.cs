using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using DG.Tweening;
using TowerBreakers.Core.Interfaces;
using TowerBreakers.Core.Events;
using TowerBreakers.Enemy.Data;
using TowerBreakers.Enemy.View;

namespace TowerBreakers.Enemy.Logic
{
    /// <summary>
    /// [설명]: 적의 데미지 수신 및 사망 처리를 담당하는 컴포넌트입니다.
    /// [역할 분리]: EnemyController에서 데미지/사망 로직을 분리한 클래스입니다.
    /// </summary>
    public class EnemyDamageReceiver : MonoBehaviour, IDamageable
    {
        #region 내부 필드
        private EnemyView m_view;
        private EnemyData m_data;
        private IEventBus m_eventBus;
        private EnemyPushLogic m_pushLogic;
        private Transform m_cachedTransform;
        private EnemyDeathEffect m_deathEffect;
        private System.Action<EnemyView, string> m_onReclaim;

        private int m_currentHp;
        private int m_enemyId;
        private int m_assignedFloorIndex;
        private bool m_isDead = false;
        private bool m_isInitialized = false;

        private static int s_nextEnemyId = 0;
        #endregion

        #region 프로퍼티
        public bool IsDead => m_isDead;
        public int EnemyId => m_enemyId;
        public int CurrentHp => m_currentHp;
        public int MaxHp => m_data != null ? m_data.Hp : 0;
        public EnemyType Type => m_data != null ? m_data.Type : EnemyType.Normal;
        public int AssignedFloorIndex => m_assignedFloorIndex;
        public EnemyView View => m_view;
        public EnemyData Data => m_data;
        #endregion

        public void Initialize(EnemyData data, EnemyView view, EnemyPushLogic pushLogic, EnemyDeathEffect deathEffect, IEventBus eventBus, int floorIndex, System.Action<EnemyView, string> onReclaim)
        {
            m_data = data;
            m_view = view;
            m_pushLogic = pushLogic;
            m_deathEffect = deathEffect;
            m_eventBus = eventBus;
            m_assignedFloorIndex = floorIndex;
            m_onReclaim = onReclaim;

            if (m_view != null)
            {
                m_cachedTransform = m_view.transform;
            }

            m_currentHp = data.Hp;
            m_isDead = false;
            m_enemyId = s_nextEnemyId++;
            m_isInitialized = true;
        }

        public void TakeDamage(int damage, float knockbackForce = 0f)
        {
            if (m_isDead || !m_isInitialized) return;

            m_currentHp -= damage;

            if (m_view != null)
            {
                m_view.PlayHitEffect();
                m_eventBus?.Publish(new OnDamageTextRequested(m_cachedTransform.position + Vector3.up * 1.5f, damage));

                if (knockbackForce > 0f)
                {
                    GetComponent<EnemyDebuffSystem>()?.ApplyKnockback(knockbackForce, 0.15f, KnockbackType.Punch);
                }
            }

            if (m_currentHp <= 0)
            {
                Die();
            }
        }

        public void Heal(int amount)
        {
            if (m_isDead || !m_isInitialized) return;
            m_currentHp = Mathf.Min(m_currentHp + amount, m_data.Hp);
        }

        public void Die()
        {
            if (m_isDead) return;
            m_isDead = true;

            DieAsync().Forget();
        }

        private async UniTaskVoid DieAsync()
        {
            if (m_view != null)
            {
                m_view.PlayAnimation(global::PlayerState.DEATH, 0);
            }

            if (m_pushLogic != null)
            {
                m_pushLogic.HandleDeath();
            }

            m_eventBus?.Publish(new OnEnemyKilled(m_enemyId, m_assignedFloorIndex, m_data.Type));

            if (m_deathEffect != null && m_view != null)
            {
                // 발 밑(Position)이 아닌 몸 중앙(Vector3.up * 0.5f)에서 폭발하도록 설정하여 위로 솟구치는 힘을 자연스럽게 유도
                m_deathEffect.PlayShatter(m_view.Renderers, m_cachedTransform.position + Vector3.up * 0.5f);
                
                // 원본 캐릭터는 즉시 비활성화 (파편만 남음)
                m_view.gameObject.SetActive(false);
            }

            await UniTask.Delay(1000, cancellationToken: this.GetCancellationTokenOnDestroy());

            m_onReclaim?.Invoke(m_view, m_data.EnemyName);
        }

        public void Reset()
        {
            m_isDead = false;
            m_currentHp = m_data.Hp;
            m_enemyId = s_nextEnemyId++;
        }
    }
}

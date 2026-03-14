using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using DG.Tweening;
using TowerBreakers.Enemy.Data;
using TowerBreakers.Enemy.View;

namespace TowerBreakers.Enemy.Logic
{
    /// <summary>
    /// [설명]: 적의 디버프(넉백, 슬로우, 기절) 처리를 담당하는 컴포넌트입니다.
    /// [역할 분리]: EnemyController에서 디버프 로직을 분리한 클래스입니다.
    /// </summary>
    public class EnemyDebuffSystem : MonoBehaviour
    {
        #region 내부 필드
        private Transform m_cachedTransform;
        private EnemyView m_view;
        private EnemyStateMachine m_stateMachine;
        private EnemyData m_data;
        private bool m_isInitialized = false;
        private bool m_isDead = false;

        private float m_lastKnockbackTime;
        private float m_speedMultiplier = 1.0f;
        private System.Threading.CancellationTokenSource m_slowCts;
        #endregion

        #region 프로퍼티
        public float SpeedMultiplier => m_speedMultiplier;
        #endregion

        public void Initialize(EnemyView view, EnemyStateMachine stateMachine, EnemyData data)
        {
            m_view = view;
            m_stateMachine = stateMachine;
            m_data = data;
            m_cachedTransform = view.transform;
            m_isInitialized = true;
        }

        public void SetDead(bool isDead)
        {
            m_isDead = isDead;
        }

        /// <summary>
        /// [설명]: 적의 넉백을 외부에 적용하기 위한 API입니다.
        /// </summary>
        public void ApplyKnockback(float distance, float duration, KnockbackType type = KnockbackType.Translate)
        {
            if (!m_isInitialized || m_isDead || m_cachedTransform == null) return;

            float currentTime = UnityEngine.Time.time;
            if (currentTime - m_lastKnockbackTime < 0.05f) return;
            m_lastKnockbackTime = currentTime;

            m_cachedTransform.DOKill(true);

            switch (type)
            {
                case KnockbackType.Translate:
                    m_cachedTransform.DOMoveX(m_cachedTransform.position.x + distance, duration)
                        .SetTarget(m_view.gameObject)
                        .SetEase(Ease.OutQuad);
                    break;
                case KnockbackType.Punch:
                    m_cachedTransform.DOPunchPosition(Vector3.right * distance, duration, 2, 0.5f)
                        .SetTarget(m_view.gameObject);
                    break;
            }
        }

        /// <summary>
        /// [설명]: 적에게 슬로우 디버프를 적용합니다.
        /// </summary>
        public void ApplySlow(float multiplier, float duration)
        {
            if (!m_isInitialized || m_isDead) return;

            m_slowCts?.Cancel();
            m_slowCts?.Dispose();
            m_slowCts = System.Threading.CancellationTokenSource.CreateLinkedTokenSource(this.GetCancellationTokenOnDestroy());

            ApplySlowAsync(multiplier, duration, m_slowCts.Token).Forget();
        }

        private async UniTaskVoid ApplySlowAsync(float multiplier, float duration, System.Threading.CancellationToken ct)
        {
            try
            {
                m_speedMultiplier = multiplier;
                await UniTask.Delay(TimeSpan.FromSeconds(duration), cancellationToken: ct);
                m_speedMultiplier = 1.0f;
            }
            catch (OperationCanceledException)
            {
            }
            finally
            {
                if (!ct.IsCancellationRequested)
                {
                    m_speedMultiplier = 1.0f;
                }
            }
        }

        /// <summary>
        /// [설명]: 적에게 기절 상태를 적용합니다.
        /// </summary>
        public void ApplyStun(float duration)
        {
            if (!m_isInitialized || m_isDead || m_stateMachine == null) return;
            var stunnedState = m_stateMachine.GetState<EnemyStunnedState>();
            if (stunnedState != null)
            {
                stunnedState.SetDuration(duration);
            }
            m_stateMachine.ChangeState<EnemyStunnedState>();
        }

        private void OnDestroy()
        {
            m_slowCts?.Cancel();
            m_slowCts?.Dispose();
        }
    }
}

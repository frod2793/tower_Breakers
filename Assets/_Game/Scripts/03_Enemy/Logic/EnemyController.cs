using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using TowerBreakers.Enemy.Data;
using TowerBreakers.Enemy.View;
using TowerBreakers.Core.Events;
using DG.Tweening;

namespace TowerBreakers.Enemy.Logic
{
    /// <summary>
    /// [설명]: 적의 상태 머신을 구동하고 Unity Update 루프와 연결하는 컨트롤러 클래스입니다.
    /// POCO인 EnemyStateMachine을 소유하며, 실제 로직 업데이트와 연출을 담당합니다.
    /// </summary>
    public class EnemyController : MonoBehaviour
    {
        #region 정적 필드
        private static int s_nextEnemyId = 0;
        #endregion

        #region 내부 변수
        private EnemyStateMachine m_stateMachine;
        private EnemyView m_view;
        private EnemyData m_data;
        private EnemyPushLogic m_pushLogic;
        private IEventBus m_eventBus;
        private ProjectileFactory m_projectileFactory;
        
        private Transform m_cachedTransform;
        private Animator m_cachedAnimator;
        
        private int m_assignedFloorIndex;
        private int m_currentHp;
        private int m_enemyId;
        private bool m_isDead = false;
        private bool m_isInitialized = false;

        /// <summary>
        /// [최적화]: 이벤트 구독 일괄 해제를 위한 리스트입니다.
        /// </summary>
        private readonly System.Collections.Generic.List<System.Action> m_unsubscribers = new System.Collections.Generic.List<System.Action>();

        /// <summary>
        /// [설명]: 사망 시 오브젝트 풀 반환을 위한 콜백입니다.
        /// </summary>
        private System.Action<EnemyView, string> m_onReclaim;
        #endregion

        #region 프로퍼티
        public bool IsDead => m_isDead;
        public int EnemyId => m_enemyId;
        #endregion

        #region 초기화 및 바인딩 로직
        /// <summary>
        /// [설명]: 적 캐릭터를 초기화하고 상태 머신을 설정합니다.
        /// </summary>
        public void Initialize(EnemyData data, EnemyView view, EnemyPushLogic pushLogic, Core.Events.IEventBus eventBus, ProjectileFactory projectileFactory = null, System.Action<EnemyView, string> onReclaim = null)
        {
            ClearSubscriptions();

            m_data = data;
            m_view = view;
            m_pushLogic = pushLogic;
            m_eventBus = eventBus;
            m_projectileFactory = projectileFactory;
            m_onReclaim = onReclaim;
            m_assignedFloorIndex = 0;

            if (m_view != null)
            {
                m_cachedTransform = m_view.transform;
                m_cachedAnimator = m_view.GetComponentInChildren<Animator>();
            }

            m_currentHp = data.Hp;
            m_isDead = false;
            m_enemyId = s_nextEnemyId++;

            InitializeStateMachine();
            BindEvents();

            m_isInitialized = true;
        }

        private void InitializeStateMachine()
        {
            m_stateMachine = new EnemyStateMachine();

            // 기초 상태 등록
            switch (m_data.Type)
            {
                case EnemyType.SupportBuffer:
                    m_stateMachine.AddState(new EnemySupportPushState(m_view, m_data, m_pushLogic, m_stateMachine, typeof(EnemyBuffState)));
                    m_stateMachine.AddState(new EnemyBuffState(m_view, m_data, m_stateMachine, m_eventBus, m_assignedFloorIndex));
                    break;
                case EnemyType.SupportShooter:
                    var playerTarget = m_pushLogic?.PlayerReceiver;
                    m_stateMachine.AddState(new EnemySupportPushState(m_view, m_data, m_pushLogic, m_stateMachine, typeof(EnemyShootState)));
                    m_stateMachine.AddState(new EnemyShootState(m_view, m_data, m_stateMachine, m_projectileFactory, playerTarget));
                    break;
                case EnemyType.Boss:
                    m_stateMachine.AddState(new EnemyBossPhaseState(this, m_view, m_data));
                    break;
                default:
                    m_stateMachine.AddState(new EnemyPushState(m_view, m_data, m_pushLogic));
                    break;
            }

            // 공통 상태 등록
            System.Type returnStateType = GetReturnStateType();
            m_stateMachine.AddState(new EnemyStunnedState(m_view, m_stateMachine, returnStateType));
            m_stateMachine.AddState(new EnemyFrozenState(m_view));
            m_stateMachine.AddState(new EnemyWaitingState(m_view));

            // 초기 상태 설정
            if (m_data.Type == EnemyType.SupportBuffer || m_data.Type == EnemyType.SupportShooter)
                m_stateMachine.ChangeState<EnemySupportPushState>();
            else
                m_stateMachine.ChangeState<EnemyPushState>();
        }

        private System.Type GetReturnStateType()
        {
            if (m_data.Type == EnemyType.SupportBuffer || m_data.Type == EnemyType.SupportShooter)
                return typeof(EnemySupportPushState);
            if (m_data.Type == EnemyType.Boss)
                return typeof(EnemyBossPhaseState);
            return typeof(EnemyPushState);
        }

        private void BindEvents()
        {
            if (m_eventBus != null)
            {
                SubscribeEvent<Core.Events.OnDefendActionTriggered>(OnDefendTriggered);
                SubscribeEvent<Core.Events.OnWallCrushOccurred>(OnWallCrushTriggered);
                SubscribeEvent<Core.Events.OnFloorStarted>(OnFloorStarted);
                SubscribeEvent<Core.Events.OnEnemyBuffRequested>(OnEnemyBuffReceived);
                SubscribeEvent<Core.Events.OnPlayerActionStarted>(OnPlayerActionStarted);
            }
        }

        /// <summary>
        /// [설명]: 이벤트를 구독하고 해제 액션을 리스트에 등록합니다.
        /// </summary>
        private void SubscribeEvent<T>(Action<T> handler) where T : struct
        {
            if (m_eventBus == null) return;
            m_eventBus.Subscribe(handler);
            m_unsubscribers.Add(() => m_eventBus.Unsubscribe(handler));
        }

        /// <summary>
        /// [설명]: 등록된 모든 이벤트 구독을 해제합니다.
        /// </summary>
        private void ClearSubscriptions()
        {
            foreach (var unsubscriber in m_unsubscribers)
            {
                unsubscriber?.Invoke();
            }
            m_unsubscribers.Clear();
        }

        /// <summary>
        /// [설명]: 선스폰된 적을 대기 상태로 초기화합니다.
        /// </summary>
        public void InitializeAsWaiting(EnemyData data, EnemyView view, EnemyPushLogic pushLogic, Core.Events.IEventBus eventBus, int floorIndex, ProjectileFactory projectileFactory = null, System.Action<EnemyView, string> onReclaim = null)
        {
            Initialize(data, view, pushLogic, eventBus, projectileFactory, onReclaim);
            m_assignedFloorIndex = floorIndex;
            
            var buffState = m_stateMachine.GetState<EnemyBuffState>();
            if (buffState != null)
            {
                buffState.SetFloorIndex(floorIndex);
            }

            m_stateMachine.ChangeState<EnemyWaitingState>();
        }
        #endregion

        #region 공개 API
        /// <summary>
        /// [설명]: 외부로부터 데미지와 넉백 힘을 받습니다.
        /// </summary>
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
                    m_cachedTransform.DOPunchPosition(Vector3.right * knockbackForce, 0.2f, 10, 1f);
                }
            }

            if (m_currentHp <= 0)
            {
                Die();
            }
        }

        /// <summary>
        /// [설명]: 적의 상태를 강제로 변경합니다.
        /// </summary>
        public void ChangeState<T>() where T : IEnemyState
        {
            m_stateMachine?.ChangeState<T>();
        }

        /// <summary>
        /// [설명]: 적의 체력을 회복합니다.
        /// </summary>
        public void Heal(int amount)
        {
            if (m_isDead || !m_isInitialized) return;
            m_currentHp = Mathf.Min(m_currentHp + amount, m_data.Hp);
        }
        #endregion

        #region 비즈니스 로직
        /// <summary>
        /// [설명]: 사망 처리를 수행합니다.
        /// </summary>
        private void Die()
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

            m_eventBus?.Publish(new Core.Events.OnEnemyKilled(m_enemyId, m_assignedFloorIndex));

            await UniTask.Delay(1000, cancellationToken: this.GetCancellationTokenOnDestroy());

            m_onReclaim?.Invoke(m_view, m_data.EnemyName);
        }

        private void OnWallCrushTriggered(Core.Events.OnWallCrushOccurred evt)
        {
            if (m_stateMachine == null || m_isDead || !m_isInitialized) return;
            if (evt.FloorIndex != m_assignedFloorIndex) return;

            m_stateMachine.ChangeState<EnemyFrozenState>();
        }

        private void OnPlayerActionStarted(Core.Events.OnPlayerActionStarted evt)
        {
            if (m_stateMachine == null || m_isDead || !m_isInitialized) return;

            if (evt.ActionName == "Leap" && m_stateMachine.IsCurrentState<EnemyFrozenState>())
            {
                ResumeMovement();
            }
        }

        private void ResumeMovement()
        {
            if (m_data.Type == EnemyType.SupportBuffer || m_data.Type == EnemyType.SupportShooter)
                m_stateMachine.ChangeState<EnemySupportPushState>();
            else if (m_data.Type == EnemyType.Boss)
                m_stateMachine.ChangeState<EnemyBossPhaseState>();
            else
                m_stateMachine.ChangeState<EnemyPushState>();
        }

        private void OnDefendTriggered(Core.Events.OnDefendActionTriggered evt)
        {
            if (m_stateMachine == null || m_isDead || !m_isInitialized) return;
            if (evt.FloorIndex != m_assignedFloorIndex) return;

            var leader = m_pushLogic != null ? m_pushLogic.GetLeader() : null;
            if (leader == null || !leader.IsTouchingPlayer(evt.DefendRange)) return;

            if (m_cachedTransform != null && evt.PushbackDistance > 0f)
            {
                m_cachedTransform.DOKill();
                m_cachedTransform.DOMoveX(m_cachedTransform.position.x + evt.PushbackDistance, 0.2f)
                    .SetEase(Ease.OutQuad);
                
                if (m_cachedAnimator != null)
                {
                    m_cachedAnimator.Play(0, -1, 0f);
                }
            }

            var stunnedState = m_stateMachine.GetState<EnemyStunnedState>();
            if (stunnedState != null)
            {
                stunnedState.SetDuration(evt.StunDuration);
            }
            m_stateMachine.ChangeState<EnemyStunnedState>();
        }

        private void OnFloorStarted(Core.Events.OnFloorStarted evt)
        {
            if (m_stateMachine == null || m_isDead) return;

            if (evt.FloorIndex == m_assignedFloorIndex)
            {
                if (m_data.Type == EnemyType.SupportBuffer || m_data.Type == EnemyType.SupportShooter)
                    m_stateMachine.ChangeState<EnemySupportPushState>();
                else
                    m_stateMachine.ChangeState<EnemyPushState>();
            }
        }

        private void OnEnemyBuffReceived(OnEnemyBuffRequested evt)
        {
            if (m_isDead || !m_isInitialized) return;
            if (evt.FloorIndex == m_assignedFloorIndex)
            {
                Heal(evt.HealAmount);
            }
        }
        #endregion

        #region 유니티 생명주기
        private void Update()
        {
            if (!m_isInitialized || m_isDead) return;
            m_stateMachine.Tick();
        }

        private void OnDestroy()
        {
            ClearSubscriptions();
        }
        #endregion
    }
}

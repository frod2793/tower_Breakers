using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using TowerBreakers.Enemy.Data;
using TowerBreakers.Enemy.View;
using TowerBreakers.Core.Events;
using DG.Tweening;
using TowerBreakers.Core.Interfaces;
using TowerBreakers.Player.Logic;

using TowerBreakers.Tower.Logic;

namespace TowerBreakers.Enemy.Logic
{
    /// <summary>
    /// [설명]: 적의 상태 머신을 구동하고 Unity Update 루프와 연결하는 컨트롤러 클래스입니다.
    /// </summary>
    public class EnemyController : MonoBehaviour, IDamageable
    {
        #region 에디터 설정
        [SerializeField, Tooltip("성능 최적화: 플레이어가 현재 층에 있을 때만 업데이트 실행")]
        private bool m_updateOnlyOnActiveFloor = true;
        #endregion

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
        private TowerManager m_towerManager;
        
        private Transform m_cachedTransform;
        private Animator m_cachedAnimator;
        
        private int m_assignedFloorIndex;
        private int m_currentHp;
        private int m_enemyId;
        private bool m_isDead = false;
        private bool m_isInitialized = false;
        private float m_lastKnockbackTime; // [최적화]: 연타 시 넉백 트윈 폭증 방지용

        /// <summary>
        /// [최적화]: 사망 시 오브젝트 풀 반환을 위한 콜백입니다.
        /// </summary>
        private System.Action<EnemyView, string> m_onReclaim;
#endregion

        /// <summary>
        /// [설명]: 적의 넉백을 외부에 적용하기 위한 API입니다.
        /// </summary>
        public void ApplyKnockback(float distance, float duration)
        {
            if (m_cachedTransform == null) return;
            m_cachedTransform.DOKill();
            m_cachedTransform.DOMoveX(m_cachedTransform.position.x + distance, duration).SetEase(Ease.OutQuad);
        }

        #region 프로퍼티
        public bool IsDead => m_isDead;
        public int EnemyId => m_enemyId;
        public EnemyType Type => m_data != null ? m_data.Type : EnemyType.Normal;
        #endregion

        #region 초기화 및 바인딩 로직
        /// <summary>
        /// [설명]: 적 캐릭터를 초기화하고 상태 머신을 설정합니다.
        /// </summary>
        public void Initialize(EnemyData data, EnemyView view, EnemyPushLogic pushLogic, Core.Events.IEventBus eventBus, TowerManager towerManager, int floorIndex, ProjectileFactory projectileFactory = null, System.Action<EnemyView, string> onReclaim = null)
        {
            // [최적화]: 기존 구독 명시적 해제
            ClearSubscriptions();

            m_data = data;
            m_view = view;
            m_pushLogic = pushLogic;
            m_eventBus = eventBus;
            m_towerManager = towerManager;
            m_projectileFactory = projectileFactory;
            m_onReclaim = onReclaim;
            m_assignedFloorIndex = floorIndex;

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
            // [최적화]: 상태 머신이 이미 존재하면 새로 생성하지 않음 (힙 할당 방지)
            if (m_stateMachine == null)
            {
                m_stateMachine = new EnemyStateMachine();
            }
            else
            {
                // 기존 상태 클리어 (필요 시)
                m_stateMachine.ClearStates();
            }

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
                m_eventBus.Subscribe<Core.Events.OnDefendActionTriggered>(OnDefendTriggered);
                m_eventBus.Subscribe<Core.Events.OnWallCrushOccurred>(OnWallCrushTriggered);
                m_eventBus.Subscribe<Core.Events.OnFloorStarted>(OnFloorStarted);
                m_eventBus.Subscribe<Core.Events.OnEnemyBuffRequested>(OnEnemyBuffReceived);
                m_eventBus.Subscribe<Core.Events.OnPlayerActionStarted>(OnPlayerActionStarted);
            }
        }

        /// <summary>
        /// [설명]: 등록된 모든 이벤트 구독을 명시적으로 해제합니다. (람다 할당 방지)
        /// </summary>
        private void ClearSubscriptions()
        {
            if (m_eventBus == null) return;

            m_eventBus.Unsubscribe<Core.Events.OnDefendActionTriggered>(OnDefendTriggered);
            m_eventBus.Unsubscribe<Core.Events.OnWallCrushOccurred>(OnWallCrushTriggered);
            m_eventBus.Unsubscribe<Core.Events.OnFloorStarted>(OnFloorStarted);
            m_eventBus.Unsubscribe<Core.Events.OnEnemyBuffRequested>(OnEnemyBuffReceived);
            m_eventBus.Unsubscribe<Core.Events.OnPlayerActionStarted>(OnPlayerActionStarted);
        }

        /// <summary>
        /// [설명]: 선스폰된 적을 대기 상태로 초기화합니다.
        /// </summary>
        public void InitializeAsWaiting(EnemyData data, EnemyView view, EnemyPushLogic pushLogic, Core.Events.IEventBus eventBus, int floorIndex, TowerManager towerManager, ProjectileFactory projectileFactory = null, System.Action<EnemyView, string> onReclaim = null)
        {
            Initialize(data, view, pushLogic, eventBus, towerManager, floorIndex, projectileFactory, onReclaim);
            
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
                
                // [최적화]: 매 프레임/매 타격마다 이벤트 버스 발행 비용을 줄이기 위해 거리/조건부 체크 검토 가능
                // 현재는 즉시 발행하되, 위치 계산에 따른 가비지 생성을 최소화합니다.
                m_eventBus?.Publish(new OnDamageTextRequested(m_cachedTransform.position + Vector3.up * 1.5f, damage));

                if (knockbackForce > 0f)
                {
                    // [최적화]: 연타 시 넉백 트윈 생성/파괴 비용 절감을 위해 쓰로틀링(0.05s) 적용
                    float currentTime = UnityEngine.Time.time;
                    if (currentTime - m_lastKnockbackTime > 0.05f)
                    {
                        m_lastKnockbackTime = currentTime;
                        m_cachedTransform.DOKill(true); // Complete current tween for stability
                        m_cachedTransform.DOPunchPosition(Vector3.right * knockbackForce, 0.15f, 2, 0.5f);
                    }
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

            m_eventBus?.Publish(new Core.Events.OnEnemyKilled(m_enemyId, m_assignedFloorIndex, m_data.Type));

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

            // [최적화]: 문자열 기반 판정 대신 enum 기반 판정으로 변경
            if (evt.ActionType == PlayerActionType.Leap && m_stateMachine.IsCurrentState<EnemyFrozenState>())
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

            if (m_cachedTransform != null && evt.PushbackDistance > 0f)
            {
                // [수정]: 적의 패링 저항(ParryResistance)을 적용하여 밀림 거리 계산
                float resistance = m_data != null ? m_data.ParryResistance : 0f;
                float finalPushback = evt.PushbackDistance * (1f - Mathf.Clamp01(resistance));

                // 즉시 넉백 트윈 실행
                m_cachedTransform.DOKill(true);
                m_cachedTransform.DOMoveX(m_cachedTransform.position.x + finalPushback, 0.25f)
                    .SetEase(Ease.OutCubic);
            }

            var stunnedState = m_stateMachine.GetState<EnemyStunnedState>();
            if (stunnedState != null)
            {
                stunnedState.SetDuration(evt.StunDuration);
            }
            
            // 기절 상태 재진입 (이미 기절 중이더라도 타이머 리셋 및 애니메이션 재생)
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

            // [성능 최적화]: 사용자가 설정한 경우, 현재 플레이어가 있는 층의 적만 업데이트를 수행합니다.
            if (m_updateOnlyOnActiveFloor && m_towerManager != null)
            {
                if (m_towerManager.CurrentFloorIndex != m_assignedFloorIndex)
                {
                    return;
                }
            }

            m_stateMachine.Tick();
        }

        private void OnDestroy()
        {
            ClearSubscriptions();
        }
        #endregion
    }
}

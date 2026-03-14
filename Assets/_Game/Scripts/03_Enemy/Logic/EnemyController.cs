using System;
using UnityEngine;
using TowerBreakers.Enemy.Data;
using TowerBreakers.Enemy.View;
using TowerBreakers.Core.Events;
using TowerBreakers.Core.Interfaces;
using TowerBreakers.Player.Logic;
using TowerBreakers.Enemy.Boss.AI.FSM;
using TowerBreakers.Tower.Logic;

namespace TowerBreakers.Enemy.Logic
{
    /// <summary>
    /// [설명]: 적의 상태 머신을 구동하고 Unity Update 루프와 연결하는 컨트롤러 클래스입니다.
    /// [역할]: 코디네이터로 하위 시스템(DamageReceiver, DebuffSystem, StateMachine)을 조율합니다.
    /// </summary>
    public class EnemyController : MonoBehaviour
    {
        #region 에디터 설정
        [SerializeField, Tooltip("성능 최적화: 플레이어가 현재 층에 있을 때만 업데이트 실행")]
        private bool m_updateOnlyOnActiveFloor = true;
        #endregion

        #region 정적 필드
        private static readonly IEnemyStateFactory s_stateFactory = new EnemyStateFactory();
        #endregion

        #region 내부 변수
        private EnemyStateMachine m_stateMachine;
        private EnemyView m_view;
        private EnemyData m_data;
        private EnemyPushLogic m_pushLogic;
        private EnemyDamageReceiver m_damageReceiver;
        private EnemyDebuffSystem m_debuffSystem;
        private IEventBus m_eventBus;
        private ProjectileFactory m_projectileFactory;
        private TowerManager m_towerManager;
        
        private BossFSM m_bossFSM;
        
        private int m_assignedFloorIndex;
        private bool m_isInitialized = false;
        #endregion

        #region 초기화 및 바인딩 로직
        private void ClearSubscriptions()
        {
            if (m_eventBus == null) return;
            m_eventBus.Unsubscribe<OnPlayerFloorChanged>(HandlePlayerFloorChanged);
            m_eventBus.Unsubscribe<OnEnemyDamaged>(HandleEnemyDamaged);
            m_eventBus.Unsubscribe<OnPlayerAttackLanded>(HandlePlayerAttackLanded);
            m_eventBus.Unsubscribe<Core.Events.OnWallCrushOccurred>(OnWallCrushTriggered);
            m_eventBus.Unsubscribe<Core.Events.OnPlayerActionStarted>(OnPlayerActionStarted);
            m_eventBus.Unsubscribe<Core.Events.OnDefendActionTriggered>(OnDefendTriggered);
            m_eventBus.Unsubscribe<Core.Events.OnFloorStarted>(OnFloorStarted);
            m_eventBus.Unsubscribe<OnEnemyBuffRequested>(OnEnemyBuffReceived);
        }

        private void BindEvents()
        {
            if (m_eventBus == null) return;
            m_eventBus.Subscribe<OnPlayerFloorChanged>(HandlePlayerFloorChanged);
            m_eventBus.Subscribe<OnEnemyDamaged>(HandleEnemyDamaged);
            m_eventBus.Subscribe<OnPlayerAttackLanded>(HandlePlayerAttackLanded);
            m_eventBus.Subscribe<Core.Events.OnWallCrushOccurred>(OnWallCrushTriggered);
            m_eventBus.Subscribe<Core.Events.OnPlayerActionStarted>(OnPlayerActionStarted);
            m_eventBus.Subscribe<Core.Events.OnDefendActionTriggered>(OnDefendTriggered);
            m_eventBus.Subscribe<Core.Events.OnFloorStarted>(OnFloorStarted);
            m_eventBus.Subscribe<OnEnemyBuffRequested>(OnEnemyBuffReceived);
        }

        /// <summary>
        /// [설명]: 적 캐릭터를 초기화하고 상태 머신을 설정합니다.
        /// </summary>
        public void Initialize(EnemyData data, EnemyView view, EnemyPushLogic pushLogic, EnemyDeathEffect deathEffect, Core.Events.IEventBus eventBus, TowerManager towerManager, int floorIndex, ProjectileFactory projectileFactory = null, System.Action<EnemyView, string> onReclaim = null)
        {
            ClearSubscriptions();

            m_data = data;
            m_view = view;
            m_pushLogic = pushLogic;
            m_eventBus = eventBus;
            m_towerManager = towerManager;
            m_projectileFactory = projectileFactory;
            m_assignedFloorIndex = floorIndex;

            m_damageReceiver = m_view.GetComponent<EnemyDamageReceiver>();
            if (m_damageReceiver == null)
            {
                m_damageReceiver = m_view.gameObject.AddComponent<EnemyDamageReceiver>();
            }
            m_damageReceiver.Initialize(data, view, pushLogic, deathEffect, eventBus, floorIndex, onReclaim);

            m_debuffSystem = m_view.GetComponent<EnemyDebuffSystem>();
            if (m_debuffSystem == null)
            {
                m_debuffSystem = m_view.gameObject.AddComponent<EnemyDebuffSystem>();
            }

            m_stateMachine = new EnemyStateMachine();
            InitializeStateMachine();

            m_debuffSystem.Initialize(view, m_stateMachine, data);

            if (data.Type == EnemyType.Boss)
            {
                InitializeBossFSM();
            }

            BindEvents();

            m_isInitialized = true;
        }

        private void InitializeStateMachine()
        {
            if (m_stateMachine == null) return;

            m_stateMachine.ClearStates();

            s_stateFactory.CreateStates(m_stateMachine, m_view, m_data, m_pushLogic, m_eventBus, m_assignedFloorIndex, m_projectileFactory, this);

            System.Type returnStateType = s_stateFactory.GetReturnStateType(m_data.Type);
            m_stateMachine.AddState(new EnemyStunnedState(m_view, m_stateMachine, returnStateType));
            m_stateMachine.AddState(new EnemyFrozenState(m_view));
            m_stateMachine.AddState(new EnemyWaitingState(m_view));

            if (m_data.Type != EnemyType.Boss)
            {
                System.Type initialStateType = s_stateFactory.GetInitialStateType(m_data.Type);
                m_stateMachine.ChangeState(initialStateType);
            }
        }

        /// <summary>
        /// [설명]: BossFSM을 초기화합니다.
        /// </summary>
        private void InitializeBossFSM()
        {
            Debug.Log($"[EnemyController] BossFSM 초기화 시작: {m_data?.EnemyName}, 위치: {transform.position}");

            if (m_view == null)
            {
                Debug.LogError("[EnemyController] m_view가 null입니다! EnemyView를 연결하세요.");
                return;
            }
            if (m_data == null)
            {
                Debug.LogError("[EnemyController] m_data가 null입니다! EnemyData를 연결하세요.");
                return;
            }
            if (m_pushLogic == null)
            {
                Debug.LogError("[EnemyController] m_pushLogic가 null입니다! EnemyPushLogic를 연결하세요.");
                return;
            }

            m_bossFSM = new BossFSM(this, m_view, m_data);
            m_bossFSM.Start();
            Debug.Log($"[EnemyController] BossFSM 초기화 완료: {m_data.EnemyName}");
        }

        /// <summary>
        /// [설명]: 대기 상태로 적을 초기화합니다.
        /// </summary>
        public void InitializeAsWaiting(EnemyData data, EnemyView view, EnemyPushLogic pushLogic, EnemyDeathEffect deathEffect, Core.Events.IEventBus eventBus, int floorIndex, TowerManager towerManager, ProjectileFactory projectileFactory = null, System.Action<EnemyView, string> onReclaim = null)
        {
            Initialize(data, view, pushLogic, deathEffect, eventBus, towerManager, floorIndex, projectileFactory, onReclaim);
            m_stateMachine.ChangeState<EnemyWaitingState>();
        }
        #endregion

        #region 공개 API
        /// <summary>
        /// [설명]: 외부로부터 데미지와 넉백 힘을 받습니다.
        /// </summary>
        public void TakeDamage(int damage, float knockbackForce = 0f)
        {
            m_damageReceiver?.TakeDamage(damage, knockbackForce);
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
            m_damageReceiver?.Heal(amount);
        }

        /// <summary>
        /// [설명]: 적을 사망 처리합니다.
        /// </summary>
        public void Die()
        {
            m_damageReceiver?.Die();
        }

        /// <summary>
        /// [설명]: 적의 넉백을 외부에 적용합니다.
        /// </summary>
        public void ApplyKnockback(float distance, float duration, KnockbackType type = KnockbackType.Translate)
        {
            m_debuffSystem?.ApplyKnockback(distance, duration, type);
        }

        /// <summary>
        /// [설명]: 적에게 슬로우 디버프를 적용합니다.
        /// </summary>
        public void ApplySlow(float multiplier, float duration)
        {
            m_debuffSystem?.ApplySlow(multiplier, duration);
        }

        /// <summary>
        /// [설명]: 적에게 기절 상태를 적용합니다.
        /// </summary>
        public void ApplyStun(float duration)
        {
            m_debuffSystem?.ApplyStun(duration);
        }
        #endregion

        #region 프로퍼티
        public bool IsDead => m_damageReceiver?.IsDead ?? false;
        public int EnemyId => m_damageReceiver?.EnemyId ?? 0;
        public int CurrentHp => m_damageReceiver?.CurrentHp ?? 0;
        public int MaxHp => m_damageReceiver?.MaxHp ?? 0;
        public EnemyType Type => m_data?.Type ?? EnemyType.Normal;
        public float SpeedMultiplier => m_debuffSystem?.SpeedMultiplier ?? 1.0f;
        public int AssignedFloorIndex => m_assignedFloorIndex;
        public EnemyStateMachine StateMachine => m_stateMachine;
        
        public object BossPhaseState => m_bossFSM;

        public EnemyView CachedView => m_view;

        public EnemyPushLogic CachedPushLogic => m_pushLogic;

        public ProjectileFactory ProjectileFactory => m_projectileFactory;

        public IEventBus EventBus => m_eventBus;

        public EnemyData Data => m_data;
        #endregion

        #region 이벤트 핸들러
        private void OnWallCrushTriggered(Core.Events.OnWallCrushOccurred evt)
        {
            if (m_stateMachine == null || IsDead || !m_isInitialized) return;
            if (evt.FloorIndex != m_assignedFloorIndex) return;

            m_stateMachine.ChangeState<EnemyFrozenState>();
        }

        private void OnPlayerActionStarted(Core.Events.OnPlayerActionStarted evt)
        {
            if (m_stateMachine == null || IsDead || !m_isInitialized) return;

            if (evt.ActionType == PlayerActionType.Leap && m_stateMachine.IsCurrentState<EnemyFrozenState>())
            {
                ResumeMovement();
            }
        }

        private void ResumeMovement()
        {
            if (m_data?.Type == EnemyType.SupportBuffer || m_data?.Type == EnemyType.SupportShooter)
                m_stateMachine.ChangeState<EnemySupportPushState>();
            else if (m_data?.Type == EnemyType.Boss)
                m_bossFSM?.ChangeState(BossStateType.Idle);
            else
                m_stateMachine.ChangeState<EnemyPushState>();
        }

        private void OnDefendTriggered(Core.Events.OnDefendActionTriggered evt)
        {
            if (m_stateMachine == null || IsDead || !m_isInitialized) return;
            if (evt.FloorIndex != m_assignedFloorIndex) return;

            if (m_view != null && evt.PushbackDistance > 0f)
            {
                float resistance = m_data != null ? m_data.ParryResistance : 0f;
                float finalPushback = evt.PushbackDistance * (1f - Mathf.Clamp01(resistance));
                ApplyKnockback(finalPushback, 0.25f);
            }

            if (m_data?.Type == EnemyType.Boss && m_bossFSM != null)
            {
                m_bossFSM.Stun(evt.StunDuration);
                return;
            }

            ApplyStun(evt.StunDuration);
        }

        private void OnFloorStarted(Core.Events.OnFloorStarted evt)
        {
            if (m_stateMachine == null || IsDead) return;

            if (evt.FloorIndex == m_assignedFloorIndex)
            {
                if (m_data?.Type == EnemyType.SupportBuffer || m_data?.Type == EnemyType.SupportShooter)
                {
                    m_stateMachine.ChangeState<EnemySupportPushState>();
                }
                else if (m_data?.Type == EnemyType.Boss)
                {
                    m_stateMachine.ChangeState<EnemyWaitingState>();
                    m_bossFSM?.Resume();
                }
                else
                {
                    m_stateMachine.ChangeState<EnemyPushState>();
                }
            }
        }

        private void OnEnemyBuffReceived(OnEnemyBuffRequested evt)
        {
            if (IsDead || !m_isInitialized) return;
            if (evt.FloorIndex == m_assignedFloorIndex)
            {
                Heal(evt.HealAmount);
            }
        }

        private void HandlePlayerFloorChanged(OnPlayerFloorChanged evt)
        {
            if (m_stateMachine == null || IsDead || !m_isInitialized) return;

            if (evt.NewFloorIndex == m_assignedFloorIndex)
            {
                if (m_data?.Type == EnemyType.SupportBuffer || m_data?.Type == EnemyType.SupportShooter)
                    m_stateMachine.ChangeState<EnemySupportPushState>();
                else if (m_data?.Type == EnemyType.Boss)
                    m_bossFSM?.ChangeState(BossStateType.Idle);
                else
                    m_stateMachine.ChangeState<EnemyPushState>();
            }
        }

        private void HandleEnemyDamaged(OnEnemyDamaged evt)
        {
            if (m_damageReceiver?.EnemyId != evt.EnemyId || IsDead || !m_isInitialized) return;

            if (m_data?.Type == EnemyType.Boss && m_bossFSM != null)
            {
                m_bossFSM.CheckPhaseTransition();
            }
        }

        private void HandlePlayerAttackLanded(OnPlayerAttackLanded evt)
        {
            if (m_damageReceiver?.EnemyId != evt.TargetEnemyId || IsDead || !m_isInitialized) return;

            if (evt.PushbackDistance > 0f)
            {
                ApplyKnockback(evt.PushbackDistance, 0.2f);
            }

            if (m_data?.Type == EnemyType.Boss && m_bossFSM != null)
            {
                m_bossFSM.Stun(evt.StunDuration);
                return;
            }

            ApplyStun(evt.StunDuration);
        }
        #endregion

        #region 유니티 생명주기
        private void Update()
        {
            if (!m_isInitialized || IsDead) return;

            if (m_updateOnlyOnActiveFloor && m_towerManager != null)
            {
                if (m_towerManager.CurrentFloorIndex != m_assignedFloorIndex)
                {
                    return;
                }
            }

            m_stateMachine?.Tick();
        }

        private void OnDestroy()
        {
            ClearSubscriptions();
            m_bossFSM?.Stop();
        }
        #endregion
    }
}

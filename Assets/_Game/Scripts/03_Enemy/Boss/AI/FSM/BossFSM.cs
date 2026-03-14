using UnityEngine;
using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Threading;
using TowerBreakers.Core.Events;
using TowerBreakers.Enemy.Logic;
using TowerBreakers.Enemy.Boss.AI.BT;
using TowerBreakers.Enemy.Data;
using TowerBreakers.Enemy.View;

namespace TowerBreakers.Enemy.Boss.AI.FSM
{
    /// <summary>
    /// [설명]: 보스 FSM (Finite State Machine) 관리자입니다.
    /// FSM + BT를 통합하여 상태 전환과 패턴 선택을 담당합니다.
    /// </summary>
    public class BossFSM : IDisposable
    {
        #region 내부 필드
        private readonly EnemyController m_controller;
        private readonly Dictionary<BossStateType, IBossState> m_states = new();
        
        private IBossState m_currentState;
        private BossStateType m_currentStateType;
        
        private readonly BossSkillContext m_skillContext;
        private BossSkillSelector m_skillSelector;
        
        private bool m_isExecuting = false;
        private CancellationTokenSource m_executionCts;
        private bool m_isDisposed = false;

        /// <summary>
        /// [설명]: 디버그용으로 강제 실행할 패턴입니다.
        /// </summary>
        private IBossPattern m_debugPatternOverride;

        // [추가] 등장 연출 완료 시점까지 패턴 실행 보류 플래그
        private bool m_isPaused = true; 

        /// <summary>
        /// [설명]: 크라켄 보스 전용 상태 데이터입니다.
        /// </summary>
        private KrakenBossState m_krakenState;

        private static readonly IBossPhaseFactory s_phaseFactory = new BossPhaseFactory();
        #endregion

        #region 프로퍼티
        public EnemyController Controller => m_controller;
        public BossSkillContext SkillContext => m_skillContext;
        public int CurrentPhaseIndex => m_skillContext.CurrentPhaseIndex;
        public BossStateType CurrentStateType => m_currentStateType;
        public bool IsExecuting => m_isExecuting;
        public string CurrentStateName => m_currentStateType.ToString();
        public int TotalPhases => m_skillContext.TotalPhases;
        public List<IBossPhase> Phases => m_skillContext.Phases;
        public ProjectileFactory ProjectileFactory => m_controller?.ProjectileFactory;
        public IEventBus EventBus => m_controller?.EventBus;
        #endregion

        #region 초기화
        public BossFSM(EnemyController controller, EnemyView view, EnemyData data)
        {
            m_controller = controller;
            InitializeStates();

            var phases = CreatePhases(data);
            m_skillContext = new BossSkillContext(controller, phases);
            m_skillSelector = new BossSkillSelector(m_skillContext);

            if (m_controller.EventBus != null)
            {
                m_controller.EventBus.Subscribe<OnBossIntroEnded>(OnBossIntroEndedHandler);
            }
        }

        private void OnBossIntroEndedHandler(OnBossIntroEnded evt)
        {
            Resume();
        }

        public void Dispose()
        {
            if (m_isDisposed) return;
            m_isDisposed = true;

            if (m_controller?.EventBus != null)
            {
                m_controller.EventBus.Unsubscribe<OnBossIntroEnded>(OnBossIntroEndedHandler);
            }

            Stop();
        }

        private void InitializeStates()
        {
            m_states[BossStateType.Idle] = new BossIdleState(this);
            m_states[BossStateType.Attack] = new BossAttackState(this);
            m_states[BossStateType.PhaseChange] = new BossPhaseChangeState(this);
            m_states[BossStateType.Stunned] = new BossStunnedState(this);
            m_states[BossStateType.Dead] = new BossDeadState(this);
        }

        private List<IBossPhase> CreatePhases(EnemyData data)
        {
            var phases = s_phaseFactory.CreatePhases(data, this);
            return phases;
        }
        #endregion

        #region 상태 관리
        public void Start()
        {
            ChangeState(BossStateType.Idle);
            TickAsync().Forget();
        }

        public void Resume()
        {
            if (!m_isPaused) return;
            m_isPaused = false;
            UnityEngine.Debug.Log("[BossFSM] 활성화 (등장연출 완료 또는 디버그 강제 시작)");
        }

        public void SetPaused(bool paused)
        {
            m_isPaused = paused;
        }

        public void Stop()
        {
            m_executionCts?.Cancel();
            m_executionCts?.Dispose();
            m_executionCts = null;
        }

        public void ChangeState(BossStateType newStateType)
        {
            if (m_currentStateType == newStateType) return;
            if (m_currentState != null && !m_currentState.CanTransitionTo(newStateType))
            {
                return;
            }

            m_currentState?.OnExit();
            m_currentStateType = newStateType;
            m_currentState = m_states[newStateType];
            m_currentState.OnEnter();

            UnityEngine.Debug.Log($"[BossFSM] 상태 전환: {m_currentStateType}");
        }

        public void Stun(float duration = 2f)
        {
            m_isPaused = true;
            var stunState = m_states[BossStateType.Stunned] as BossStunnedState;
            if (stunState != null)
            {
                stunState.SetDuration(duration);
            }
            ChangeState(BossStateType.Stunned);
        }

        public void Die()
        {
            ChangeState(BossStateType.Dead);
        }

        public void TakeDamage(int damage, float knockbackForce = 0f)
        {
            m_controller?.TakeDamage(damage, knockbackForce);
        }

        /// <summary>
        /// [설명]: 페이즈 전환 조건을 확인하고 필요 시 상태를 변경합니다.
        /// </summary>
        public void CheckPhaseTransition()
        {
            if (IsPhaseChanged() && m_currentStateType != BossStateType.PhaseChange && m_currentStateType != BossStateType.Dead)
            {
                ChangeState(BossStateType.PhaseChange);
            }
        }
        #endregion

        #region 패턴 선택 (BT 사용)
        public bool CanSelectPattern()
        {
            return !m_isExecuting;
        }

        public IBossPattern SelectPatternViaBT()
        {
            return m_skillSelector.Evaluate();
        }

        public IBossPattern GetCurrentPattern()
        {
            return m_skillContext.GetCurrentPattern();
        }

        public void AdvancePatternIndex()
        {
            m_skillContext.AdvancePatternIndex();
        }

        public bool TryChangePhase()
        {
            return m_skillContext.TryChangePhase();
        }

        public void ForceChangePhase(int phaseIndex)
        {
            m_skillContext.ForceChangePhase(phaseIndex);
        }

        public void ExecutePatternImmediate(int patternIndex)
        {
            m_skillContext.ExecutePatternImmediate(patternIndex);
        }

        /// <summary>
        /// [설명]: 인스펙터 버튼 등을 통해 특정 패턴을 즉시 실행합니다.
        /// </summary>
        /// <param name="pattern">실행할 패턴 인스턴스</param>
        public void TriggerDebugPattern(IBossPattern pattern)
        {
            if (pattern == null) return;

            m_isExecuting = false;
            m_debugPatternOverride = pattern;
            Resume();
            ChangeState(BossStateType.Attack);
        }

        /// <summary>
        /// [설명]: 예약된 디버그 패턴을 가져오고 소모합니다.
        /// </summary>
        public IBossPattern ConsumeDebugPattern()
        {
            var pattern = m_debugPatternOverride;
            m_debugPatternOverride = null;
            return pattern;
        }

        public void ExecutePatternDebug(int patternIndex)
        {
            var pattern = m_skillContext.GetPatternByIndex(patternIndex);
            if (pattern != null)
            {
                Debug.Log($"[BossFSM] 디버그 패턴 실행: {pattern.PatternName}");
                pattern.ExecuteAsync(m_controller, m_controller.CachedView.GetCancellationTokenOnDestroy()).Forget();
            }
        }

        public bool IsPhaseChanged()
        {
            return m_skillContext.ShouldChangePhase();
        }
        #endregion

        #region Tick 루프
        private async UniTaskVoid TickAsync()
        {
            m_executionCts = new CancellationTokenSource();
            var ct = m_executionCts.Token;

            while (!ct.IsCancellationRequested)
            {
                if (!m_isPaused && m_currentState != null && !m_isExecuting)
                {
                    m_currentState.OnTick();

                    if (IsPhaseChanged() && m_currentStateType != BossStateType.PhaseChange)
                    {
                        ChangeState(BossStateType.PhaseChange);
                        m_skillSelector.RebuildTree();
                    }

                    if (m_currentStateType == BossStateType.Attack && !m_isExecuting)
                    {
                        m_isExecuting = true;
                        await m_currentState.OnExecuteAsync(ct);

                        int delayMs = (int)(m_controller.Data.PatternDelay * 1000);
                        await UniTask.Delay(delayMs, cancellationToken: ct);

                        m_isExecuting = false;
                    }
                }

                await UniTask.Yield(ct);
            }
        }
        #endregion
    }
}

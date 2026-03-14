using Cysharp.Threading.Tasks;
using System.Threading;

namespace TowerBreakers.Enemy.Boss.AI.FSM
{
    /// <summary>
    /// [설명]: 보스 FSM 상태 타입 열거형입니다.
    /// </summary>
    public enum BossStateType
    {
        Idle,
        Attack,
        PhaseChange,
        Stunned,
        Dead
    }

    /// <summary>
    /// [설명]: 보스 FSM 상태의 기본 인터페이스입니다.
    /// </summary>
    public interface IBossState
    {
        BossStateType StateType { get; }
        bool CanTransitionTo(BossStateType newState);
        void OnEnter();
        void OnExit();
        void OnTick();
        UniTask OnExecuteAsync(CancellationToken ct);
    }

    /// <summary>
    /// [설명]: 보스 FSM 상태의 기본 구현 클래스입니다.
    /// </summary>
    public abstract class BossStateBase : IBossState
    {
        protected readonly BossFSM m_fsm;

        public abstract BossStateType StateType { get; }
        public virtual bool CanTransitionTo(BossStateType newState) => true;

        protected BossStateBase(BossFSM fsm)
        {
            m_fsm = fsm;
        }

        public virtual void OnEnter() { }
        public virtual void OnExit() { }
        public virtual void OnTick() { }
        public virtual UniTask OnExecuteAsync(CancellationToken ct) => UniTask.CompletedTask;
    }

    /// <summary>
    /// [설명]: 유휴 상태입니다. 패턴 실행 대기로 전환합니다.
    /// </summary>
    public class BossIdleState : BossStateBase
    {
        public override BossStateType StateType => BossStateType.Idle;

        public BossIdleState(BossFSM fsm) : base(fsm) { }

        public override void OnEnter()
        {
            var view = m_fsm.Controller.CachedView;
            if (view != null) view.PlayAnimation(global::PlayerState.IDLE);
        }

        public override void OnTick()
        {
            if (m_fsm.CanSelectPattern())
            {
                m_fsm.ChangeState(BossStateType.Attack);
            }
        }
    }

    /// <summary>
    /// [설명]: 공격 상태입니다. BT를 통해 패턴을 선택하고 실행합니다.
    /// </summary>
    public class BossAttackState : BossStateBase
    {
        public override BossStateType StateType => BossStateType.Attack;

        public BossAttackState(BossFSM fsm) : base(fsm) { }

        public override async UniTask OnExecuteAsync(CancellationToken ct)
        {
            // 디버그 예약 패턴이 있다면 최우선 실행, 없다면 BT로 선택
            var selectedPattern = m_fsm.ConsumeDebugPattern() ?? m_fsm.SelectPatternViaBT();

            if (selectedPattern != null)
            {
                var view = m_fsm.Controller.CachedView;
                // [참고]: 일반 Animator를 사용하는 보스도 EnemyView.PlayAnimation을 통해 "ATTACK" 트리거가 동작합니다.
                if (view != null) view.PlayAnimation(global::PlayerState.ATTACK, 0);
                
                UnityEngine.Debug.Log($"[BossAttackState] 패턴 실행 시작: {selectedPattern.PatternName}");
                await selectedPattern.ExecuteAsync(m_fsm.Controller, ct);
            }

            m_fsm.ChangeState(BossStateType.Idle);
        }
    }

    /// <summary>
    /// [설명]: 페이즈 전환 상태입니다. HP 기반 페이즈 변경을 처리합니다.
    /// </summary>
    public class BossPhaseChangeState : BossStateBase
    {
        public override BossStateType StateType => BossStateType.PhaseChange;

        public BossPhaseChangeState(BossFSM fsm) : base(fsm) { }

        public override bool CanTransitionTo(BossStateType newState)
        {
            return newState == BossStateType.Idle || newState == BossStateType.Dead;
        }

        public override void OnEnter()
        {
            int oldPhase = m_fsm.CurrentPhaseIndex;
            m_fsm.TryChangePhase();
            int newPhase = m_fsm.CurrentPhaseIndex;

            if (oldPhase != newPhase)
            {
                UnityEngine.Debug.Log($"[BossFSM] 페이즈 전환: {oldPhase + 1} → {newPhase + 1}");
            }
        }

        public override void OnTick()
        {
            m_fsm.ChangeState(BossStateType.Idle);
        }
    }

    /// <summary>
    /// [설명]: 기절 상태입니다. 공격을 중단하고 대기합니다.
    /// </summary>
    public class BossStunnedState : BossStateBase
    {
        private float m_stunDuration;
        private float m_timer;

        public override BossStateType StateType => BossStateType.Stunned;

        public BossStunnedState(BossFSM fsm) : base(fsm) { }

        public void SetDuration(float duration)
        {
            m_stunDuration = duration;
        }

        public override bool CanTransitionTo(BossStateType newState)
        {
            return newState == BossStateType.Idle || newState == BossStateType.Dead;
        }

        public override void OnEnter()
        {
            m_fsm.SetPaused(true);
            m_timer = 0f;
            if (m_stunDuration <= 0f)
            {
                m_stunDuration = 2f;
            }
            var view = m_fsm.Controller.CachedView;
            if (view != null) view.PlayAnimation(global::PlayerState.DAMAGED);
        }

        public override void OnTick()
        {
            m_timer += UnityEngine.Time.deltaTime;
            if (m_timer >= m_stunDuration)
            {
                m_fsm.SetPaused(false);
                m_fsm.ChangeState(BossStateType.Idle);
            }
        }
    }

    /// <summary>
    /// [설명]: 사망 상태입니다. 보스 사망 처리를 수행합니다.
    /// </summary>
    public class BossDeadState : BossStateBase
    {
        public override BossStateType StateType => BossStateType.Dead;

        public override bool CanTransitionTo(BossStateType newState) => false;

        public BossDeadState(BossFSM fsm) : base(fsm) { }

        public override void OnEnter()
        {
            UnityEngine.Debug.Log("[BossFSM] 보스 사망");
            var view = m_fsm.Controller.CachedView;
            if (view != null) view.PlayAnimation(global::PlayerState.DEATH);
            m_fsm.Controller.Die();
        }
    }
}

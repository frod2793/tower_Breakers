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
    /// POCO인 EnemyStateMachine을 소유하며, 실제 로직 업데이트를 담당합니다.
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

        #region 초기화
        /// <summary>
        /// [설명]: 적 캐릭터를 초기화하고 상태 머신을 설정합니다.
        /// </summary>
        public void Initialize(EnemyData data, EnemyView view, EnemyPushLogic pushLogic, Core.Events.IEventBus eventBus, ProjectileFactory projectileFactory = null, System.Action<EnemyView, string> onReclaim = null)
        {
            // 풀 재사용 또는 재초기화 시 이전 구독 일괄 해제 (Part 4-2)
            ClearSubscriptions();

            m_data = data;
            m_view = view;
            m_pushLogic = pushLogic;
            m_eventBus = eventBus;
            m_projectileFactory = projectileFactory;
            m_onReclaim = onReclaim;
            m_assignedFloorIndex = 0; // 기본값

            m_currentHp = data.Hp;
            m_isDead = false;
            m_enemyId = s_nextEnemyId++;

            m_stateMachine = new EnemyStateMachine();

            // 상태 등록 (타입별 분기)
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
                    // 기본 패턴 등록 예시 (추후 데이터에서 주입 가능)
                    break;
                default:
                    m_stateMachine.AddState(new EnemyPushState(m_view, m_data, m_pushLogic));
                    break;
            }

            // 기절 상태 등록 (복귀 상태 타입 주입)
            System.Type returnStateType = typeof(EnemyPushState);
            if (m_data.Type == EnemyType.SupportBuffer || m_data.Type == EnemyType.SupportShooter)
            {
                returnStateType = typeof(EnemySupportPushState);
            }
            else if (m_data.Type == EnemyType.Boss)
            {
                returnStateType = typeof(EnemyBossPhaseState);
            }

            m_stateMachine.AddState(new EnemyStunnedState(m_view, m_stateMachine, returnStateType));
            m_stateMachine.AddState(new EnemyFrozenState(m_view));
            m_stateMachine.AddState(new EnemyWaitingState(m_view));

            // 초기 상태 설정
            if (m_data.Type == EnemyType.SupportBuffer || m_data.Type == EnemyType.SupportShooter)
                m_stateMachine.ChangeState<EnemySupportPushState>();
            else
                m_stateMachine.ChangeState<EnemyPushState>();

            // 이벤트 구독 (군집 전체 통제) 및 해제 목록 등록
            if (m_eventBus != null)
            {
                SubscribeEvent<Core.Events.OnDefendActionTriggered>(OnDefendTriggered);
                SubscribeEvent<Core.Events.OnWallCrushOccurred>(OnWallCrushTriggered);
                SubscribeEvent<Core.Events.OnFloorStarted>(OnFloorStarted);
                SubscribeEvent<Core.Events.OnEnemyBuffRequested>(OnEnemyBuffReceived);
                SubscribeEvent<Core.Events.OnPlayerActionStarted>(OnPlayerActionStarted);
            }

            m_isInitialized = true;
        }

        /// <summary>
        /// [설명]: 선스폰된 적을 대기 상태로 초기화합니다.
        /// </summary>
        public void InitializeAsWaiting(EnemyData data, EnemyView view, EnemyPushLogic pushLogic, Core.Events.IEventBus eventBus, int floorIndex, ProjectileFactory projectileFactory = null, System.Action<EnemyView, string> onReclaim = null)
        {
            Initialize(data, view, pushLogic, eventBus, projectileFactory, onReclaim);
            m_assignedFloorIndex = floorIndex;
            
            // 서포터 버프 상태의 층 정보 갱신 (에러 5 수정)
            var buffState = m_stateMachine.GetState<EnemyBuffState>();
            if (buffState != null)
            {
                buffState.SetFloorIndex(floorIndex);
            }

            // 대기 상태로 강제 전환
            m_stateMachine.ChangeState<EnemyWaitingState>();
        }
        #endregion

        #region 공개 API
        /// <summary>
        /// [설명]: 외부로부터 데미지와 넉백 힘을 받습니다.
        /// </summary>
        /// <param name="damage">입힐 데미지 수치</param>
        /// <param name="knockbackForce">피격 시 밀려나는 힘 (선택사항)</param>
        public void TakeDamage(int damage, float knockbackForce = 0f)
        {
            if (m_isDead || !m_isInitialized) return;

            m_currentHp -= damage;

            // 피격 시각 효과 오출 (빨간색 깜빡임)
            if (m_view != null)
            {
                m_view.PlayHitEffect();
                
                // [신규]: 적 위치에 데미지 텍스트 요청
                // Debug.Log($"[EnemyController] {gameObject.name} 데미지 텍스트 요청 발행: DMG={damage}");
                m_eventBus?.Publish(new OnDamageTextRequested(m_view.transform.position + Vector3.up * 1.5f, damage));

                // 넉백 처리 (뒤로 튕겼다가 제자리로 돌아오는 느낌의 연출)
                if (knockbackForce > 0f)
                {
                    m_view.transform.DOPunchPosition(Vector3.right * knockbackForce, 0.2f, 10, 1f);
                }
            }

            if (m_currentHp <= 0)
            {
                Die();
            }
        }

        /// <summary>
        /// [설명]: 적의 상태를 강제로 변경합니다. (예: 피격 시 기절 등)
        /// </summary>
        public void ChangeState<T>() where T : IEnemyState
        {
            m_stateMachine?.ChangeState<T>();
        }

        /// <summary>
        /// [설명]: 적의 체력을 회복합니다. 최대 HP를 초과하지 않도록 클램프합니다.
        /// </summary>
        public void Heal(int amount)
        {
            if (m_isDead || !m_isInitialized) return;

            m_currentHp = Mathf.Min(m_currentHp + amount, m_data.Hp);
            
            // 회복 연출 (추후 이펙트 추가 가능)
            Debug.Log($"[EnemyController] {gameObject.name} 회복: +{amount} (현재 HP: {m_currentHp}/{m_data.Hp})");
        }
        #endregion

        #region 비즈니스 로직
        /// <summary>
        /// [설명]: 사망 처리를 수행합니다. 대열을 재정비하고 애니메이션 재생 후 오브젝트를 반환합니다.
        /// </summary>
        private void Die()
        {
            if (m_isDead) return;
            m_isDead = true;

            DieAsync().Forget();
        }

        private async UniTaskVoid DieAsync()
        {
            Debug.Log($"[{gameObject.name}] 사망: 연출 시작 및 대열 재정비");

            // 1. 사망 애니메이션 재생
            if (m_view != null)
            {
                m_view.PlayAnimation(global::PlayerState.DEATH, 0);
            }

            // 2. 기차 대열 재연결 (중요: 뒷사람과 앞사람을 이어줌)
            if (m_pushLogic != null)
            {
                m_pushLogic.HandleDeath();
            }

            // 3. 처치 이벤트 발행
            m_eventBus?.Publish(new Core.Events.OnEnemyKilled(m_enemyId, m_assignedFloorIndex));

            // 4. 사망 연출 대기 (1초) - SPUM 사망 애니메이션 시간을 고려
            await UniTask.Delay(1000, cancellationToken: this.GetCancellationTokenOnDestroy());

            // 5. 오브젝트 풀로 반환
            m_onReclaim?.Invoke(m_view, m_data.EnemyName);
        }

        /// <summary>
        /// [설명]: 벽 압착 발생 시 호출되어 동결 상태로 전환합니다.
        /// </summary>
        private void OnWallCrushTriggered(Core.Events.OnWallCrushOccurred evt)
        {
            if (m_stateMachine == null || m_isDead || !m_isInitialized) return;

            // [층 필터링]: 자신의 층이 아닐 경우 무시
            if (evt.FloorIndex != m_assignedFloorIndex) return;

            // 이미 동결 상태일 수 있지만 명시적으로 전환하여 애니메이션 리셋 등 수행
            m_stateMachine.ChangeState<EnemyFrozenState>();
        }

        /// <summary>
        /// [설명]: 플레이어의 액션 시작을 감지하여 동결 상태를 해제합니다.
        /// </summary>
        private void OnPlayerActionStarted(Core.Events.OnPlayerActionStarted evt)
        {
            if (m_stateMachine == null || m_isDead || !m_isInitialized) return;

            // 플레이어가 '도약'을 수행하면 동결 상태를 해제하고 다시 진격하기 시작합니다.
            // '방어'는 OnDefendActionTriggered 이벤트에 의해 별도로 처리(스턴)됩니다.
            if (evt.ActionName == "Leap" && m_stateMachine.IsCurrentState<EnemyFrozenState>())
            {
                Debug.Log($"[EnemyController] {gameObject.name} 동결 해제 (도약 감지)");
                ResumeMovement();
            }
        }

        /// <summary>
        /// [설명]: 적의 원래 이동/공격 패턴 상태로 복귀합니다.
        /// </summary>
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

            // [층 필터링]: 자신의 층이 아닐 경우 무시
            if (evt.FloorIndex != m_assignedFloorIndex) return;

            // [군집별 필터링]: 플레이어와 접촉 중이거나 매우 근접한 군집(리더 기준)만 반응
            // 리더가 플레이어 영향권(evt.DefendRange) 내에 있을 때만 해당 그룹 전체가 방어의 영향을 받음
            var leader = m_pushLogic != null ? m_pushLogic.GetLeader() : null;
            if (leader == null || !leader.IsTouchingPlayer(evt.DefendRange))
            {
                return;
            }

            Debug.Log($"[EnemyController] {gameObject.name} 방어 이벤트 수신: PushDistance={evt.PushbackDistance}");

            // 1. 밀어내기: 적을 오른쪽으로 PushbackDistance만큼 부드럽게 이동 (Knockback 연출)
            if (m_view != null && evt.PushbackDistance > 0f)
            {
                // 이전 트윈 중단
                m_view.transform.DOKill();

                // DOMoveX를 사용하여 0.2초 동안 부드럽게 밀려남 (타격감 중심 OutQuad)
                m_view.transform.DOMoveX(m_view.transform.position.x + evt.PushbackDistance, 0.2f)
                    .SetEase(Ease.OutQuad);
                
                // 애니메이션 싱크 맞춤 (피격 느낌을 위해 리셋)
                var animator = m_view.GetComponentInChildren<Animator>();
                if (animator != null)
                {
                    animator.Play(0, -1, 0f);
                }
            }

            // 2. 기절 상태로 전환 (이동 로직 중단 및 일정 시간 대기)
            var stunnedState = m_stateMachine.GetState<EnemyStunnedState>();
            if (stunnedState != null)
            {
                stunnedState.SetDuration(evt.StunDuration);
            }
            m_stateMachine.ChangeState<EnemyStunnedState>();
        }

        /// <summary>
        /// [설명]: 층이 시작될 때 본인의 소속 층이면 진격을 시작합니다.
        /// </summary>
        private void OnFloorStarted(Core.Events.OnFloorStarted evt)
        {
            if (m_stateMachine == null || m_isDead) return;

            // 대기 중인 상태에서 자신의 층이 시작되면 진격 상태로 전환
            if (evt.FloorIndex == m_assignedFloorIndex)
            {
                Debug.Log($"[EnemyController] {gameObject.name} (Floor {m_assignedFloorIndex}) 진격 개시!");
                if (m_data.Type == EnemyType.SupportBuffer || m_data.Type == EnemyType.SupportShooter)
                    m_stateMachine.ChangeState<EnemySupportPushState>();
                else
                    m_stateMachine.ChangeState<EnemyPushState>();
            }
        }

        /// <summary>
        /// [설명]: 서포터 유닛의 버프 이벤트를 수신하여 같은 층의 아군을 치유합니다.
        /// </summary>
        private void OnEnemyBuffReceived(OnEnemyBuffRequested evt)
        {
            if (m_isDead || !m_isInitialized) return;

            // 같은 층의 아군만 회복 (본인 포함)
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

            // 상태 머신 업데이트 루프 실행
            m_stateMachine.Tick();
        }

        private void OnDestroy()
        {
            // 메모리 누수 방지: 모든 이벤트 구독 일괄 해제
            ClearSubscriptions();
        }

        /// <summary>
        /// [설명]: 이벤트를 구독하고 해제 액션을 리스트에 등록합니다.
        /// </summary>
        private void SubscribeEvent<T>(System.Action<T> handler) where T : struct
        {
            if (m_eventBus == null) return;
            m_eventBus.Subscribe(handler);
            m_unsubscribers.Add(() => m_eventBus.Unsubscribe(handler));
        }

        /// <summary>
        /// [설명]: 등록된 모든 이벤트 구독을 일괄 해제합니다.
        /// </summary>
        private void ClearSubscriptions()
        {
            foreach (var unsub in m_unsubscribers)
            {
                unsub?.Invoke();
            }
            m_unsubscribers.Clear();
        }
        #endregion
    }
}

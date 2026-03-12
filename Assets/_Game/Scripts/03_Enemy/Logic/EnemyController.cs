using UnityEngine;
using TowerBreakers.Enemy.Data;
using TowerBreakers.Enemy.View;
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
        private Core.Events.IEventBus m_eventBus;
        private int m_currentHp;
        private int m_enemyId;
        private bool m_isDead = false;
        private bool m_isInitialized = false;

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
        public void Initialize(EnemyData data, EnemyView view, EnemyPushLogic pushLogic, Core.Events.IEventBus eventBus, System.Action<EnemyView, string> onReclaim = null)
        {
            // 풀 재사용 시 이전 이벤트 구독 해제
            if (m_isInitialized && m_eventBus != null)
            {
                m_eventBus.Unsubscribe<Core.Events.OnDefendActionTriggered>(OnDefendTriggered);
            }

            m_data = data;
            m_view = view;
            m_pushLogic = pushLogic;
            m_eventBus = eventBus;
            m_onReclaim = onReclaim;

            m_currentHp = data.Hp;
            m_isDead = false;
            m_enemyId = s_nextEnemyId++;

            m_stateMachine = new EnemyStateMachine();

            // 상태 등록
            m_stateMachine.AddState(new EnemyPushState(m_view, m_data, m_pushLogic));
            m_stateMachine.AddState(new EnemyStunnedState(m_view, m_stateMachine));

            // 초기 상태 설정
            m_stateMachine.ChangeState<EnemyPushState>();

            // 광역 경직 이벤트 구독 (군집 전체 동시 경직)
            m_eventBus?.Subscribe<Core.Events.OnDefendActionTriggered>(OnDefendTriggered);

            m_isInitialized = true;
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
        #endregion

        #region 비즈니스 로직
        /// <summary>
        /// [설명]: 사망 처리를 수행합니다. 대열을 재정비하고 오브젝트를 반환합니다.
        /// </summary>
        private void Die()
        {
            if (m_isDead) return;
            m_isDead = true;

            Debug.Log($"[{gameObject.name}] 사망: 대열 재정비 및 풀 반환");

            // 1. 기차 대열 재연결 (중요: 뒷사람과 앞사람을 이어줌)
            if (m_pushLogic != null)
            {
                m_pushLogic.HandleDeath();
            }

            // 2. 처치 이벤트 발행
            m_eventBus?.Publish(new Core.Events.OnEnemyKilled(m_enemyId));

            // 3. 오브젝트 풀로 반환
            m_onReclaim?.Invoke(m_view, m_data.EnemyName);
        }

        /// <summary>
        /// [설명]: 플레이어의 방어 액션 발생 시 호출되어 기절 상태로 전환합니다.
        /// </summary>
        private void OnDefendTriggered(Core.Events.OnDefendActionTriggered evt)
        {
            if (m_stateMachine == null || m_isDead) return;

            // 이벤트에서 전달받은 기절 지속시간 적용
            var stunnedState = m_stateMachine.GetState<EnemyStunnedState>();
            if (stunnedState != null)
            {
                stunnedState.SetDuration(evt.StunDuration);
            }

            m_stateMachine.ChangeState<EnemyStunnedState>();
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
            // 메모리 누수 방지: 이벤트 구독 해제
            m_eventBus?.Unsubscribe<Core.Events.OnDefendActionTriggered>(OnDefendTriggered);
        }
        #endregion
    }
}

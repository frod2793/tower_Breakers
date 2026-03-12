using UnityEngine;
using TowerBreakers.Enemy.View;

namespace TowerBreakers.Enemy.Logic
{
    /// <summary>
    /// [설명]: 적의 기절(Stun) 상태입니다. 방어 액션 수신 시 정지합니다.
    /// </summary>
    public class EnemyStunnedState : IEnemyState
    {
        #region 내부 필드
        private readonly EnemyView m_view;
        private float m_duration;
        private float m_timer;
        private readonly EnemyStateMachine m_stateMachine;
        #endregion

        public EnemyStunnedState(EnemyView view, EnemyStateMachine stateMachine, float duration = 1.0f)
        {
            m_view = view;
            m_stateMachine = stateMachine;
            m_duration = duration;
        }

        /// <summary>
        /// [설명]: 외부에서 기절 지속시간을 동적으로 설정합니다.
        /// </summary>
        /// <param name="duration">기절 지속시간 (초)</param>
        public void SetDuration(float duration)
        {
            m_duration = duration;
        }

        public void OnEnter()
        {
            m_timer = 0f;
            // 기절 애니메이션 또는 이펙트 연출
            Debug.Log("[EnemyStunnedState] 적 기절 시작");
        }

        public void OnExit()
        {
            Debug.Log("[EnemyStunnedState] 적 기절 종료");
        }

        public void OnTick()
        {
            m_timer += Time.deltaTime;
            if (m_timer >= m_duration)
            {
                m_stateMachine.ChangeState<EnemyPushState>();
            }
        }
    }
}

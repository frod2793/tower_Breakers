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
        private readonly EnemyStateMachine m_stateMachine;
        private readonly System.Type m_returnStateType;
        private float m_duration;
        private float m_timer;
        #endregion

        public EnemyStunnedState(EnemyView view, EnemyStateMachine stateMachine, System.Type returnStateType, float duration = 1.0f)
        {
            m_view = view;
            m_stateMachine = stateMachine;
            m_returnStateType = returnStateType;
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
            // 기절 애니메이션 (SPUM의 IDLE 사용)
            m_view.PlayAnimation(global::PlayerState.IDLE);
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
                m_stateMachine.ChangeState(m_returnStateType);
            }
        }
    }
}

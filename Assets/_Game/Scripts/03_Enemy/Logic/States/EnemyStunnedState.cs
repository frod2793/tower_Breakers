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
            // [수정]: 기절 애니메이션 동기화 - 이미 재생 중이더라도 강제로 첫 프레임부터 재생
            if (m_view != null)
            {
                // SPUM의 PlayAnimation은 내부적으로 트리거를 사용할 수 있으므로, 
                // 군집 동기화를 위해 Animator에 직접 접근하여 상태를 강제 재생하거나 트리거를 초기화하는 방안 검토
                m_view.PlayAnimation(global::PlayerState.DAMAGED, 0);

                var animator = m_view.CachedAnimator;
                if (animator != null)
                {
                    // DAMAGED 상태의 해시값을 사용하여 즉시 강제 재생 (동기화 핵심)
                    animator.Play("DAMAGED", 0, 0f);
                }
            }
        }

        public void OnExit()
        {
            // Debug.Log("[EnemyStunnedState] 적 기절 종료"); // 로그 제거
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

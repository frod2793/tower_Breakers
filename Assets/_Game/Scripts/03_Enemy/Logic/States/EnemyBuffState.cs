using UnityEngine;
using TowerBreakers.Enemy.Data;
using TowerBreakers.Enemy.View;
using TowerBreakers.Core.Events;

namespace TowerBreakers.Enemy.Logic
{
    /// <summary>
    /// [설명]: 주변 아군에게 힐링 버프를 제공하는 상태입니다.
    /// 일정 시간 동안 애니메이션을 재생하고 이벤트를 발행한 후 전진 상태로 복귀합니다.
    /// </summary>
    public class EnemyBuffState : IEnemyState
    {
        #region 내부 필드
        private readonly EnemyView m_view;
        private readonly EnemyData m_data;
        private readonly EnemyStateMachine m_stateMachine;
        private readonly IEventBus m_eventBus;
        private int m_floorIndex;

        private float m_timer;
        #endregion

        public EnemyBuffState(EnemyView view, EnemyData data, EnemyStateMachine stateMachine, IEventBus eventBus, int floorIndex)
        {
            m_view = view;
            m_data = data;
            m_stateMachine = stateMachine;
            m_eventBus = eventBus;
            m_floorIndex = floorIndex;
        }

        #region 공개 메서드
        /// <summary>
        /// [설명]: 버프가 작용할 층 인덱스를 설정합니다.
        /// </summary>
        /// <param name="floorIndex">층 인덱스</param>
        public void SetFloorIndex(int floorIndex)
        {
            m_floorIndex = floorIndex;
        }
        #endregion

        public void OnEnter()
        {
            m_timer = 0f;
            
            // 버프 애니메이션 (IDLE 또는 특수 애니메이션 활용)
            m_view.PlayAnimation(global::PlayerState.IDLE);
            
            // 버프 요청 이벤트 발행
            m_eventBus?.Publish(new OnEnemyBuffRequested(m_floorIndex, m_data.BuffHealAmount));
            
            // [로그 제거]: 콘솔 노이즈 방지
        }

        public void OnExit() { }

        public void OnTick()
        {
            m_timer += Time.deltaTime;
            
            // 시전 시간이 지나면 전진 상태로 복귀
            if (m_timer >= m_data.AbilityDuration)
            {
                m_stateMachine.ChangeState<EnemySupportPushState>();
            }
        }
    }
}

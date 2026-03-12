using UnityEngine;
using TowerBreakers.Player.View;
using TowerBreakers.Enemy.Logic;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using TowerBreakers.Core.Events;

namespace TowerBreakers.Player.Logic
{
    /// <summary>
    /// [설명]: 플레이어의 방어(Defend) 상태입니다. 전방 적 행렬을 일시 기절시킵니다.
    /// </summary>
    public class PlayerDefendState : IPlayerState
    {
        #region 내부 필드
        private readonly PlayerView m_view;
        private readonly PlayerStateMachine m_stateMachine;
        private readonly Core.Events.IEventBus m_eventBus;
        #endregion

        public PlayerDefendState(PlayerView view, PlayerStateMachine stateMachine, Core.Events.IEventBus eventBus)
        {
            m_view = view;
            m_stateMachine = stateMachine;
            m_eventBus = eventBus;
        }

        public void OnEnter()
        {
            Debug.Log("[PlayerDefendState] 방어(기절) 실행");
            
            // 모든 적 군집에 광역 경직(Stun) 이벤트 전파 (지속 시간 2.0초)
            m_eventBus?.Publish(new OnDefendActionTriggered(2.0f));
            
            m_view.PlayAnimation(global::PlayerState.OTHER, 0); 
            
            // 일정 시간 후 Idle 복귀 (임시 0.5초)
            ReturnToIdleAfterDelay().Forget();
        }

        private async Cysharp.Threading.Tasks.UniTaskVoid ReturnToIdleAfterDelay()
        {
            await Cysharp.Threading.Tasks.UniTask.Delay(500);
            m_stateMachine.ChangeState<PlayerIdleState>();
        }

        public void OnExit() { }

        public void OnTick() { }
    }
}

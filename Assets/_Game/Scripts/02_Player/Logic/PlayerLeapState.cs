using UnityEngine;
using DG.Tweening;
using TowerBreakers.Player.Data;
using TowerBreakers.Player.View;

namespace TowerBreakers.Player.Logic
{
    /// <summary>
    /// [설명]: 플레이어의 도약(Leap) 상태입니다. 선두 적의 위치 앞까지 순식간에 이동합니다.
    /// </summary>
    public class PlayerLeapState : IPlayerState
    {
        #region 내부 필드
        private readonly PlayerView m_view;
        private readonly PlayerModel m_model;
        private readonly PlayerData m_data;
        private readonly PlayerStateMachine m_stateMachine;
        #endregion

        public PlayerLeapState(PlayerView view, PlayerModel model, PlayerData m_data, PlayerStateMachine stateMachine)
        {
            m_view = view;
            m_model = model;
            this.m_data = m_data;
            m_stateMachine = stateMachine;
        }

        public void OnEnter()
        {
            Debug.Log("[PlayerLeapState] 도약 시작");
            ExecuteLeap();
        }

        public void OnExit() { }

        public void OnTick() { }

        private void ExecuteLeap()
        {
            // 목표 위치 계산 (현재 위치에서 오른쪽으로 LeapDistance만큼 이동)
            // 화면 오른쪽 끝을 벗어나지 않도록 제한하거나, 그냥 적 앞으로 이동하는 연출
            float targetX = m_model.Position.x + m_data.LeapDistance;
            Vector2 targetPos = new Vector2(targetX, m_model.Position.y);

            // DOTween으로 포물선 도약 연출 (오른쪽으로 점프)
            m_view.transform.DOJump(targetPos, 1.5f, 1, 0.4f)
                .OnUpdate(() => m_model.Position = m_view.transform.position)
                .OnComplete(() => m_stateMachine.ChangeState<PlayerIdleState>());

            m_view.PlayAnimation(global::PlayerState.MOVE, 0); 
        }
    }
}

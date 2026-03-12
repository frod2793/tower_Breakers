using UnityEngine;

namespace TowerBreakers.Player.Logic
{
    /// <summary>
    /// [설명]: 플레이어의 기본 대기 상태입니다.
    /// </summary>
    public class PlayerIdleState : IPlayerState
    {
        private readonly TowerBreakers.Player.View.PlayerView m_view;

        public PlayerIdleState(TowerBreakers.Player.View.PlayerView view)
        {
            m_view = view;
        }

        public void OnEnter()
        {
            if (m_view != null)
            {
                m_view.PlayAnimation(global::PlayerState.IDLE, 0);
            }
        }

        public void OnExit() { }

        public void OnTick() { }
    }
}

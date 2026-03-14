using Cysharp.Threading.Tasks;
using UnityEngine;
using TowerBreakers.Core.Events;
using TowerBreakers.Player.View;

namespace TowerBreakers.Core.GameState
{
    /// <summary>
    /// [설명]: 게임 오버 상태를 관리하는 클래스입니다.
    /// 플레이어 사망 시 진입하며, 게임 로직을 중단하고 UI를 대기합니다.
    /// </summary>
    public class GameOverState : IGameState
    {
        private readonly PlayerView m_playerView;
        private readonly IEventBus m_eventBus;

        public GameOverState(PlayerView playerView, IEventBus eventBus)
        {
            m_playerView = playerView;
            m_eventBus = eventBus;
        }

        public UniTask OnEnter()
        {
            Debug.Log("[GameOverState] 진입");
            
            // 플레이어 사망 애니메이션 재생
            if (m_playerView != null)
            {
                m_playerView.PlayAnimation(global::PlayerState.DEATH, 0);
            }

            return UniTask.CompletedTask;
        }

        public UniTask OnExit()
        {
            Debug.Log("[GameOverState] 퇴장");
            return UniTask.CompletedTask;
        }

        public void OnUpdate()
        {
            // 게임 오버 상태에서는 기본 게임 로직(이동, 스폰 등)을 수행하지 않음
        }
    }
}

using TowerBreakers.Core.Events;
using TowerBreakers.Player.Data;
using System;

namespace TowerBreakers.Combat
{
    /// <summary>
    /// [설명]: 게임의 전투 및 데미지 판정을 중앙에서 관리하는 시스템입니다.
    /// </summary>
    public class CombatSystem : IDisposable
    {
        #region 내부 필드
        private readonly IEventBus m_eventBus;
        private readonly PlayerModel m_playerModel;
        #endregion

        #region 초기화
        public CombatSystem(IEventBus eventBus, PlayerModel playerModel)
        {
            m_eventBus = eventBus;
            m_playerModel = playerModel;
            
            m_eventBus.Subscribe<OnPlayerPushed>(HandlePlayerPushedAtWall);
        }
        #endregion

        #region 비즈니스 로직
        /// <summary>
        /// [설명]: 플레이어가 벽에 밀렸을 때의 데미지 처리를 수행합니다.
        /// </summary>
        private void HandlePlayerPushedAtWall(OnPlayerPushed evt)
        {
            // 벽에 닿은 상태에서 밀리는 양에 비례하여 데미지 적용 (임시 공식)
            int damage = (int)(evt.PushDistance * 10f);
            if (damage > 0)
            {
                m_playerModel.TakeDamage(damage);
                m_eventBus.Publish(new OnPlayerDamaged(damage, m_playerModel.CurrentHp));
                
                if (m_playerModel.IsDead)
                {
                    m_eventBus.Publish(new OnGameOver());
                }
            }
        }
        #endregion

        #region 해제
        public void Dispose()
        {
            m_eventBus.Unsubscribe<OnPlayerPushed>(HandlePlayerPushedAtWall);
        }
        #endregion
    }
}

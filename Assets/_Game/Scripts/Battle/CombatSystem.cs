using System;
using TowerBreakers.Core.Events;
using TowerBreakers.Player.Logic;
using TowerBreakers.Player.DTO;
using VContainer.Unity;

namespace TowerBreakers.Battle
{
    /// <summary>
    /// [클래스]: 전투 판정 및 데미지 처리를 담당하는 핵심 시스템입니다.
    /// EventBus를 통해 물리적 상호작용(밀림, 벽 도달)을 감지하고 비즈니스 로직을 실행합니다.
    /// </summary>
    public class CombatSystem : IInitializable, IDisposable
    {
        #region 내부 필드
        private readonly IEventBus m_eventBus;
        private readonly PlayerLogic m_playerLogic;
        private readonly PlayerConfigDTO m_playerConfig;
        #endregion

        #region 초기화
        public CombatSystem(IEventBus eventBus, PlayerLogic playerLogic, PlayerConfigDTO playerConfig)
        {
            m_eventBus = eventBus;
            m_playerLogic = playerLogic;
            m_playerConfig = playerConfig;
        }

        public void Initialize()
        {
            SubscribeEvents();
        }

        private void SubscribeEvents()
        {
            // [참고]: 벽 압착 감지는 현재 PlayerPushReceiver가 수행하고 있으나, 
            // 추후 이 시스템에서 직접 감지하도록 통합할 수 있습니다.
        }

        public void Dispose()
        {
            UnsubscribeEvents();
        }

        private void UnsubscribeEvents()
        {
        }
        #endregion

        #region 핵심 로직
        /// <summary>
        /// [설명]: 벽 압착 시 데미지 처리를 수행합니다.
        /// </summary>
        public void HandleWallCrush()
        {
            if (m_playerLogic == null || m_playerConfig == null) return;

            // 데미지 입힘
            m_playerLogic.TakeDamage(m_playerConfig.DamagePerHit);

            // [연출 요청]: 타격 연출(카메라 쉐이크 등)을 위해 이벤트 발행
            m_eventBus.Publish(new OnWallCrushOccurred());
        }
        #endregion
    }
}

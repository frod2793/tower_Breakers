using System;
using TowerBreakers.Player.Data;
using TowerBreakers.Tower.Logic;
using TowerBreakers.Core;
using TowerBreakers.Core.Events;

namespace TowerBreakers.UI.HUD
{
    /// <summary>
    /// [설명]: HUD의 데이터를 관리하고 뷰와 바인딩되는 뷰모델 클래스입니다.
    /// </summary>
    public class HUDViewModel : IDisposable
    {
        #region 내부 필드
        private readonly PlayerModel m_playerModel;
        private readonly TowerManager m_towerManager;
        private readonly CooldownSystem m_cooldownSystem;
        private readonly IEventBus m_eventBus;
        #endregion

        #region 프로퍼티 (View가 구독할 데이터)
        public float HpRatio => (float)m_playerModel.CurrentHp / m_playerModel.MaxHp;
        public int CurrentFloor => m_towerManager.CurrentFloorIndex + 1;
        #endregion

        #region 이벤트
        public event Action OnDataUpdated;
        #endregion

        public HUDViewModel(PlayerModel playerModel, TowerManager towerManager, CooldownSystem cooldownSystem, IEventBus eventBus)
        {
            m_playerModel = playerModel;
            m_towerManager = towerManager;
            m_cooldownSystem = cooldownSystem;
            m_eventBus = eventBus;

            m_playerModel.OnHpChanged += HandleHpChanged;
            m_eventBus.Subscribe<OnFloorCleared>(HandleFloorCleared);
        }

        private void HandleHpChanged(int current, int max) => OnDataUpdated?.Invoke();
        private void HandleFloorCleared(OnFloorCleared evt) => OnDataUpdated?.Invoke();

        public float GetCooldownProgress(string actionName) => m_cooldownSystem.GetNormalizedProgress(actionName);

        public void Dispose()
        {
            m_playerModel.OnHpChanged -= HandleHpChanged;
            m_eventBus.Unsubscribe<OnFloorCleared>(HandleFloorCleared);
        }
    }
}

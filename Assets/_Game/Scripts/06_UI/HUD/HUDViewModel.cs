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
        public int CurrentLifeCount => m_playerModel.CurrentLifeCount;
        public int MaxLifeCount => m_playerModel.MaxLifeCount;
        public int KillCount => m_playerModel.KillCount;
        public int ChestCount => m_playerModel.ChestCount;
        public int CurrentFloor => m_towerManager.CurrentFloorIndex + 1;
        public bool IsGoVisible { get; private set; } = false;
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

            m_playerModel.OnLifeCountChanged += HandleLifeCountChanged;
            m_playerModel.OnKillsChanged += HandleKillsChanged;
            m_playerModel.OnChestsChanged += HandleChestsChanged;

            m_eventBus.Subscribe<OnFloorCleared>(HandleFloorCleared);
            m_eventBus.Subscribe<OnFloorReadyForNext>(HandleFloorReadyForNext);
            m_eventBus.Subscribe<OnEnemyKilled>(HandleEnemyKilled);
            m_eventBus.Subscribe<OnChestCollected>(HandleChestCollected);
        }

        private void HandleLifeCountChanged(int current, int max) => OnDataUpdated?.Invoke();
        private void HandleKillsChanged(int kills) => OnDataUpdated?.Invoke();
        private void HandleChestsChanged(int chests) => OnDataUpdated?.Invoke();

        private void HandleEnemyKilled(OnEnemyKilled evt)
        {
            m_playerModel.AddKill();
        }

        private void HandleChestCollected(OnChestCollected evt)
        {
            for (int i = 0; i < evt.Count; i++)
            {
                m_playerModel.AddChest();
            }
        }

        private void HandleFloorReadyForNext(OnFloorReadyForNext evt)
        {
            IsGoVisible = true;
            OnDataUpdated?.Invoke();
        }

        private void HandleFloorCleared(OnFloorCleared evt)
        {
            IsGoVisible = false;
            
            // [추가]: 층 클리어 보상으로 보물상자 1개 획득 (임시 연동)
            m_eventBus.Publish(new OnChestCollected(1));
            
            OnDataUpdated?.Invoke();
        }

        public float GetCooldownProgress(string actionName) => m_cooldownSystem.GetNormalizedProgress(actionName);

        public void Dispose()
        {
            m_playerModel.OnLifeCountChanged -= HandleLifeCountChanged;
            m_playerModel.OnKillsChanged -= HandleKillsChanged;
            m_playerModel.OnChestsChanged -= HandleChestsChanged;

            m_eventBus.Unsubscribe<OnFloorCleared>(HandleFloorCleared);
            m_eventBus.Unsubscribe<OnFloorReadyForNext>(HandleFloorReadyForNext);
            m_eventBus.Unsubscribe<OnEnemyKilled>(HandleEnemyKilled);
            m_eventBus.Unsubscribe<OnChestCollected>(HandleChestCollected);
        }
    }
}

using System;
using UnityEngine;
using TowerBreakers.Player.Data.SO;
using TowerBreakers.Player.Data.Models;
using TowerBreakers.Core.Events;
using VContainer;

namespace TowerBreakers.Interactions.Logic
{
    /// <summary>
    /// [설명]: 보상 상자 개방 시 보상 테이블에서 아이템을 추첨하여 플레이어 인벤토리에 적용합니다.
    /// OnRewardChestOpened 이벤트를 수신하여 동작하는 POCO 클래스입니다.
    /// </summary>
    public class RewardApplier : IDisposable
    {
        #region 내부 필드
        private readonly IEventBus m_eventBus;
        private readonly InventoryModel m_inventory;
        private readonly PlayerModel m_playerModel;
        private readonly RewardTableData m_rewardTable;
        #endregion

        #region 초기화
        public RewardApplier(IEventBus eventBus, InventoryModel inventory, PlayerModel playerModel, RewardTableData rewardTable)
        {
            m_eventBus = eventBus;
            m_inventory = inventory;
            m_playerModel = playerModel;
            m_rewardTable = rewardTable;

            if (m_eventBus != null)
            {
                m_eventBus.Subscribe<OnRewardChestOpened>(ApplyReward);
            }
        }
        #endregion

        #region 내부 로직
        /// <summary>
        /// [설명]: 상자가 열렸을 때 보상을 추첨하여 인벤토리에 추가합니다.
        /// </summary>
        /// <param name="evt">상자 개방 이벤트 데이터</param>
        private void ApplyReward(OnRewardChestOpened evt)
        {
            if (m_inventory == null || m_playerModel == null)
            {
                Debug.LogWarning("[RewardApplier] 필요한 디펜던시가 설정되지 않았습니다.");
                return;
            }

            // 이벤트에서 전달된 보상 테이블을 우선 사용하고, 없으면 등록된 테이블을 사용
            var rewardTable = evt.RewardTable ?? m_rewardTable;
            if (rewardTable == null)
            {
                Debug.LogWarning("[RewardApplier] 보상 테이블이 설정되지 않았습니다.");
                return;
            }

            // 1. 보상 추첨
            RewardEntry entry = rewardTable.GetRandomReward();
            if (entry == null)
            {
                Debug.LogWarning("[RewardApplier] 보상 추첨 결과가 null입니다.");
                return;
            }

            // 2. 타입에 따른 인벤토리 추가 및 즉시 효과 적용 (순수 로직 계층은 Sprite를 직접 다루지 않음)
            string rewardKey = string.Empty;

            if (entry.IsWeapon)
            {
                m_inventory.AddWeapon(entry.Weapon);
                rewardKey = entry.Weapon.WeaponName;
                Debug.Log($"[RewardApplier] 무기 획득: {rewardKey}");
            }
            else if (entry.IsArmor)
            {
                m_inventory.AddArmor(entry.Armor);
                rewardKey = entry.Armor.ArmorName;
                
                // 갑주 획득 시 보너스 회복 적용
                if (entry.Armor.HealAmount > 0)
                {
                    m_playerModel.Heal(entry.Armor.HealAmount);
                    Debug.Log($"[RewardApplier] 갑주 획득 보너스 회복: {entry.Armor.HealAmount}");
                }
                
                Debug.Log($"[RewardApplier] 갑주 획득: {rewardKey}");
            }

            // 3. 연출을 위한 이벤트 발행 (순수 데이터인 string 키만 전달)
            if (!string.IsNullOrEmpty(rewardKey))
            {
                m_eventBus?.Publish(new OnRewardSpawned(rewardKey, evt.Position, evt.FloorIndex));
            }
            else
            {
                Debug.LogWarning("[RewardApplier] 보상 정보를 찾을 수 없습니다.");
            }
        }
        #endregion

        #region 인터페이스 구현
        public void Dispose()
        {
            if (m_eventBus != null)
            {
                m_eventBus.Unsubscribe<OnRewardChestOpened>(ApplyReward);
            }
        }
        #endregion
    }
}

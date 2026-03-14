using UnityEngine;
using TowerBreakers.Player.Data.SO;
using TowerBreakers.Player.Data.Models;
using TowerBreakers.Core.Events;
using VContainer;

namespace TowerBreakers.Interactions.Logic
{
    /// <summary>
    /// [설명]: 보상 상자 개방 시 보상 테이블에서 아이템을 추첨하여 플레이어 인벤토리에 적용합니다.
    /// OnRewardChestOpened 이벤트를 수신하여 동작합니다.
    /// </summary>
    public class RewardApplier : MonoBehaviour
    {
        #region 에디터 설정
        [SerializeField, Tooltip("기본 보상 테이블")]
        private RewardTableData m_defaultRewardTable;
        #endregion

        #region 내부 필드
        private IEventBus m_eventBus;
        private InventoryModel m_inventory;
        #endregion

        #region 초기화
        [Inject]
        public void Construct(IEventBus eventBus, InventoryModel inventory)
        {
            m_eventBus = eventBus;
            m_inventory = inventory;

            if (m_eventBus != null)
            {
                m_eventBus.Subscribe<OnRewardChestOpened>(ApplyReward);
            }
        }
        #endregion

        #region 내부 로직
        /// <summary>
        /// [설명]: 상자가 열렸을 때 보상을 추첨하고 인벤토리에 추가합니다.
        /// </summary>
        /// <param name="evt">상자 개방 이벤트 데이터</param>
        private void ApplyReward(OnRewardChestOpened evt)
        {
            if (m_defaultRewardTable == null || m_inventory == null)
            {
                Debug.LogWarning("[RewardApplier] RewardTable 또는 InventoryModel이 설정되지 않았습니다.");
                return;
            }

            // 1. 보상 추첨
            RewardEntry entry = m_defaultRewardTable.GetRandomReward();
            if (entry == null)
            {
                Debug.LogWarning("[RewardApplier] 보상 추첨 결과가 null입니다.");
                return;
            }

            // 2. 타입에 따른 인벤토리 추가
            if (entry.IsWeapon)
            {
                m_inventory.AddWeapon(entry.Weapon);
                Debug.Log($"[RewardApplier] 무기 획득: {entry.Weapon.WeaponName}");
            }
            else if (entry.IsArmor)
            {
                m_inventory.AddArmor(entry.Armor);
                Debug.Log($"[RewardApplier] 갑주 획득: {entry.Armor.ArmorName}");
            }
            else
            {
                Debug.LogWarning("[RewardApplier] 유효한 보상 데이터가 엔트리에 없습니다.");
            }
        }
        #endregion

        #region 유니티 생명주기
        private void OnDestroy()
        {
            if (m_eventBus != null)
            {
                m_eventBus.Unsubscribe<OnRewardChestOpened>(ApplyReward);
            }
        }
        #endregion
    }
}

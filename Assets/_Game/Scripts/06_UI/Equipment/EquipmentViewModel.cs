using System;
using System.Collections.Generic;
using TowerBreakers.Player.Data.Models;
using TowerBreakers.Player.Data.SO;

namespace TowerBreakers.UI.Equipment
{
    /// <summary>
    /// [설명]: 인벤토리 UI와 플레이어 데이터를 연결하는 뷰모델 클래스입니다.
    /// UI에 표시될 정제된 데이터와 사용자 커맨드를 제공합니다.
    /// </summary>
    public class EquipmentViewModel
    {
        #region 내부 필드
        private readonly InventoryModel m_inventoryModel;
        private readonly PlayerModel m_playerModel;
        #endregion

        #region 프로퍼티
        /// <summary>
        /// [설명]: 전체 보유 중인 무기 목록입니다.
        /// </summary>
        public IReadOnlyList<WeaponData> OwnedWeapons => m_inventoryModel.OwnedWeapons;

        /// <summary>
        /// [설명]: 현재 인벤토리에서 선택(포커스)된 무기입니다.
        /// </summary>
        public WeaponData SelectedWeapon => m_inventoryModel.SelectedWeapon;

        /// <summary>
        /// [설명]: 플레이어가 현재 실제로 장착 중인 무기입니다.
        /// </summary>
        public WeaponData CurrentlyEquippedWeapon => m_playerModel.CurrentWeapon;
        #endregion

        #region 이벤트
        public event Action OnInventoryChanged;
        public event Action<WeaponData> OnSelectedWeaponChanged;
        public event Action<WeaponData> OnEquippedWeaponChanged;
        #endregion

        #region 생성자 및 초기화
        public EquipmentViewModel(InventoryModel inventoryModel, PlayerModel playerModel)
        {
            m_inventoryModel = inventoryModel;
            m_playerModel = playerModel;

            // 모델 이벤트 구독
            m_inventoryModel.OnInventoryUpdated += () => OnInventoryChanged?.Invoke();
            m_inventoryModel.OnSelectedWeaponChanged += (weapon) => OnSelectedWeaponChanged?.Invoke(weapon);
            m_playerModel.OnWeaponChanged += (weapon) => OnEquippedWeaponChanged?.Invoke(weapon);
        }
        #endregion

        #region 공개 명령 (Commands)
        /// <summary>
        /// [설명]: UI 슬롯 클릭 시 호출하여 선택된 무기를 변경합니다.
        /// </summary>
        public void SelectWeapon(WeaponData weapon)
        {
            m_inventoryModel.SelectWeapon(weapon);
        }

        /// <summary>
        /// [설명]: 현재 선택된 무기를 플레이어에게 실제로 장착합니다.
        /// </summary>
        public void EquipSelectedWeapon()
        {
            if (m_inventoryModel.SelectedWeapon != null)
            {
                m_playerModel.SetWeapon(m_inventoryModel.SelectedWeapon);
            }
        }

        /// <summary>
        /// [설명]: 현재 선택된 무기가 이미 장착된 무기인지 확인합니다.
        /// </summary>
        public bool IsSelectedWeaponEquipped()
        {
            if (m_inventoryModel.SelectedWeapon == null) return false;
            return m_inventoryModel.SelectedWeapon == m_playerModel.CurrentWeapon;
        }
        #endregion
    }
}

using System;
using System.Linq;
using TowerBreakers.Player.Data.SO;
using System.Collections.Generic;

namespace TowerBreakers.Player.Data.Models
{
    /// <summary>
    /// [설명]: 플레이어가 보유한 무기 목록과 현재 선택된 무기 정보를 관리하는 모델 클래스입니다.
    /// POCO 클래스로 작성되었으며, MVVM 패턴의 Model 역할을 수행합니다.
    /// </summary>
    public class InventoryModel
    {
        #region 내부 변수
        private List<WeaponData> m_ownedWeapons = new List<WeaponData>();
        private WeaponData m_selectedWeapon;

        private List<ArmorData> m_ownedArmors = new List<ArmorData>();
        private ArmorData m_selectedArmor;
        #endregion

        #region 프로퍼티
        /// <summary>
        /// [설명]: 보유 중인 무기 목록의 읽기 전용 리스트입니다.
        /// </summary>
        public IReadOnlyList<WeaponData> OwnedWeapons => m_ownedWeapons;

        /// <summary>
        /// [설명]: 현재 인벤토리 UI에서 포커스(선택)된 무기 데이터입니다.
        /// </summary>
        public WeaponData SelectedWeapon
        {
            get => m_selectedWeapon;
            private set
            {
                if (m_selectedWeapon == value) return;
                m_selectedWeapon = value;
                OnSelectedWeaponChanged?.Invoke(m_selectedWeapon);
            }
        }

        /// <summary>
        /// [설명]: 보유 중인 갑주 목록의 읽기 전용 리스트입니다.
        /// </summary>
        public IReadOnlyList<ArmorData> OwnedArmors => m_ownedArmors;

        /// <summary>
        /// [설명]: 현재 인벤토리 UI에서 포커스(선택)된 갑주 데이터입니다.
        /// </summary>
        public ArmorData SelectedArmor
        {
            get => m_selectedArmor;
            private set
            {
                if (m_selectedArmor == value) return;
                m_selectedArmor = value;
                OnSelectedArmorChanged?.Invoke(m_selectedArmor);
            }
        }
        #endregion

        #region 이벤트
        /// <summary>
        /// [설명]: 인벤토리 목록이 갱신되었을 때 호출됩니다.
        /// </summary>
        public event Action OnInventoryUpdated;

        /// <summary>
        /// [설명]: UI에서 선택된 무기가 변경되었을 때 호출됩니다.
        /// </summary>
        public event Action<WeaponData> OnSelectedWeaponChanged;

        /// <summary>
        /// [설명]: UI에서 선택된 갑주가 변경되었을 때 호출됩니다.
        /// </summary>
        public event Action<ArmorData> OnSelectedArmorChanged;
        #endregion

        #region 공개 메서드
        /// <summary>
        /// [설명]: 보유 목록에 새로운 무기를 추가합니다.
        /// </summary>
        /// <param name="weapon">추가할 무기 데이터</param>
        public void AddWeapon(WeaponData weapon)
        {
            if (weapon == null || m_ownedWeapons.Contains(weapon)) return;
            
            m_ownedWeapons.Add(weapon);
            OnInventoryUpdated?.Invoke();

            // 처음 추가된 무기라면 자동으로 선택
            if (m_selectedWeapon == null)
            {
                SelectWeapon(weapon);
            }
        }

        /// <summary>
        /// [설명]: UI에서 특정 무기를 클릭했을 때 선택 상태를 갱신합니다.
        /// </summary>
        /// <param name="weapon">선택할 무기 데이터</param>
        public void SelectWeapon(WeaponData weapon)
        {
            if (weapon != null && m_ownedWeapons.Contains(weapon))
            {
                SelectedWeapon = weapon;
            }
        }

        /// <summary>
        /// [설명]: 보유 목록에 새로운 갑주를 추가합니다.
        /// </summary>
        /// <param name="armor">추가할 갑주 데이터</param>
        public void AddArmor(ArmorData armor)
        {
            if (armor == null || m_ownedArmors.Contains(armor)) return;

            m_ownedArmors.Add(armor);
            OnInventoryUpdated?.Invoke();

            // 처음 추가된 갑주라면 자동으로 선택
            if (m_selectedArmor == null)
            {
                SelectArmor(armor);
            }
        }

        /// <summary>
        /// [설명]: UI에서 특정 갑주를 클릭했을 때 선택 상태를 갱신합니다.
        /// </summary>
        /// <param name="armor">선택할 갑주 데이터</param>
        public void SelectArmor(ArmorData armor)
        {
            if (armor != null && m_ownedArmors.Contains(armor))
            {
                SelectedArmor = armor;
            }
        }
        #endregion
    }
}

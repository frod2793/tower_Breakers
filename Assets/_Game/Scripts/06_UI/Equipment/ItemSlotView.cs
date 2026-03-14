using UnityEngine;
using UnityEngine.UI;
using TowerBreakers.Player.Data.SO;
using System;

namespace TowerBreakers.UI.Equipment
{
    /// <summary>
    /// [설명]: 인벤토리의 개별 장비 슬롯을 관리하는 뷰 클래스입니다.
    /// 사용자의 요청에 따라 아이콘과 장착 버튼만 포함하도록 간소화되었습니다.
    /// </summary>
    public class ItemSlotView : MonoBehaviour
    {
        #region 에디터 설정
        [SerializeField, Tooltip("장비 아이콘 이미지")]
        private Image m_iconImage;

        [SerializeField, Tooltip("슬롯/장착 버튼")]
        private Button m_slotButton;

        [SerializeField, Tooltip("장착 중 표시 오브젝트")]
        private GameObject m_equippedMark;
        #endregion

        #region 내부 변수
        private WeaponData m_weaponData;
        private ArmorData m_armorData;
        private Action<WeaponData> m_onWeaponClicked;
        private Action<ArmorData> m_onArmorClicked;
        #endregion

        #region 초기화
        /// <summary>
        /// [설명]: 슬롯의 무기 데이터와 콜백을 설정합니다.
        /// </summary>
        public void SetWeaponSlot(WeaponData data, Action<WeaponData> onClicked)
        {
            if (data == null) return;

            m_weaponData = data;
            m_armorData = null;
            m_onWeaponClicked = onClicked;

            if (m_iconImage != null)
            {
                m_iconImage.sprite = data.Icon;
            }

            if (m_slotButton != null)
            {
                m_slotButton.onClick.RemoveAllListeners();
                m_slotButton.onClick.AddListener(() => m_onWeaponClicked?.Invoke(m_weaponData));
            }
        }

        /// <summary>
        /// [설명]: 슬롯의 갑주 데이터와 콜백을 설정합니다.
        /// </summary>
        public void SetArmorSlot(ArmorData data, Action<ArmorData> onClicked)
        {
            if (data == null) return;

            m_armorData = data;
            m_weaponData = null;
            m_onArmorClicked = onClicked;

            if (m_iconImage != null)
            {
                m_iconImage.sprite = data.Icon;
            }

            if (m_slotButton != null)
            {
                m_slotButton.onClick.RemoveAllListeners();
                m_slotButton.onClick.AddListener(() => m_onArmorClicked?.Invoke(m_armorData));
            }
        }
        #endregion

        #region 상태 업데이트
        /// <summary>
        /// [설명]: 현재 슬롯의 장착 상태 시각화를 갱신합니다.
        /// </summary>
        public void UpdateEquippedState(WeaponData equippedWeapon, ArmorData equippedHelmet, ArmorData equippedBodyArmor)
        {
            bool isEquipped = false;

            if (m_weaponData != null)
            {
                isEquipped = m_weaponData == equippedWeapon;
            }
            else if (m_armorData != null)
            {
                if (m_armorData.Category == ArmorCategory.Helmet)
                {
                    isEquipped = m_armorData == equippedHelmet;
                }
                else
                {
                    isEquipped = m_armorData == equippedBodyArmor;
                }
            }

            if (m_equippedMark != null)
            {
                m_equippedMark.SetActive(isEquipped);
            }
        }
        #endregion
    }
}

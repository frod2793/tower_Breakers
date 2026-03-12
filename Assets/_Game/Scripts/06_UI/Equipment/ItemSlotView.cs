using UnityEngine;
using UnityEngine.UI;
using TMPro;
using TowerBreakers.Player.Data;
using System;

namespace TowerBreakers.UI.Equipment
{
    /// <summary>
    /// [설명]: 인벤토리의 개별 무기 슬롯을 관리하는 뷰 클래스입니다.
    /// </summary>
    public class ItemSlotView : MonoBehaviour
    {
        #region 에디터 설정
        [SerializeField, Tooltip("무기 아이콘 이미지")]
        private Image m_iconImage;

        [SerializeField, Tooltip("슬롯 클릭 버튼")]
        private Button m_slotButton;

        [SerializeField, Tooltip("장착 중 표시 오브젝트")]
        private GameObject m_equippedMark;

        [SerializeField, Tooltip("선택 중 표시 오브젝트")]
        private GameObject m_selectedMark;
        #endregion

        #region 내부 변수
        private WeaponData m_data;
        private Action<WeaponData> m_onClicked;
        #endregion

        #region 초기화
        /// <summary>
        /// [설명]: 슬롯의 데이터와 콜백을 설정합니다.
        /// </summary>
        public void SetSlot(WeaponData data, Action<WeaponData> onClicked)
        {
            if (data == null) return;

            m_data = data;
            m_onClicked = onClicked;

            if (m_iconImage != null)
            {
                m_iconImage.sprite = data.WeaponSprite;
            }

            if (m_slotButton != null)
            {
                m_slotButton.onClick.RemoveAllListeners();
                m_slotButton.onClick.AddListener(() => m_onClicked?.Invoke(m_data));
            }
        }
        #endregion

        #region 공개 API
        /// <summary>
        /// [설명]: 현재 슬롯의 장착 및 선택 시각화 상태를 갱신합니다.
        /// </summary>
        public void UpdateState(WeaponData selected, WeaponData equipped)
        {
            if (m_data == null) return;

            if (m_equippedMark != null)
            {
                m_equippedMark.SetActive(m_data == equipped);
            }

            if (m_selectedMark != null)
            {
                m_selectedMark.SetActive(m_data == selected);
            }
        }
        #endregion
    }
}

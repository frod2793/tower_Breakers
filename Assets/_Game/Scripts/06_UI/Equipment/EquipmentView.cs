using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using TowerBreakers.Player.Data.SO;

namespace TowerBreakers.UI.Equipment
{
    /// <summary>
    /// [설명]: 장비 인벤토리 화면을 관리하는 메인 뷰 클래스입니다.
    /// MVVM 패턴을 따르며 ViewModel의 상태를 시각화합니다.
    /// </summary>
    public class EquipmentView : MonoBehaviour
    {
        #region 에디터 설정
        [Header("슬롯 설정")]
        [SerializeField, Tooltip("슬롯들이 생성될 부모 컨테이너")]
        private Transform m_slotGrid;

        [SerializeField, Tooltip("무기 슬롯 프리팹")]
        private ItemSlotView m_slotPrefab;

        [Header("정보 패널")]
        [SerializeField, Tooltip("선택된 무기 이름 텍스트")]
        private TMP_Text m_weaponNameText;

        [SerializeField, Tooltip("선택된 무기 상세 정보 텍스트")]
        private TMP_Text m_weaponDetailText;

        [Header("버튼")]
        [SerializeField, Tooltip("장착 버튼")]
        private Button m_equipButton;

        [SerializeField, Tooltip("닫기 버튼")]
        private Button m_closeButton;
        #endregion

        #region 내부 변수
        private EquipmentViewModel m_viewModel;
        private List<ItemSlotView> m_slots = new List<ItemSlotView>();

        // 이벤트 해제를 위한 캐시된 핸들러
        private Action<WeaponData> m_handleSelectedChanged;
        private Action<WeaponData> m_handleEquippedChanged;
        #endregion

        #region 초기화 및 바인딩 로직
        /// <summary>
        /// [설명]: 외부에서 주입된 뷰모델을 사용하여 뷰를 초기화합니다.
        /// </summary>
        public void Initialize(EquipmentViewModel viewModel)
        {
            if (viewModel == null)
            {
                Debug.LogError("[EquipmentView] ViewModel이 null입니다!");
                return;
            }

            m_viewModel = viewModel;
            Bind();
            
            // 초기 화면 갱신
            RefreshUI();
        }

        private void Bind()
        {
            if (m_viewModel == null) return;

            // 이벤트 해제를 위해 핸들러 캐싱
            m_handleSelectedChanged = _ => RefreshInfoPanel();
            m_handleEquippedChanged = _ => RefreshUI();

            // 모델 이벤트 바인딩
            m_viewModel.OnInventoryChanged += RefreshUI;
            m_viewModel.OnSelectedWeaponChanged += m_handleSelectedChanged;
            m_viewModel.OnEquippedWeaponChanged += m_handleEquippedChanged;

            // UI 이벤트 바인딩
            if (m_equipButton != null)
            {
                m_equipButton.onClick.AddListener(OnEquipButtonClicked);
            }

            if (m_closeButton != null)
            {
                m_closeButton.onClick.AddListener(() => gameObject.SetActive(false));
            }
        }
        #endregion

        #region UI 갱신 로직
        /// <summary>
        /// [설명]: 전체 슬롯 목록과 정보 패널을 새로 고칩니다.
        /// </summary>
        private void RefreshUI()
        {
            if (m_viewModel == null || m_slotGrid == null || m_slotPrefab == null) return;

            // 기존 슬롯 제거 (간단한 구현을 위해 전체 재생성)
            foreach (var slot in m_slots)
            {
                if (slot != null) Destroy(slot.gameObject);
            }
            m_slots.Clear();

            // 보유 무기 리스트 순회하며 슬롯 생성
            var ownedWeapons = m_viewModel.OwnedWeapons;
            foreach (var weapon in ownedWeapons)
            {
                var newSlot = Instantiate(m_slotPrefab, m_slotGrid);
                if (newSlot != null)
                {
                    newSlot.SetSlot(weapon, m_viewModel.SelectWeapon);
                    m_slots.Add(newSlot);
                }
            }

            RefreshInfoPanel();
            UpdateSlotHighlights();
        }

        /// <summary>
        /// [설명]: 선택된 무기의 상세 스탯 정보를 패널에 표시합니다.
        /// </summary>
        private void RefreshInfoPanel()
        {
            if (m_viewModel == null) return;

            var selected = m_viewModel.SelectedWeapon;
            if (selected == null)
            {
                if (m_weaponNameText != null) m_weaponNameText.text = "선택된 무기 없음";
                if (m_weaponDetailText != null) m_weaponDetailText.text = "-";
                if (m_equipButton != null) m_equipButton.interactable = false;
                return;
            }

            if (m_weaponNameText != null) m_weaponNameText.text = selected.WeaponName;
            
            if (m_weaponDetailText != null)
            {
                m_weaponDetailText.text = $"공격력 보정: x{selected.AttackPowerModifier:F1}\n" +
                                          $"사거리 보정: x{selected.AttackRangeModifier:F1}\n" +
                                          $"공격속도 보정: x{selected.AttackSpeedModifier:F1}";
            }

            if (m_equipButton != null)
            {
                // 이미 장착 중이면 비활성화
                m_equipButton.interactable = !m_viewModel.IsSelectedWeaponEquipped();
            }

            UpdateSlotHighlights();
        }

        /// <summary>
        /// [설명]: 슬롯들의 선택/장착 하이라이트 상태만 업데이트합니다.
        /// </summary>
        private void UpdateSlotHighlights()
        {
            if (m_viewModel == null) return;

            var selected = m_viewModel.SelectedWeapon;
            var equipped = m_viewModel.CurrentlyEquippedWeapon;

            foreach (var slot in m_slots)
            {
                if (slot != null)
                {
                    slot.UpdateState(selected, equipped);
                }
            }
        }
        #endregion

        #region UI 이벤트 핸들러
        private void OnEquipButtonClicked()
        {
            if (m_viewModel != null)
            {
                m_viewModel.EquipSelectedWeapon();
            }
        }
        #endregion

        #region 유니티 생명주기
        private void OnDestroy()
        {
            if (m_viewModel != null)
            {
                m_viewModel.OnInventoryChanged -= RefreshUI;
                m_viewModel.OnSelectedWeaponChanged -= m_handleSelectedChanged;
                m_viewModel.OnEquippedWeaponChanged -= m_handleEquippedChanged;
            }
        }
        #endregion
    }
}

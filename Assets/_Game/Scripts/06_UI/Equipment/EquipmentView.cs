using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using TowerBreakers.Player.Data.SO;
using TowerBreakers.Player.View;
using VContainer;

namespace TowerBreakers.UI.Equipment
{
    /// <summary>
    /// [설명]: 장비 인벤토리 화면을 관리하는 메인 뷰 클래스입니다.
    /// MVVM 패턴을 따르며 ViewModel 및 UserSessionModel과 연동됩니다.
    /// </summary>
    public class EquipmentView : MonoBehaviour
    {
        #region 에디터 설정
        [Header("카테고리 탭")]
        [SerializeField, Tooltip("무기 탭 버튼")]
        private Button m_weaponTabButton;

        [SerializeField, Tooltip("갑주 탭 버튼")]
        private Button m_armorTabButton;

        [Header("슬롯 설정")]
        [SerializeField, Tooltip("슬롯들이 생성될 부모 컨테이너")]
        private Transform m_slotGrid;

        [SerializeField, Tooltip("장비 슬롯 프리팹")]
        private ItemSlotView m_slotPrefab;

        [Header("리소스 설정")]
        [SerializeField, Tooltip("자동 로드할 슬롯 프리팹 경로")]
        private string m_slotPrefabPath = "Prefabs/itemSlot";

        [Header("버튼")]
        [SerializeField, Tooltip("저장 버튼"), UnityEngine.Serialization.FormerlySerializedAs("m_equipButton")]
        private Button m_saveButton;

        [Header("프리뷰")]
        [SerializeField, Tooltip("프리뷰용 플레이어 뷰")]
        private PlayerView m_playerPreview;
        #endregion

        #region 내부 변수
        private EquipmentViewModel m_viewModel;
        private List<ItemSlotView> m_slots = new List<ItemSlotView>();
        #endregion

        #region 초기화 및 바인딩
        /// <summary>
        /// [설명]: 리팩토링된 뷰모델을 사용하여 뷰를 초기화합니다.
        /// </summary>
        [Inject]
        public void Initialize(EquipmentViewModel viewModel)
        {
            if (viewModel == null) return;
            m_viewModel = viewModel;

            if (m_playerPreview == null)
            {
                m_playerPreview = FindFirstObjectByType<PlayerView>();
            }

            Bind();
            Refresh();
        }

        private void Bind()
        {
            if (m_viewModel == null) return;

            m_viewModel.OnDataUpdated += Refresh;
            m_viewModel.OnCategoryChanged += Refresh;
            
            // 실시간 프리뷰 및 슬롯 마크 갱신 연동
            m_viewModel.OnSelectedWeaponChanged += (weapon) => 
            {
                if (m_playerPreview != null) m_playerPreview.SetWeapon(weapon);
                UpdateSlotMarks();
            };
            
            m_viewModel.OnSelectedArmorChanged += (armor) => 
            {
                if (m_playerPreview != null) m_playerPreview.SetArmor(armor);
                UpdateSlotMarks();
            };

            if (m_saveButton != null)
            {
                m_saveButton.onClick.RemoveAllListeners();
                m_saveButton.onClick.AddListener(() => m_viewModel.SaveEquipmentSelection());
            }

            if (m_weaponTabButton != null)
            {
                m_weaponTabButton.onClick.RemoveAllListeners();
                m_weaponTabButton.onClick.AddListener(() => m_viewModel.SetCategory(EquipmentCategory.Weapon));
            }

            if (m_armorTabButton != null)
            {
                m_armorTabButton.onClick.RemoveAllListeners();
                m_armorTabButton.onClick.AddListener(() => m_viewModel.SetCategory(EquipmentCategory.Armor));
            }
        }
        #endregion

        #region 내부 로직
        private void Refresh()
        {
            if (m_viewModel == null)
            {
                return;
            }

            // [방어 코드]: 필수 컴포넌트 누락 시 자동 검색 및 로드
            if (m_slotGrid == null)
            {
                m_slotGrid = transform.Find("Grid");
                if (m_slotGrid == null)
                {
                    m_slotGrid = transform.Find("SlotGrid");
                }
                if (m_slotGrid == null)
                {
                    m_slotGrid = transform.Find("Viewport/Content");
                }
            }

            if (m_slotPrefab == null)
            {
                m_slotPrefab = Resources.Load<ItemSlotView>(m_slotPrefabPath);
            }

            // 인게임 등 UI가 활성화되지 않은 환경이나 설정을 의도적으로 비워둔 경우 에러 없이 리턴
            if (m_slotGrid == null || m_slotPrefab == null)
            {
                return;
            }

            // 기존 슬롯 제거
            foreach (var slot in m_slots)
            {
                if (slot != null) Destroy(slot.gameObject);
            }
            m_slots.Clear();

            // 카테고리에 따른 슬롯 생성
            if (m_viewModel.CurrentCategory == EquipmentCategory.Weapon)
            {
                foreach (var weapon in m_viewModel.OwnedWeapons)
                {
                    CreateWeaponSlot(weapon);
                }
            }
            else
            {
                foreach (var armor in m_viewModel.OwnedArmors)
                {
                    CreateArmorSlot(armor);
                }
            }
            
            UpdateSlotMarks();
            UpdateTabVisuals();

            Debug.Log($"[EquipmentView] UI 리프레시 완료: {m_slots.Count}개의 슬롯 생성됨 (Category: {m_viewModel.CurrentCategory})");
        }

        private void CreateWeaponSlot(WeaponData data)
        {
            var slot = Instantiate(m_slotPrefab, m_slotGrid);
            if (slot != null)
            {
                slot.SetWeaponSlot(data, (w) => m_viewModel.SelectWeapon(w));
                m_slots.Add(slot);
            }
        }

        private void CreateArmorSlot(ArmorData data)
        {
            var slot = Instantiate(m_slotPrefab, m_slotGrid);
            if (slot != null)
            {
                slot.SetArmorSlot(data, (a) => m_viewModel.SelectArmor(a));
                m_slots.Add(slot);
            }
        }

        private void UpdateSlotMarks()
        {
            if (m_viewModel == null) return;

            foreach (var slot in m_slots)
            {
                if (slot != null)
                {
                    slot.UpdateEquippedState(
                        m_viewModel.EquippedWeapon,
                        m_viewModel.EquippedHelmet,
                        m_viewModel.EquippedBodyArmor
                    );
                }
            }
        }

        private void UpdateTabVisuals()
        {
            if (m_viewModel == null) return;

            bool isWeapon = m_viewModel.CurrentCategory == EquipmentCategory.Weapon;
            if (m_weaponTabButton != null) m_weaponTabButton.interactable = !isWeapon;
            if (m_armorTabButton != null) m_armorTabButton.interactable = isWeapon;
        }
        #endregion
    }
}

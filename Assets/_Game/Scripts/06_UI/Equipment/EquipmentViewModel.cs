using System;
using System.Collections.Generic;
using System.Linq;
using TowerBreakers.Player.Data.Models;
using TowerBreakers.Player.Data.SO;
using TowerBreakers.Player.Data;

namespace TowerBreakers.UI.Equipment
{
    /// <summary>
    /// [설명]: 인벤토리 UI와 플레이어 데이터를 연결하는 뷰모델 클래스입니다.
    /// UserSessionModel과 연동하여 씬 간 장비 데이터를 유지합니다.
    /// </summary>
    public class EquipmentViewModel
    {
    #region 내부 필드
    private readonly InventoryModel m_inventoryModel;
    private readonly PlayerModel m_playerModel;
    private readonly UserSessionModel m_sessionModel;
    private readonly EquipmentDatabase m_database;
    private readonly PlayerData m_playerData;
    #endregion

        #region 프로퍼티
        public IReadOnlyList<WeaponData> OwnedWeapons => m_inventoryModel.OwnedWeapons;
        public IReadOnlyList<ArmorData> OwnedArmors => m_inventoryModel.OwnedArmors;

        public WeaponData SelectedWeapon => m_inventoryModel.SelectedWeapon;
        public ArmorData SelectedArmor => m_inventoryModel.SelectedArmor;

        public WeaponData EquippedWeapon => m_inventoryModel.EquippedWeapon;
        public ArmorData EquippedHelmet => m_inventoryModel.EquippedHelmet;
        public ArmorData EquippedBodyArmor => m_inventoryModel.EquippedBodyArmor;

        public EquipmentCategory CurrentCategory { get; private set; } = EquipmentCategory.Weapon;
        #endregion

        #region 이벤트
        public event Action OnDataUpdated;
        public event Action OnCategoryChanged;
        public event Action<WeaponData> OnSelectedWeaponChanged;
        public event Action<ArmorData> OnSelectedArmorChanged;
        #endregion

        #region 생성자 및 초기화
        public EquipmentViewModel(
            InventoryModel inventoryModel, 
            PlayerModel playerModel,
            UserSessionModel sessionModel,
            EquipmentDatabase database,
            PlayerData playerData)
        {
            if (inventoryModel == null || playerModel == null || sessionModel == null || database == null || playerData == null)
            {
                UnityEngine.Debug.LogError($"[EquipmentViewModel] 필수 의존성이 누락되었습니다! (Inv: {inventoryModel != null}, Player: {playerModel != null}, Session: {sessionModel != null}, DB: {database != null}, Data: {playerData != null})");
                return;
            }

            m_inventoryModel = inventoryModel;
            m_playerModel = playerModel;
            m_sessionModel = sessionModel;
            m_database = database;
            m_playerData = playerData;

            Bind();
            SyncWithSession();
        }

        private void Bind()
        {
            m_inventoryModel.OnInventoryUpdated += () => OnDataUpdated?.Invoke();
            m_inventoryModel.OnSelectedWeaponChanged += (w) => OnSelectedWeaponChanged?.Invoke(w);
            m_inventoryModel.OnSelectedArmorChanged += (a) => OnSelectedArmorChanged?.Invoke(a);
            m_inventoryModel.OnEquipmentChanged += () => OnDataUpdated?.Invoke();
        }

        private void SyncWithSession()
        {
            var dto = m_sessionModel.CurrentEquipment;
            if (dto == null)
            {
                UnityEngine.Debug.LogWarning("[EquipmentViewModel] 세션 데이터가 없습니다.");
                return;
            }

            // 1. 장착 데이터 동기화
            var weapon = m_database.GetWeapon(dto.WeaponId);
            var helmet = m_database.GetArmor(dto.HelmetId);
            var bodyArmor = m_database.GetArmor(dto.BodyArmorId);

            m_inventoryModel.SyncEquipment(weapon, helmet, bodyArmor);

            // 2. 보유 목록 동기화 (상세 로깅을 위해 루프 사용)
            // [필터링]: 기본 무기(DefaultWeapon)는 상자 획득물이 아니므로 인벤토리 리스트에서 제외합니다.
            var ownedWeapons = new List<WeaponData>();
            string defaultWeaponId = m_playerData.DefaultWeapon != null ? m_database.GetWeaponId(m_playerData.DefaultWeapon) : string.Empty;

            UnityEngine.Debug.Log($"[EquipmentViewModel] 무기 로드 시작 (TotalOwned: {dto.OwnedWeaponIds.Count}, Filtered Default: {defaultWeaponId})");
            foreach (var id in dto.OwnedWeaponIds)
            {
                // 기본 무기 ID는 리스트업하지 않음 (사용자 요청: 상자 획득 장비만 표시)
                if (id == defaultWeaponId) continue;

                var data = m_database.GetWeapon(id);
                if (data != null)
                {
                    ownedWeapons.Add(data);
                    UnityEngine.Debug.Log($"[EquipmentViewModel] 무기 소유 확인 (리스트업): {id}");
                }
                else UnityEngine.Debug.LogWarning($"[EquipmentViewModel] 무기 ID 매칭 실패: {id}");
            }
            
            var ownedArmors = new List<ArmorData>();
            UnityEngine.Debug.Log($"[EquipmentViewModel] 갑주 로드 시작 (Count: {dto.OwnedArmorIds.Count})");
            foreach (var id in dto.OwnedArmorIds)
            {
                var data = m_database.GetArmor(id);
                if (data != null)
                {
                    ownedArmors.Add(data);
                    UnityEngine.Debug.Log($"[EquipmentViewModel] 갑주 소유 확인 (리스트업): {id}");
                }
                else UnityEngine.Debug.LogWarning($"[EquipmentViewModel] 갑주 ID 매칭 실패: {id}");
            }

            m_inventoryModel.SyncOwnedItems(ownedWeapons, ownedArmors);
            
            UnityEngine.Debug.Log($"[EquipmentViewModel] 세션 동기화 결과: DTO(무기:{dto.OwnedWeaponIds.Count}, 갑주:{dto.OwnedArmorIds.Count}) -> 최종 리스트업(무기:{ownedWeapons.Count}, 갑주:{ownedArmors.Count})");
        }
        #endregion

        #region 공개 명령 (Commands)
        public void SetCategory(EquipmentCategory category)
        {
            if (CurrentCategory == category) return;
            CurrentCategory = category;
            OnCategoryChanged?.Invoke();
            OnDataUpdated?.Invoke();
        }

        public void SelectWeapon(WeaponData data)
        {
            m_inventoryModel.SelectWeapon(data);
        }

        public void SelectArmor(ArmorData data)
        {
            m_inventoryModel.SelectArmor(data);
        }

        /// <summary>
        /// [설명]: 현재 선택된 아이템을 실제로 장착하고 세션에 저장합니다.
        /// </summary>
        public void SaveEquipmentSelection()
        {
            var dto = m_sessionModel.CurrentEquipment;
            if (dto == null)
            {
                dto = new EquipmentDTO();
            }

            if (m_inventoryModel.SelectedWeapon != null)
            {
                dto.WeaponId = m_database.GetWeaponId(m_inventoryModel.SelectedWeapon);
            }

            if (m_inventoryModel.SelectedArmor != null)
            {
                var armor = m_inventoryModel.SelectedArmor;
                if (armor.Category == ArmorCategory.Helmet)
                {
                    dto.HelmetId = m_database.GetArmorId(armor);
                }
                else
                {
                    dto.BodyArmorId = m_database.GetArmorId(armor);
                }
            }

            m_sessionModel.UpdateEquipment(dto);
            SyncWithSession();

            // 실제 PlayerModel에도 즉시 반영 (인게임 즉시 변화용)
            ApplyToPlayerModel();
        }

        private void ApplyToPlayerModel()
        {
            if (m_inventoryModel.EquippedWeapon != null) m_playerModel.SetWeapon(m_inventoryModel.EquippedWeapon);
            if (m_inventoryModel.EquippedHelmet != null) m_playerModel.SetArmor(m_inventoryModel.EquippedHelmet);
            if (m_inventoryModel.EquippedBodyArmor != null) m_playerModel.SetArmor(m_inventoryModel.EquippedBodyArmor);
        }
        #endregion
    }

    public enum EquipmentCategory
    {
        Weapon,
        Armor
    }
}

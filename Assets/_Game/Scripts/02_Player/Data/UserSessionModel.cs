using System;
using UnityEngine;

namespace TowerBreakers.Player.Data
{
    /// <summary>
    /// [설명]: 씬 전환 시에도 유지되어야 하는 사용자 세션 데이터를 관리하는 클래스입니다.
    /// PlayerPrefs 기반 JSON 직렬화를 통해 데이터를 영속적으로 저장합니다.
    /// </summary>
    public class UserSessionModel
    {
        #region 내부 필드
        private EquipmentDTO m_currentEquipment;
        private const string SAVE_KEY = "UserSession_Equipment";
        #endregion

        #region 프로퍼티
        /// <summary>
        /// [설명]: 현재 사용자가 장착 중인 장비 정보(DTO)입니다.
        /// </summary>
        public EquipmentDTO CurrentEquipment
        {
            get => m_currentEquipment;
            private set
            {
                m_currentEquipment = value;
                OnEquipmentChanged?.Invoke(m_currentEquipment);
            }
        }
        #endregion

        #region 이벤트
        /// <summary>
        /// [설명]: 세션 내 장착 정보가 변경되었을 때 호출됩니다.
        /// </summary>
        public event Action<EquipmentDTO> OnEquipmentChanged;
        #endregion

        #region 생성자
        public UserSessionModel()
        {
            // [설명]: 저장된 데이터가 있으면 로드, 없으면 빈 DTO로 초기화
            Load();
        }
        #endregion

        #region 공개 메서드
        /// <summary>
        /// [설명]: 새로운 장비 DTO 데이터를 세션에 저장합니다.
        /// </summary>
        public void UpdateEquipment(EquipmentDTO dto)
        {
            if (dto == null) return;
            CurrentEquipment = dto;
            Save();
        }

        /// <summary>
        /// [설명]: 개별 부위별 ID를 사용하여 세션 데이터를 갱신합니다.
        /// </summary>
        public void SetEquipmentIds(string weaponId, string helmetId, string bodyArmorId)
        {
            var newDto = new EquipmentDTO(weaponId, helmetId, bodyArmorId);
            // [설명]: 기존 보유 목록을 유지
            if (m_currentEquipment != null)
            {
                newDto.OwnedWeaponIds = m_currentEquipment.OwnedWeaponIds;
                newDto.OwnedArmorIds = m_currentEquipment.OwnedArmorIds;
            }
            UpdateEquipment(newDto);
        }

        /// <summary>
        /// [설명]: 보유 무기 ID를 추가하고 즉시 저장합니다.
        /// </summary>
        /// <param name="weaponId">추가할 무기의 고유 ID</param>
        public void AddOwnedWeapon(string weaponId)
        {
            if (string.IsNullOrEmpty(weaponId)) return;
            if (m_currentEquipment == null) m_currentEquipment = new EquipmentDTO();

            if (!m_currentEquipment.OwnedWeaponIds.Contains(weaponId))
            {
                m_currentEquipment.OwnedWeaponIds.Add(weaponId);
                Debug.Log($"[TRACE] AddOwnedWeapon: {weaponId} 추가됨. 현재 목록: {m_currentEquipment.OwnedWeaponIds.Count}개");
                Save();
                Debug.Log($"<color=green>[UserSession] 신규 무기 획득 및 영속화: {weaponId}</color>");
                Debug.Log($"[TRACE] AddOwnedWeapon: Save() 완료. 직렬화 데이터: {JsonUtility.ToJson(m_currentEquipment)}");
            }
            else
            {
                Debug.Log($"[TRACE] AddOwnedWeapon: {weaponId}은(는) 이미 보유 중. 목록 크기: {m_currentEquipment.OwnedWeaponIds.Count}");
            }
        }

        /// <summary>
        /// [설명]: 보유 갑주 ID를 추가하고 즉시 저장합니다.
        /// </summary>
        /// <param name="armorId">추가할 갑주의 고유 ID</param>
        public void AddOwnedArmor(string armorId)
        {
            if (string.IsNullOrEmpty(armorId)) return;
            if (m_currentEquipment == null) m_currentEquipment = new EquipmentDTO();

            if (!m_currentEquipment.OwnedArmorIds.Contains(armorId))
            {
                m_currentEquipment.OwnedArmorIds.Add(armorId);
                Debug.Log($"[TRACE] AddOwnedArmor: {armorId} 추가됨. 현재 목록: {m_currentEquipment.OwnedArmorIds.Count}개");
                Save();
                Debug.Log($"<color=green>[UserSession] 신규 갑주 획득 및 영속화: {armorId}</color>");
                Debug.Log($"[TRACE] AddOwnedArmor: Save() 완료. 직렬화 데이터: {JsonUtility.ToJson(m_currentEquipment)}");
            }
            else
            {
                Debug.Log($"[TRACE] AddOwnedArmor: {armorId}은(는) 이미 보유 중. 목록 크기: {m_currentEquipment.OwnedArmorIds.Count}");
            }
        }

        /// <summary>
        /// [설명]: 보유 중인 무기 목록만 초기화합니다.
        /// </summary>
        public void ClearOwnedWeapons()
        {
            if (m_currentEquipment == null) return;
            m_currentEquipment.OwnedWeaponIds.Clear();
            Save();
            Debug.Log("<color=yellow>[UserSession] 보유 무기 목록 초기화 완료</color>");
        }

        /// <summary>
        /// [설명]: 보유 중인 갑주 목록만 초기화합니다.
        /// </summary>
        public void ClearOwnedArmors()
        {
            if (m_currentEquipment == null) return;
            m_currentEquipment.OwnedArmorIds.Clear();
            Save();
            Debug.Log("<color=yellow>[UserSession] 보유 갑주 목록 초기화 완료</color>");
        }

        /// <summary>
        /// [설명]: 현재 상태를 순수 DTO로 추출합니다. 외부에서 데이터를 읽을 때 사용합니다.
        /// </summary>
        /// <returns>현재 장비 데이터의 복사본</returns>
        public EquipmentDTO ExportDTO()
        {
            if (m_currentEquipment == null)
            {
                return new EquipmentDTO();
            }
            return m_currentEquipment.Clone();
        }

        /// <summary>
        /// [설명]: 외부 DTO로부터 상태를 복원하고 영속화합니다. 씬 전환 시 데이터 동기화에 사용됩니다.
        /// </summary>
        /// <param name="dto">복원할 장비 데이터</param>
        public void ImportDTO(EquipmentDTO dto)
        {
            if (dto == null)
            {
                Debug.LogWarning("[UserSession] ImportDTO: 전달된 DTO가 null입니다. 작업을 건너뜁니다.");
                return;
            }

            Debug.Log($"[UserSession] ImportDTO: 무기 {dto.OwnedWeaponIds.Count}개, 갑주 {dto.OwnedArmorIds.Count}개");
            CurrentEquipment = dto.Clone();
            Save();
        }
        #endregion

        #region 영속화 로직
        /// <summary>
        /// [설명]: 현재 세션 데이터를 PlayerPrefs에 JSON으로 저장합니다.
        /// </summary>
        public void Save()
        {
            if (m_currentEquipment == null) return;

            string json = JsonUtility.ToJson(m_currentEquipment);
            PlayerPrefs.SetString(SAVE_KEY, json);
            PlayerPrefs.Save();
            Debug.Log($"[UserSession] 세션 데이터 저장 완료: {json}");
        }

        /// <summary>
        /// [설명]: PlayerPrefs에서 세션 데이터를 복원합니다.
        /// </summary>
        public void Load()
        {
            if (PlayerPrefs.HasKey(SAVE_KEY))
            {
                string json = PlayerPrefs.GetString(SAVE_KEY);
                m_currentEquipment = JsonUtility.FromJson<EquipmentDTO>(json);
                Debug.Log($"[UserSession] 세션 데이터 로드 완료: {json}");
            }
            else
            {
                m_currentEquipment = new EquipmentDTO();
                Debug.Log("[UserSession] 저장된 세션 데이터 없음, 새 DTO 생성");
            }
        }

        /// <summary>
        /// [설명]: 저장된 세션 데이터를 초기화합니다. (디버그/리셋용)
        /// </summary>
        public void ClearSave()
        {
            PlayerPrefs.DeleteKey(SAVE_KEY);
            m_currentEquipment = new EquipmentDTO();
            Debug.Log("[UserSession] 세션 데이터 초기화 완료");
        }
        #endregion
    }
}

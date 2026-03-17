using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using TowerBreakers.Player.Data;
using TowerBreakers.Player.Model;
using TowerBreakers.Player.Service;

namespace TowerBreakers.SPUM
{
    /// <summary>
    /// [기능]: SPUM 캐릭터 외형 관리자
    /// </summary>
    public class SPUM_CharacterManager : MonoBehaviour
    {
        [Header("SPUM 참조")]
        [Tooltip("SPUM 매니저")]
        [SerializeField] private SPUM_Manager m_spumManager;

        [Tooltip("SPUM 프리팹")]
        [SerializeField] private SPUM_Prefabs m_spumPrefabs;

        [Header("설정")]
        [Tooltip("기본 유닛 타입")]
        [SerializeField] private string m_unitType = "Player";

        private UserSessionModel m_userSession;
        private IEquipmentService m_equipmentService;
        private List<PreviewMatchingElement> m_currentMatchingElements = new List<PreviewMatchingElement>();

        public void Initialize(UserSessionModel userSession, IEquipmentService equipmentService)
        {
            m_userSession = userSession;
            m_equipmentService = equipmentService;

            FindSPUMManager();
            SubscribeEvents();

            UpdateAllEquipment();
        }

        private void FindSPUMManager()
        {
            if (m_spumManager == null && m_spumPrefabs != null)
            {
                m_spumManager = m_spumPrefabs.GetComponent<SPUM_Manager>();
            }

            if (m_spumManager == null)
            {
                m_spumManager = FindObjectOfType<SPUM_Manager>();
            }

            if (m_spumManager == null)
            {
                Debug.LogWarning("[SPUM_CharacterManager] SPUM_Manager를 찾을 수 없습니다.");
            }
        }

        private void SubscribeEvents()
        {
            if (m_userSession != null)
            {
                m_userSession.OnEquipmentChanged += OnEquipmentChanged;
            }
        }

        private void UnsubscribeEvents()
        {
            if (m_userSession != null)
            {
                m_userSession.OnEquipmentChanged -= OnEquipmentChanged;
            }
        }

        private void OnEquipmentChanged(EquipmentType type, string itemId)
        {
            UpdateEquipmentAppearance(type, itemId);
        }

        private void UpdateAllEquipment()
        {
            m_currentMatchingElements.Clear();

            foreach (EquipmentType type in System.Enum.GetValues(typeof(EquipmentType)))
            {
                var itemId = m_userSession.GetEquippedId(type);
                var element = CreateMatchingElement(type, itemId);
                if (element != null)
                {
                    m_currentMatchingElements.Add(element);
                }
            }

            ApplyMatchingElements();
        }

        public void UpdateEquipmentAppearance(EquipmentType type, string itemId)
        {
            // 기존 타입의 매칭 엘리먼트 제거
            m_currentMatchingElements.RemoveAll(e => IsMatchingType(e, type));

            // 신규 아이템 추가
            if (!string.IsNullOrEmpty(itemId))
            {
                var element = CreateMatchingElement(type, itemId);
                if (element != null)
                {
                    m_currentMatchingElements.Add(element);
                }
            }

            ApplyMatchingElements();

            Debug.Log($"[SPUM_CharacterManager] 외형 업데이트 - {type}: {itemId}");
        }

        private bool IsMatchingType(PreviewMatchingElement element, EquipmentType type)
        {
            // Weapon 타입은 SpumStructure로 세부 구분 가능하나, 여기서는 단순 타입 비교
            // 실무에서는 element.PartSubType 등에 타입 정보를 저장하여 정확히 필터링
            return element.PartSubType == type.ToString();
        }

        private PreviewMatchingElement CreateMatchingElement(EquipmentType type, string itemId)
        {
            if (string.IsNullOrEmpty(itemId))
            {
                return null;
            }

            var equipmentData = m_equipmentService.GetEquipmentData(itemId);
            if (equipmentData == null)
            {
                Debug.LogWarning($"[SPUM_CharacterManager] 장비 데이터를 찾을 수 없습니다: {itemId}");
                return null;
            }

            var element = new PreviewMatchingElement
            {
                UnitType = m_unitType,
                PartType = GetPartType(type),
                PartSubType = type.ToString(),
                Dir = equipmentData.SpumDir, // "Right", "Left" 등 에셋 데이터 기반
                Structure = equipmentData.SpumStructure, // "0_Sword", "6_Shield" 등 에셋 데이터 기반
                ItemPath = equipmentData.SpumItemPath,
                Index = 0,
                MaskIndex = 0,
                Color = Color.white
            };

            return element;
        }

        private string GetPartType(EquipmentType type)
        {
            switch (type)
            {
                case EquipmentType.Weapon:
                    return "Weapons";
                case EquipmentType.Armor:
                    return "Cloth"; // 기본적으로 상의(Cloth) 매칭
                case EquipmentType.Helmet:
                    return "Helmet";
                default:
                    return "Weapons";
            }
        }

        private void ApplyMatchingElements()
        {
            if (m_spumManager == null)
            {
                Debug.LogWarning("[SPUM_CharacterManager] SPUM_Manager가 null입니다.");
                return;
            }

            m_spumManager.SetSprite(m_currentMatchingElements);
        }

        public void UpdateSpumAppearance(EquipmentData data)
        {
            if (data == null)
            {
                return;
            }

            UpdateEquipmentAppearance(data.Type, data.ID);
        }

        private void OnDestroy()
        {
            UnsubscribeEvents();
        }
    }
}

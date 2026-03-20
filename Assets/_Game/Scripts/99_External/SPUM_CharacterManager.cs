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
        [Tooltip("SPUM 프리팹 (필수)")]
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

            if (m_spumPrefabs == null) m_spumPrefabs = GetComponent<SPUM_Prefabs>();
            if (m_spumPrefabs == null) m_spumPrefabs = GetComponentInChildren<SPUM_Prefabs>();

            if (m_spumPrefabs != null)
            {
                Debug.Log($"[SPUM_CharacterManager] 프리팹 연결 성공: {m_spumPrefabs.gameObject.name}");
            }
            else
            {
                Debug.LogError("[SPUM_CharacterManager] SPUM_Prefabs를 찾을 수 없습니다!");
            }

            SubscribeEvents();
            UpdateAllEquipment();
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
                if (!string.IsNullOrEmpty(itemId))
                {
                    var equipmentData = m_equipmentService.GetEquipmentData(itemId);
                    if (equipmentData != null)
                    {
                        foreach (var part in equipmentData.SpumParts)
                        {
                            var element = CreateMatchingElement(type, part);
                            if (element != null) m_currentMatchingElements.Add(element);
                        }
                    }
                }
            }

            ApplyMatchingElements();
        }

        public void UpdateEquipmentAppearance(EquipmentType type, string itemId)
        {
            // [강제 로그]: 이 로그가 안 찍힌다면 호출 자체가 안 된 것
            Debug.Log($"<color=yellow>[SPUM_CharacterManager] UpdateAppearance 호출됨 - Type: {type}, ID: {itemId}</color>");
            
            // 기존 해당 부위 제거
            m_currentMatchingElements.RemoveAll(e => e.PartSubType == type.ToString());

            if (!string.IsNullOrEmpty(itemId) && m_equipmentService != null)
            {
                var equipmentData = m_equipmentService.GetEquipmentData(itemId);
                if (equipmentData != null)
                {
                    Debug.Log($"[SPUM_CharacterManager] 에셋 로드 성공: {equipmentData.ItemName} (Parts: {equipmentData.SpumParts.Count})");
                    foreach (var part in equipmentData.SpumParts)
                    {
                        var element = CreateMatchingElement(type, part);
                        if (element != null) 
                        {
                            m_currentMatchingElements.Add(element);
                            Debug.Log($"[SPUM_CharacterManager] 부위 등록: {part.Structure}");
                        }
                    }
                }
            }

            ApplyMatchingElements();
        }

        private bool IsMatchingType(PreviewMatchingElement element, EquipmentType type)
        {
            // Weapon 타입은 SpumStructure로 세부 구분 가능하나, 여기서는 단순 타입 비교
            // 실무에서는 element.PartSubType 등에 타입 정보를 저장하여 정확히 필터링
            return element.PartSubType == type.ToString();
        }

        private PreviewMatchingElement CreateMatchingElement(EquipmentType type, EquipmentData.SpumPartInfo part)
        {
            if (part == null) return null;
            
            return new PreviewMatchingElement
            {
                UnitType = m_unitType,
                PartType = GetPartType(type),
                PartSubType = GetPartType(type), // SPUM 내부 매칭용
                Dir = "Right",
                Structure = part.Structure,
                ItemPath = part.SpritePath,
                Index = 0,
                MaskIndex = 0,
                Color = Color.white
            };
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
            if (m_spumPrefabs == null) return;

            // 1. 프리팹 내부 데이터 갱신
            m_spumPrefabs.ImageElement.Clear();
            m_spumPrefabs.ImageElement.AddRange(m_currentMatchingElements);

            // 2. 하위 MatchingList들을 찾아 실제 렌더러에 스프라이트 적용
            var matchingTables = m_spumPrefabs.GetComponentsInChildren<SPUM_MatchingList>(true);
            var allTargetElements = matchingTables.SelectMany(mt => mt.matchingTables).ToList();

            foreach (var target in allTargetElements)
            {
                // [개선]: UnitType과 PartSubType 조건을 제외하고 Structure와 PartType으로만 매칭 (더 유연함)
                var match = m_currentMatchingElements.FirstOrDefault(ie => 
                    ie.PartType == target.PartType && 
                    ie.Structure == target.Structure);

                if (match != null)
                {
                    var sprite = LoadSpriteFromPath(match.ItemPath, match.Structure);
                    if (sprite != null)
                    {
                        target.renderer.sprite = sprite;
                        target.renderer.color = match.Color;
                        target.renderer.gameObject.SetActive(true);
                    }
                }
            }

            Debug.Log($"[SPUM_CharacterManager] 외형 적용 완료 (매칭된 엘리먼트: {m_currentMatchingElements.Count})");
        }

        private Sprite LoadSpriteFromPath(string path, string spriteName)
        {
            if (string.IsNullOrEmpty(path)) return null;

            // [핵심 수정]: 'Assets/Resources/' 문자열을 제거하고 확장자를 없애야 Resources.Load가 작동함
            string resourcePath = path;
            int resIndex = path.IndexOf("Resources/");
            if (resIndex != -1)
            {
                resourcePath = path.Substring(resIndex + 10); // "Resources/" 다음부터 시작
            }
            resourcePath = resourcePath.Replace(".png", "").Replace(".jpg", "");

            var sprites = Resources.LoadAll<Sprite>(resourcePath);
            if (sprites == null || sprites.Length == 0) 
            {
                Debug.LogWarning($"[SPUM_CharacterManager] 리소스를 찾을 수 없음: {resourcePath}");
                return null;
            }

            // 이름이 일치하는 스프라이트 탐색, 없으면 첫 번째 반환
            return System.Array.Find(sprites, s => s.name == spriteName) ?? sprites[0];
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

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
                // 프리팹 내부 구조 진단 로그
                DiagnosePrefabStructure();
            }
            else
            {
                Debug.LogError("[SPUM_CharacterManager] SPUM_Prefabs를 찾을 수 없습니다!");
            }

            UpdateAllEquipment();
        }

        private void DiagnosePrefabStructure()
        {
            var matchingTables = m_spumPrefabs.GetComponentsInChildren<SPUM_MatchingList>(true);
            Debug.Log($"<color=white>[진단] 프리팹 내 MatchingList 개수: {matchingTables.Length}</color>");
            
            foreach (var mt in matchingTables)
            {
                foreach (var target in mt.matchingTables)
                {
                    if (target.renderer != null)
                    {
                        Debug.Log($"[진단] 사용 가능한 렌더러: PartType={target.PartType}, Structure={target.Structure}, Name={target.renderer.name}");
                    }
                }
            }
        }

        public void UpdateAllEquipment()
        {
            if (m_userSession == null || m_equipmentService == null) return;

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
            Debug.Log($"<color=cyan>[SPUM_CharacterManager] 외형 업데이트 시작 - 부위: {type}, ID: {itemId}</color>");
            
            m_currentMatchingElements.RemoveAll(e => e.PartSubType == type.ToString());

            if (!string.IsNullOrEmpty(itemId) && m_equipmentService != null)
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

            ApplyMatchingElements();
        }

        private PreviewMatchingElement CreateMatchingElement(EquipmentType type, EquipmentData.SpumPartInfo part)
        {
            if (part == null) return null;
            
            return new PreviewMatchingElement
            {
                UnitType = m_unitType,
                PartType = GetPartType(type),
                PartSubType = type.ToString(),
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
                case EquipmentType.Weapon: return "Weapons";
                case EquipmentType.Armor: return "Cloth";
                case EquipmentType.Helmet: return "Helmet";
                default: return "Weapons";
            }
        }

        private void ApplyMatchingElements()
        {
            if (m_spumPrefabs == null) return;

            m_spumPrefabs.ImageElement.Clear();
            m_spumPrefabs.ImageElement.AddRange(m_currentMatchingElements);

            var matchingTables = m_spumPrefabs.GetComponentsInChildren<SPUM_MatchingList>(true);
            int matchedCount = 0;

            // 데이터 정보 로그
            foreach(var data in m_currentMatchingElements)
            {
                Debug.Log($"<color=orange>[매칭시도] 데이터: PartType={data.PartType}, Structure={data.Structure}, Path={data.ItemPath}</color>");
            }
            
            foreach (var mt in matchingTables)
            {
                foreach (var target in mt.matchingTables)
                {
                    if (target.renderer == null) continue;

                    string rendererName = target.renderer.name;

                    // 정밀 매칭 검사
                    bool isMatch = false;
                    foreach (var ie in m_currentMatchingElements)
                    {
                        // 1. 방패 여부 확인 (Structure나 Path에 Shield 포함 여부)
                        bool isShieldItem = ie.Structure.Contains("Shield") || ie.ItemPath.Contains("Shield");

                        // 2. 카테고리 매칭 (Type)
                        bool typeMatch = (ie.PartType == target.PartType || 
                                          target.PartType.Contains(ie.PartType) || 
                                          ie.PartType.Contains(target.PartType));
                        
                        // Armor 특수 케이스: Cloth와 Armor 카테고리 상호 호환
                        if (!typeMatch)
                        {
                            if ((ie.PartType == "Cloth" || ie.PartType == "Armor") && 
                                (target.PartType == "Cloth" || target.PartType == "Armor"))
                            {
                                typeMatch = true;
                            }
                        }

                        if (!typeMatch) continue;

                        // [슬롯 제한 로직]
                        if (target.PartType.Contains("Weapon"))
                        {
                            if (isShieldItem)
                            {
                                // 방패는 R_Shield 슬롯에만 장착
                                if (rendererName != "R_Shield") continue;
                            }
                            else
                            {
                                // 무기는 L_Weapon 슬롯에만 장착 (사용자 요청: "오른쪽 L_Weapon")
                                if (rendererName != "L_Weapon") continue;
                            }
                        }

                        // 3. 구조 매칭 (Structure)
                        bool structMatch = false;

                        // 방패의 경우 슬롯 이름 매칭으로 갈음
                        if (isShieldItem && rendererName == "R_Shield")
                        {
                            structMatch = true;
                        }
                        // 완전 일치 또는 상호 포함 (예: "4_Helmet" <-> "Helmet")
                        else if (ie.Structure == target.Structure || 
                            ie.Structure.Contains(target.Structure) || 
                            target.Structure.Contains(ie.Structure))
                        {
                            structMatch = true;
                        }
                        // 특정 카테고리의 경우 기본 매칭 허용 (단일 슬롯인 경우가 많음)
                        else if (target.PartType == "Helmet" || target.PartType.Contains("Weapon"))
                        {
                            structMatch = true;
                        }
                        // 접미사 및 방향성 매칭 (예: Armor_L -> Left, Shoulder_R -> Right)
                        else if (target.Structure == "Left" && (ie.Structure.EndsWith("_L") || ie.Structure.ToLower().Contains("left")))
                        {
                            structMatch = true;
                        }
                        else if (target.Structure == "Right" && (ie.Structure.EndsWith("_R") || ie.Structure.ToLower().Contains("right")))
                        {
                            structMatch = true;
                        }
                        // 몸통 매칭 (예: 7_Armor -> Body)
                        else if (target.Structure == "Body" && (ie.Structure.Contains("Armor") || ie.Structure.Contains("Cloth")))
                        {
                            structMatch = true;
                        }

                        if (structMatch)
                        {
                            var sprite = LoadSpriteFromPath(ie.ItemPath, ie.Structure);
                            if (sprite != null)
                            {
                                target.renderer.sprite = sprite;
                                target.renderer.color = ie.Color;
                                target.renderer.gameObject.SetActive(true);
                                matchedCount++;
                                isMatch = true;
                                Debug.Log($"<color=lime>[성공] 매칭됨! 프리팹({target.PartType}/{target.Structure}/{rendererName}) <-> 데이터({ie.PartType}/{ie.Structure})</color>");
                                break;
                            }
                            else
                            {
                                Debug.LogError($"[실패] 리소스 로드 실패: {ie.ItemPath} (Structure: {ie.Structure})");
                            }
                        }
                    }

                    // 옷(Cloth/Armor)은 매칭되지 않더라도 비활성화하지 않고 기본 외형 유지
                    if (!isMatch && IsEquipmentPart(target.PartType))
                    {
                        if (target.PartType.Contains("Cloth") || target.PartType.Contains("Armor"))
                        {
                            // 유지
                        }
                        else
                        {
                            target.renderer.gameObject.SetActive(false);
                        }
                    }
                }
            }

            Debug.Log($"<color=yellow>[결과] 데이터 개수: {m_currentMatchingElements.Count}, 실제 렌더러 적용 성공: {matchedCount}</color>");
        }

        private bool IsEquipmentPart(string partType)
        {
            return partType.Contains("Weapon") || partType.Contains("Helmet") || partType.Contains("Shield");
        }

        private Sprite LoadSpriteFromPath(string path, string spriteName)
        {
            if (string.IsNullOrEmpty(path)) return null;

            string resourcePath = path.Replace("\\", "/");
            int resIndex = resourcePath.IndexOf("Resources/");
            if (resIndex != -1) resourcePath = resourcePath.Substring(resIndex + 10);
            
            int dotIndex = resourcePath.LastIndexOf(".");
            string fileName = "";
            if (dotIndex != -1)
            {
                int slashIndex = resourcePath.LastIndexOf("/");
                fileName = resourcePath.Substring(slashIndex + 1, dotIndex - slashIndex - 1);
                resourcePath = resourcePath.Substring(0, dotIndex);
            }

            var sprites = Resources.LoadAll<Sprite>(resourcePath);
            if (sprites == null || sprites.Length == 0) return null;

            // 1. 구조 이름으로 찾기 (예: 4_Helmet)
            var result = System.Array.Find(sprites, s => s.name == spriteName);
            // 2. 파일 이름으로 찾기 (예: Helmet_6)
            if (result == null) result = System.Array.Find(sprites, s => s.name == fileName);
            // 3. 첫 번째 스프라이트 사용
            if (result == null) result = sprites[0];

            return result;
        }

        public void UpdateSpumAppearance(EquipmentData data)
        {
            if (data == null) return;
            UpdateEquipmentAppearance(data.Type, data.ID);
        }
    }
}

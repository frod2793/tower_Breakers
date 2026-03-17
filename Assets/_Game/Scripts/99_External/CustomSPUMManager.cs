using System.Collections.Generic;
using UnityEngine;
using TowerBreakers.Player.Data;
using TowerBreakers.Player.Model;
using TowerBreakers.Player.Service;

namespace TowerBreakers.SPUM
{
    /// <summary>
    /// [기능]: SPUM 캐릭터 외형 관리자 (SPUM_Manager 없이 독립 작동)
    /// </summary>
    public class CustomSPUMManager : MonoBehaviour
    {
        [Header("SPUM 참조")]
        [Tooltip("SPUM 프리팹")]
        [SerializeField] private SPUM_Prefabs m_spumPrefabs;

        [Header("스프라이트 렌더러")]
        [Tooltip("무기 렌더러 리스트")]
        [SerializeField] private List<SpriteRenderer> m_weaponRenderers = new List<SpriteRenderer>();

        [Tooltip("방어구 렌더러 리스트")]
        [SerializeField] private List<SpriteRenderer> m_armorRenderers = new List<SpriteRenderer>();

        [Tooltip("헤어/투구 렌더러 리스트")]
        [SerializeField] private List<SpriteRenderer> m_hairRenderers = new List<SpriteRenderer>();

        [Header("설정")]
        [Tooltip("스프라이트 기본 경로")]
        [SerializeField] private string m_spriteBasePath = "SPUM/Sprites";

        private Dictionary<EquipmentType, List<SpriteRenderer>> m_equipmentRenderers;
        private Dictionary<string, Sprite> m_spriteCache = new Dictionary<string, Sprite>();

        private UserSessionModel m_userSession;
        private IEquipmentService m_equipmentService;

        public void Initialize(UserSessionModel userSession, IEquipmentService equipmentService)
        {
            m_userSession = userSession;
            m_equipmentService = equipmentService;

            SetupRendererReferences();
            SubscribeEvents();

            if (m_spumPrefabs != null)
            {
                m_spumPrefabs.PopulateAnimationLists();
                m_spumPrefabs.OverrideControllerInit();
            }

            Debug.Log("[CustomSPUMManager] 초기화 완료");
        }

        private void SetupRendererReferences()
        {
            m_equipmentRenderers = new Dictionary<EquipmentType, List<SpriteRenderer>>();

            m_equipmentRenderers[EquipmentType.Weapon] = m_weaponRenderers;
            m_equipmentRenderers[EquipmentType.Armor] = m_armorRenderers;
            m_equipmentRenderers[EquipmentType.Helmet] = m_hairRenderers;

            if (m_spumPrefabs != null)
            {
                var allRenderers = m_spumPrefabs.GetComponentsInChildren<SpriteRenderer>();
                
                if (m_weaponRenderers.Count == 0)
                {
                    m_weaponRenderers = FindRenderersByKeyword(allRenderers, "Weapon");
                    m_equipmentRenderers[EquipmentType.Weapon] = m_weaponRenderers;
                }
                
                if (m_armorRenderers.Count == 0)
                {
                    m_armorRenderers = FindRenderersByKeyword(allRenderers, "Armor");
                    m_equipmentRenderers[EquipmentType.Armor] = m_armorRenderers;
                }
                
                if (m_hairRenderers.Count == 0)
                {
                    m_hairRenderers = FindRenderersByKeyword(allRenderers, "Hair");
                    m_equipmentRenderers[EquipmentType.Helmet] = m_hairRenderers;
                }
            }
        }

        private List<SpriteRenderer> FindRenderersByKeyword(SpriteRenderer[] allRenderers, string keyword)
        {
            var result = new List<SpriteRenderer>();
            foreach (var sr in allRenderers)
            {
                if (sr.name.Contains(keyword, System.StringComparison.OrdinalIgnoreCase))
                {
                    result.Add(sr);
                }
            }
            return result;
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
            foreach (EquipmentType type in System.Enum.GetValues(typeof(EquipmentType)))
            {
                var itemId = m_userSession.GetEquippedId(type);
                UpdateEquipmentAppearance(type, itemId);
            }
        }

        public void UpdateEquipmentAppearance(EquipmentType type, string itemId)
        {
            if (!m_equipmentRenderers.TryGetValue(type, out var renderers))
            {
                Debug.LogWarning($"[CustomSPUMManager] 장비 타입 {type}의 렌더러가 없습니다.");
                return;
            }

            if (renderers == null || renderers.Count == 0)
            {
                return;
            }

            if (string.IsNullOrEmpty(itemId))
            {
                ClearEquipmentSprite(type, renderers);
                return;
            }

            var equipmentData = m_equipmentService.GetEquipmentData(itemId);
            if (equipmentData == null)
            {
                Debug.LogWarning($"[CustomSPUMManager] 장비 데이터를 찾을 수 없습니다: {itemId}");
                return;
            }

            SetEquipmentSprite(equipmentData, renderers);

            Debug.Log($"[CustomSPUMManager] 외형 업데이트 - {type}: {equipmentData.ItemName}");
        }

        private void SetEquipmentSprite(EquipmentData data, List<SpriteRenderer> renderers)
        {
            var spriteId = data.SpumSpriteId;
            if (string.IsNullOrEmpty(spriteId))
            {
                ClearEquipmentSprite(data.Type, renderers);
                return;
            }

            var sprite = LoadSprite(spriteId);
            if (sprite == null)
            {
                Debug.LogWarning($"[CustomSPUMManager] 스프라이트를 로드할 수 없습니다: {spriteId}");
                ClearEquipmentSprite(data.Type, renderers);
                return;
            }

            for (int i = 0; i < renderers.Count; i++)
            {
                if (renderers[i] != null)
                {
                    renderers[i].sprite = sprite;
                    renderers[i].gameObject.SetActive(true);
                }
            }
        }

        private void ClearEquipmentSprite(EquipmentType type, List<SpriteRenderer> renderers)
        {
            if (renderers == null)
            {
                return;
            }

            for (int i = 0; i < renderers.Count; i++)
            {
                if (renderers[i] != null)
                {
                    renderers[i].sprite = null;
                    renderers[i].gameObject.SetActive(false);
                }
            }
        }

        private Sprite LoadSprite(string spriteId)
        {
            if (string.IsNullOrEmpty(spriteId))
            {
                return null;
            }

            if (m_spriteCache.TryGetValue(spriteId, out var cachedSprite))
            {
                return cachedSprite;
            }

            var sprite = Resources.Load<Sprite>($"{m_spriteBasePath}/{spriteId}");
            if (sprite == null)
            {
                sprite = Resources.Load<Sprite>($"Items/{spriteId}");
            }

            if (sprite != null)
            {
                m_spriteCache[spriteId] = sprite;
            }

            return sprite;
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
            m_spriteCache.Clear();
        }
    }
}

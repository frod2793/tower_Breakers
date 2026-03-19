using UnityEngine;

namespace TowerBreakers.Player.Data
{
    /// <summary>
    /// [기능]: 장비 데이터 ScriptableObject
    /// </summary>
    [CreateAssetMenu(fileName = "Equipment_", menuName = "Data/Item/Equipment")]
    public class EquipmentData : ScriptableObject
    {
        [Header("기본 정보")]
        [Tooltip("장비 고유 ID")]
        [SerializeField] private string m_id;

        [Tooltip("장비 표시 이름")]
        [SerializeField] private string m_itemName;

        [Tooltip("장비 아이콘 (상자 개봉 연출 등에 사용)")]
        [SerializeField] private Sprite m_icon;

        [Tooltip("장비 설명")]
        [SerializeField] [TextArea] private string m_description;

        [Tooltip("장비 타입")]
        [SerializeField] private EquipmentType m_type;

        [Tooltip("장비 등급 (일반, 레어, 전설)")]
        [SerializeField] private int m_grade;

        [Header("스탯")]
        [Tooltip("장비 스탯 변조값")]
        [SerializeField] private StatModifiers m_stats;

        [Header("SPUM 연동")]
        [Tooltip("SPUM 스프라이트 시트 식별자")]
        [SerializeField] private string m_spumSpriteId;

        [Tooltip("SPUM 방향 (Right/Left)")]
        [SerializeField] private string m_spumDir = "Right";

        [Tooltip("SPUM 무기 구조 (0_Sword, 1_Axe, 2_Bow, 6_Shield 등)")]
        [SerializeField] private string m_spumStructure;

        [Tooltip("SPUM 아이템 리소스 경로")]
        [SerializeField] private string m_spumItemPath;

        public string ID => m_id;
        public Sprite Icon => m_icon;
        public string ItemName => m_itemName;
        public string Description => m_description;
        public EquipmentType Type => m_type;
        public int Grade => m_grade;
        public StatModifiers Stats => m_stats;
        public string SpumSpriteId => m_spumSpriteId;
        public string SpumDir => m_spumDir;
        public string SpumStructure => m_spumStructure;
        public string SpumItemPath => m_spumItemPath;

        private void OnValidate()
        {
            if (string.IsNullOrEmpty(m_id))
            {
                m_id = name;
            }
        }
    }
}

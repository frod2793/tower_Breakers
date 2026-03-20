using System.Collections.Generic;
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

        [Tooltip("무기 타입 (Weapon인 경우에만 유효)")]
        [SerializeField] private WeaponType m_weaponType;

        [Tooltip("장비 등급 (일반, 레어, 전설)")]
        [SerializeField] private int m_grade;

        [Header("스탯")]
        [Tooltip("장비 스탯 변조값")]
        [SerializeField] private StatModifiers m_stats;

        [Header("SPUM 연동")]
        [SerializeField] private List<SpumPartInfo> m_spumParts = new List<SpumPartInfo>();

        public string ID => m_id;
        public Sprite Icon => m_icon;
        public string ItemName => m_itemName;
        public string Description => m_description;
        public EquipmentType Type => m_type;
        public WeaponType WeaponType => m_weaponType;
        public int Grade => m_grade;
        public StatModifiers Stats => m_stats;
        public IReadOnlyList<SpumPartInfo> SpumParts => m_spumParts;

        [System.Serializable]
        public class SpumPartInfo
        {
            public string Structure; // 예: 7_Armor, 8_Shoulder_L
            public string SpritePath; // 리소스 경로
            public Sprite Sprite;     // [추가]: 런타임 직접 참조용 스프라이트
        }

        private void OnValidate()
        {
            if (string.IsNullOrEmpty(m_id))
            {
                m_id = name;
            }
        }
    }
}

using UnityEngine;

namespace TowerBreakers.Player.Data.SO
{
    public enum ArmorType
    {
        Light,
        Medium,
        Heavy
    }

    public enum ArmorCategory
    {
        Helmet,
        BodyArmor
    }

    /// <summary>
    /// [설명]: 갑주의 외형 스프라이트와 관련 애니메이션, 능력치 보정 데이터를 저장하는 ScriptableObject입니다.
    /// SPUM의 _armorList(Body, Left, Right)를 교체하는 데 사용됩니다.
    /// </summary>
    [CreateAssetMenu(fileName = "NewArmorData", menuName = "TowerBreakers/Armor Data")]
    public class ArmorData : ScriptableObject
    {
        #region 에디터 설정
        [Header("갑주 정보")]
        [SerializeField] private string m_armorName;
        [SerializeField] private ArmorCategory m_category;
        [SerializeField] private ArmorType m_type;

        [Header("외형 설정")]
        [SerializeField, Tooltip("흉갑 스프라이트 (SPUM _armorList[0])")]
        private Sprite m_bodyArmorSprite;

        [SerializeField, Tooltip("왼쪽 어깨/팔 갑주 스프라이트 (SPUM _armorList[1])")]
        private Sprite m_leftShoulderSprite;

        [SerializeField, Tooltip("오른쪽 어깨/팔 갑주 스프라이트 (SPUM _armorList[2])")]
        private Sprite m_rightShoulderSprite;

        [SerializeField, Tooltip("헬멧 스프라이트 (SPUM _hairList[3])")]
        private Sprite m_helmetSprite;

        [SerializeField, Tooltip("UI 및 상자 연출에 사용할 아이콘 스프라이트")]
        private Sprite m_icon;


        [Header("능력치 보정")]
        [SerializeField, Tooltip("추가 생명력 (하트 칸 수)")]
        private int m_lifeBonus = 0;

        [SerializeField, Tooltip("획득 시 회복량 (하트 칸 수)")]
        private int m_healAmount = 0;

        [SerializeField, Range(0f, 1f), Tooltip("밀기 저항 (0.0: 저항 없음, 1.0: 완전히 밀리지 않음)")]
        private float m_pushResistance = 0f;

        [SerializeField, Tooltip("이동 속도 보정 배율 (1.0 = 100%)")]
        private float m_moveSpeedModifier = 1.0f;
        #endregion

        #region 프로퍼티
        public string ArmorName => m_armorName;
        public ArmorCategory Category => m_category;
        public ArmorType Type => m_type;
        public Sprite BodyArmorSprite => m_bodyArmorSprite;
        public Sprite LeftShoulderSprite => m_leftShoulderSprite;
        public Sprite RightShoulderSprite => m_rightShoulderSprite;
        public Sprite HelmetSprite => m_helmetSprite;
        public Sprite Icon => m_icon != null ? m_icon : m_bodyArmorSprite;
        public int LifeBonus => m_lifeBonus;
        public int HealAmount => m_healAmount;
        public float PushResistance => m_pushResistance;
        public float MoveSpeedModifier => m_moveSpeedModifier;
        #endregion
    }
}

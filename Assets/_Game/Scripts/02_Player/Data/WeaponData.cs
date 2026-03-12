using UnityEngine;

namespace TowerBreakers.Player.Data
{
    public enum WeaponType
    {
        Sword,
        Axe,
        Spear
    }

    /// <summary>
    /// [설명]: 무기의 외형 스프라이트와 애니메이션 클립, 능력치 보정 데이터를 저장하는 ScriptableObject입니다.
    /// </summary>
    [CreateAssetMenu(fileName = "NewWeaponData", menuName = "TowerBreakers/Weapon Data")]
    public class WeaponData : ScriptableObject
    {
        #region 에디터 설정
        [Header("무기 정보")]
        [SerializeField] private string m_weaponName;
        [SerializeField] private WeaponType m_type;

        [Header("외형 설정")]
        [SerializeField, Tooltip("SPUM _weaponList[0]에 할당할 스프라이트")]
        private Sprite m_weaponSprite;

        [Header("애니메이션 설정")]
        [SerializeField, Tooltip("전용 공격 애니메이션 클립 (ATTACK_List[0] 교체용)")]
        private AnimationClip m_attackClip;

        [Header("능력치 보정")]
        [SerializeField, Tooltip("공격력 보정 배율 (1.0 = 100%)")]
        private float m_attackPowerModifier = 1.0f;

        [SerializeField, Tooltip("공격 사거리 보정 배율")]
        private float m_attackRangeModifier = 1.0f;

        [SerializeField, Tooltip("공격 속도 보정 배율")]
        private float m_attackSpeedModifier = 1.0f;

        [Header("상호작용 연출")]
        [SerializeField, Tooltip("적 타격 시 밀어내는 미세 넉백 거리")]
        private float m_knockbackForce = 0.5f;

        [SerializeField, Tooltip("타격 성공 시 화면 역경직(Hit-Stop) 지속시간")]
        private float m_hitStopDuration = 0.05f;
        #endregion

        #region 프로퍼티
        public string WeaponName => m_weaponName;
        public WeaponType Type => m_type;
        public Sprite WeaponSprite => m_weaponSprite;
        public AnimationClip AttackClip => m_attackClip;
        public float AttackPowerModifier => m_attackPowerModifier;
        public float AttackRangeModifier => m_attackRangeModifier;
        public float AttackSpeedModifier => m_attackSpeedModifier;
        public float KnockbackForce => m_knockbackForce;
        public float HitStopDuration => m_hitStopDuration;
        #endregion
    }
}
